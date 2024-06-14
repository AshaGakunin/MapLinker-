using System;
using System.Linq;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using MapLinker.Objects;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Dalamud.IoC;
using Dalamud.Utility;
using ImGuiNET;
using MapLinker.Gui;
using System.Threading.Tasks;
using System.Threading;
using static Dalamud.Plugin.Services.IFramework;

namespace MapLinker
{
    public class MapLinker : IDalamudPlugin
    {
        public string Name => "MapLinker";
        public PluginUi Gui { get; private set; }
        public DalamudPluginInterface Interface { get; private set; }
        public ICommandManager CommandManager { get; private set; }
        public IDataManager DataManager { get; private set; }
        public IClientState ClientState { get; private set; }
        public ITargetManager TargetManager { get; private set; }
        public IFramework Framework { get; private set; }
        public IChatGui ChatGui { get; private set; }
        public IGameGui GameGui { get; private set; }
        

        //Additions
            [PluginService][RequiredVersion("1.0")] public static IObjectTable Obj { get; private set; } = null!;
            

        public Configuration Config { get; private set; }
        public PlayerCharacter LocalPlayer => ClientState.LocalPlayer;
        public bool IsLoggedIn => LocalPlayer != null;
        public bool IsInHomeWorld => LocalPlayer?.CurrentWorld == LocalPlayer?.HomeWorld;

        public Lumina.Excel.ExcelSheet<Aetheryte> Aetherytes = null;
        public Lumina.Excel.ExcelSheet<MapMarker> AetherytesMap = null;
        private Localizer _localizer;

        public void Dispose()
        {
            Framework.Update -= PollPlayerCombatStatus;
            ChatGui.ChatMessage -= Chat_OnChatMessage;
            CommandManager.RemoveHandler("/maplink");
            Gui?.Dispose();
            
        }

        public MapLinker(
            DalamudPluginInterface pluginInterface,
            IChatGui chat,
            ICommandManager commands,
            IDataManager data,
            IClientState clientState,
            IFramework framework,
            IGameGui gameGui,
            ITargetManager targetManager)
        {
            Interface = pluginInterface;
            ClientState = clientState;
            TargetManager = targetManager;
            Framework = framework;
            CommandManager = commands;
            DataManager = data;
            ChatGui = chat;
            GameGui = gameGui;
            Aetherytes = DataManager.GetExcelSheet<Aetheryte>(ClientState.ClientLanguage);
            AetherytesMap = DataManager.GetExcelSheet<MapMarker>(ClientState.ClientLanguage);
            Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(pluginInterface);
            CommandManager.AddHandler("/maplink", new CommandInfo(CommandHandler)
            {
                HelpMessage = "/maplink - open the maplink panel."
            });
            Gui = new PluginUi(this);
            if (Config.TeleportQueued)
            {
                if (LocalPlayer.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat))
                {
                    if (Config.TeleportQueuedLocation != null)
                    {
                        PlaceMapMarker(Config.TeleportQueuedLocation);
                        TeleportToAetheryte(Config.TeleportQueuedLocation);
                        Config.TeleportQueuedLocation = null;
                        Config.TeleportQueued = false;
                    }

                }
            }
            ClientState.LocalPlayer.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat);
            ChatGui.ChatMessage += Chat_OnChatMessage;
            Framework.Update += PollPlayerCombatStatus;
        }

       

        public void CommandHandler(string command, string arguments)
        {
            var args = arguments.Trim().Replace("\"", string.Empty);
            string[] argsArray = args.Split(" ");

            if (argsArray.Length == 2)
            {
                int listIndex;
                List<MapLinkMessage> mapList = Config.MapLinkMessageList;
                if (Config.SortDesc)
                {
                    mapList = mapList.OrderByDescending(mlm => mlm.RecordTime).ToList();
                }
                else
                {
                    mapList = mapList.OrderBy(mlm => mlm.RecordTime).ToList();
                }

                // Convert to zero-based numbering
                if (argsArray[1].Equals("first", StringComparison.OrdinalIgnoreCase))
                {
                    listIndex = 0;
                }
                else if (argsArray[1].Equals("last", StringComparison.OrdinalIgnoreCase))
                {
                    listIndex = mapList.Count - 1;
                }
                else if (int.TryParse(argsArray[1], out listIndex))
                {
                    if (listIndex <= 0 || listIndex > mapList.Count)
                    {
                        listIndex = -1;
                    }
                    else
                    {
                        listIndex--;
                    }
                }
                else
                {
                    listIndex = -1;
                }

                if (listIndex < 0 || listIndex > mapList.Count)
                {
                    Gui.ConfigWindow.Visible = !Gui.ConfigWindow.Visible;
                    return;
                }

                if (argsArray[0].Equals("use", StringComparison.OrdinalIgnoreCase))
                {
                    PlaceMapMarker(mapList[listIndex]);
                    TeleportToAetheryte(mapList[listIndex]);
                }
                else if (argsArray[0].Equals("go", StringComparison.OrdinalIgnoreCase))
                {
                    TeleportToAetheryte(mapList[listIndex]);
                }
                else if (argsArray[0].Equals("map", StringComparison.OrdinalIgnoreCase))
                {
                    PlaceMapMarker(mapList[listIndex]);
                }
            }
            else
            {
                Gui.ConfigWindow.Visible = !Gui.ConfigWindow.Visible;
                return;
            }
        }
        public void Log(string message)
        {
            if (!Config.PrintMessage) return;
            var msg = $"[{Name}] {message}";
            PluginLog.Log(msg);
            ChatGui.Print(msg);
        }
        public void LogError(string message)
        {
            if (!Config.PrintError) return;
            var msg = $"[{Name}] {message}";
            PluginLog.LogError(msg);
            ChatGui.PrintError(msg);
        }
        private int ConvertMapCoordinateToRawPosition(float pos, float scale)
        {
            float num = scale / 100f;
            return (int)((float)((pos - 1.0) * num / 41.0 * 2048.0 - 1024.0) / num * 1000f);
        }
        private float ConvertRawPositionToMapCoordinate(int pos, float scale)
        {
            float num = scale / 100f;
            return (float)((pos / 1000f * num + 1024.0) / 2048.0 * 41.0 / num + 1.0);
        }

        private float ConvertMapMarkerToMapCoordinate(int pos, float scale)
        {
            float num = scale / 100f;
            var rawPosition = (int)((float)(pos - 1024.0) / num * 1000f);
            return ConvertRawPositionToMapCoordinate(rawPosition, scale);
        }

        private double ToMapCoordinate(double val, float scale)
        {
            var c = scale / 100.0;

            val *= c;
            return ((41.0 / c) * ((val + 1024.0) / 2048.0)) + 1;
        }

        //Addition
            public bool AetheryteCloserThanPlayer(MapLinkMessage flag,MapLinkMessage PlayerPosition)
            {
                bool decision = false;
                foreach (var data in Aetherytes)
                {
                    if (!decision)
                    {
                        if (!data.IsAetheryte) continue;
                        if (data.Territory.Value == null) continue;
                        if (data.PlaceName.Value == null) continue;
                        var scale = flag.Scale;
                        if (data.Territory.Value.RowId == flag.TerritoryId)
                        {
                            {
                                var mapMarker = AetherytesMap.FirstOrDefault(m => (m.DataType == 3 && m.DataKey == data.RowId));
                                if (mapMarker == null)
                                {
                                    continue;
                                }
                                var AethersX = ConvertMapMarkerToMapCoordinate(mapMarker.X, scale);
                                var AethersY = ConvertMapMarkerToMapCoordinate(mapMarker.Y, scale);
                                double temp_distance_flag_to_aetheryte = Math.Pow(AethersX - flag.X, 2) + Math.Pow(AethersY - flag.Y, 2);
                                double temp_distance_flag_to_player = Math.Pow(PlayerPosition.X - flag.X, 2) + Math.Pow(PlayerPosition.Y - flag.Y, 2);
                                //PluginLog.Log("Flag to aetheryte distance " + temp_distance_flag_to_aetheryte.ToString() + " vs Player to flag distance " + temp_distance_flag_to_player.ToString());
                                if (temp_distance_flag_to_aetheryte < temp_distance_flag_to_player)
                                {
                                    //PluginLog.Log("an atheryte is closer");
                                    decision = true;
                                    continue;
                                }
                            }
                        }
                    }
                }
                return decision;
            }
            private void PollPlayerCombatStatus(IFramework framework)
            {
                //PluginLog.Log("Would Have Teleported Here");
               
                if (Config.TeleportQueued)
                {
                    PluginLog.Log(Config.TeleportQueued.ToString());
                    if (!ClientState.LocalPlayer.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat))
                    {
                        TeleportToAetheryte(Config.TeleportQueuedLocation);
                        PlaceMapMarker(Config.TeleportQueuedLocation);
                        Config.TeleportQueued = false;
                    }
                }
                
            }
            public void SetTeleportQueue(bool Queued, MapLinkMessage Place)
            {
                
                Config.TeleportQueued= Queued;
                Config.TeleportQueuedLocation= Place;
                PluginLog.Log("Setting Teleport Queded To true: " + Config.TeleportQueued.ToString());
            }

        public string GetNearestAetheryte(MapLinkMessage maplinkMessage)
        {
            string aetheryteName = "";
            double distance = 0;
            foreach (var data in Aetherytes)
            {
                if (!data.IsAetheryte) continue;
                if (data.Territory.Value == null) continue;
                if (data.PlaceName.Value == null) continue;
                var scale = maplinkMessage.Scale;
                if (data.Territory.Value.RowId == maplinkMessage.TerritoryId)
                {
                    var mapMarker = AetherytesMap.FirstOrDefault(m => (m.DataType == 3 && m.DataKey == data.RowId));
                    if (mapMarker == null)
                    {
                        LogError($"Cannot find aetherytes position for {maplinkMessage.PlaceName}#{data.PlaceName.Value.Name}");
                        continue;
                    }
                    var AethersX = ConvertMapMarkerToMapCoordinate(mapMarker.X, scale);
                    var AethersY = ConvertMapMarkerToMapCoordinate(mapMarker.Y, scale);
                    Log($"Aetheryte: {data.PlaceName.Value.Name} ({AethersX} ,{AethersY})");
                    double temp_distance = Math.Pow(AethersX - maplinkMessage.X, 2) + Math.Pow(AethersY - maplinkMessage.Y, 2);
                    if (aetheryteName == "" || temp_distance < distance)
                    {
                        distance = temp_distance;
                        aetheryteName = data.PlaceName.Value.Name;
                    }
                }
            }
            return aetheryteName;
        }

        public void GetTarget()
        {
            string messageText = "";
            float coordX = 0;
            float coordY = 0;
            float scale = 100;

            var target = TargetManager.Target;
            var territoryType = ClientState.TerritoryType;
            var place = DataManager.GetExcelSheet<Map>(ClientState.ClientLanguage).FirstOrDefault(m => m.TerritoryType.Row == territoryType);
            var placeName = place.PlaceName.Row;
            scale = place.SizeFactor;
            var placeNameRow = DataManager.GetExcelSheet<PlaceName>(ClientState.ClientLanguage).GetRow(placeName).Name;
            if (target != null)
            {
                coordX = (float)ToMapCoordinate(target.Position.X, scale);
                coordY = (float)ToMapCoordinate(target.Position.Z, scale);
                messageText += placeNameRow;
                messageText += " X:" + coordX.ToString("#0.0");
                messageText += " Y:" + coordY.ToString("#0.0");
                var newMapLinkMessage = new MapLinkMessage(
                        (ushort)XivChatType.Debug,
                        target.Name.ToString(),
                        messageText,
                        coordX,
                        coordY,
                        scale,
                        territoryType,
                        placeNameRow,
                        DateTime.Now
                    );
                Config.MapLinkMessageList.Add(newMapLinkMessage);
                if (Config.MapLinkMessageList.Count > Config.MaxRecordings)
                {
                    var tempList = Config.MapLinkMessageList.OrderBy(e => e.RecordTime);
                    Config.MapLinkMessageList.RemoveRange(0, Config.MapLinkMessageList.Count - Config.MaxRecordings);
                    var infoMsg = $"There are too many records, truncated to the latest {Config.MaxRecordings} records";
                    PluginLog.Information(infoMsg);
                }
            }
            

        }

        private void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (!Config.Recording) return;
            bool hasMapLink = false;
            float coordX = 0;
            float coordY = 0;
            float scale = 100;
            MapLinkPayload maplinkPayload = null;
            foreach (var payload in message.Payloads)
            {
                if (payload is MapLinkPayload mapLinkload)
                {
                    maplinkPayload = mapLinkload;
                    hasMapLink = true;
                    // float fudge = 0.05f;
                    scale = mapLinkload.TerritoryType.Map.Value.SizeFactor;
                    // coordX = ConvertRawPositionToMapCoordinate(mapLinkload.RawX, scale) - fudge;
                    // coordY = ConvertRawPositionToMapCoordinate(mapLinkload.RawY, scale) - fudge;
                    coordX = mapLinkload.XCoord;
                    coordY = mapLinkload.YCoord;
                    Log($"TerritoryId: {mapLinkload.TerritoryType.RowId} {mapLinkload.PlaceName} ({coordX} ,{coordY})");
                }
            }
            string messageText = message.TextValue;
            if (hasMapLink)
            {
                var newMapLinkMessage = new MapLinkMessage(
                        (ushort)type,
                        sender.TextValue,
                        messageText,
                        coordX,
                        coordY,
                        scale,
                        maplinkPayload.TerritoryType.RowId,
                        maplinkPayload.PlaceName,
                        DateTime.Now
                    );

                //Additions
                if (Config.PromptTeleport)
                {

                    //Calculate if Tping is actually worth it
                        var TpCheckTest = false;
                        if (maplinkPayload.TerritoryType.RowId == ClientState.TerritoryType)
                        { 
                            var playerPositon = new MapLinkMessage(
                                (ushort)type,
                                LocalPlayer.Name.TextValue,
                                messageText,
                                LocalPlayer.GetMapCoordinates().X,
                                LocalPlayer.GetMapCoordinates().Y,
                                scale,
                                ClientState.TerritoryType,
                                maplinkPayload.PlaceName,
                                DateTime.Now
                            );
                            TpCheckTest = AetheryteCloserThanPlayer(newMapLinkMessage, playerPositon);
                        }
                        else { TpCheckTest = true; }
                        PluginLog.Log(" Worth It Or Not To TP:" + TpCheckTest.ToString());
                    if (TpCheckTest)
                    {
                        if (Config.WhiteListOnly)
                        {
                            //Gather Sender Name
                            //Removes special characters people can put on friends in their friends list.
                            var charsToRemove = new string[] { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
                            string senderstring = sender.ToString();
                            foreach (var c in charsToRemove)
                            {
                                senderstring = senderstring.Replace(c, string.Empty);
                            }


                            var homeworld = "";
                            try
                            {
                                homeworld = sender.Payloads[0].ToString().Split(",")[2].Split(": ")[1];
                            }
                            catch (Exception e) { PluginLog.Log("no probably player"); }

                            if (LocalPlayer.Name.ToString() != sender.TextValue)
                            {
                                for (int i = 0; i < Config.PlayerTPWhiteList.Count; i++)
                                {

                                    if (Config.PlayerTPWhiteList[i].Name == sender.TextValue)
                                    {
                                        PluginLog.Log(sender.TextValue + " offering a place to tp you");

                                        Config.MostRecentMapLink = newMapLinkMessage;
                                        Config.PromptWindowGoal = "It is faster to Teleport";

                                        PluginLog.Log("Tp check is true white list is false");
                                        Config.PopupOpen = true;

                                        try
                                        {
                                            //Gui.ConfigWindow.Draw();
                                            Gui.ConfigWindow.Visible = true;
                                        }
                                        catch (Exception e) { PluginLog.Log(e.ToString()); }
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < Config.PlayerTPWhiteList.Count; i++)
                                {
                                    // if (sender.ToString() == Config.PlayerTPWhiteList[i].Name)
                                    //{
                                    //PluginLog.Log("Yes this matched");
                                    //}
                                    //PluginLog.Log(Config.PlayerTPWhiteList[i].Name+" " + Config.PlayerTPWhiteList[i].HomeWorld);
                                }
                                //PluginLog.Log(sender.ToString()+" VS "+sender.TextValue);
                                //TeleportToAetheryte(newMapLinkMessage);
                            }
                        }
                        else
                        {


                            if (!Config.TeleportQueued)
                            {
                                if (!(Config.MostRecentMapLink.X == newMapLinkMessage.X && Config.MostRecentMapLink.Y == newMapLinkMessage.Y && Config.MostRecentMapLink.TerritoryId == newMapLinkMessage.TerritoryId))
                                {
                                    Config.MostRecentMapLink = newMapLinkMessage;
                                    Config.PromptWindowGoal = "It is faster to Teleport";

                                    PluginLog.Log("Tp check is true white list is false");
                                    Config.PopupOpen = true;
                                    
                                    try
                                    {
                                        //Gui.ConfigWindow.Draw();
                                        Gui.ConfigWindow.Visible = true;
                                    }
                                    catch (Exception e) { PluginLog.Log(e.ToString()); }


                                }

                            }


                        }
                    }
                    else
                    {
                        if (!(Config.MostRecentMapLink.X == newMapLinkMessage.X && Config.MostRecentMapLink.Y == newMapLinkMessage.Y && Config.MostRecentMapLink.TerritoryId == newMapLinkMessage.TerritoryId))
                        {
                            Config.MostRecentMapLink = newMapLinkMessage;
                            Config.PromptWindowGoal = "It is faster to Fly";

                            PluginLog.Log("Tp check is false");
                            Config.PopupOpen = true;
                            try
                            {
                                Gui.ConfigWindow.Visible = true;
                            }
                            catch (Exception e) { PluginLog.Log(e.ToString()); }


                        }
                    }
                }


                
                bool filteredOut = false;
                if (sender.TextValue.ToLower() == "sonar")
                    filteredOut = true;
                bool alreadyInList = Config.MapLinkMessageList.Any(w => {
                    bool sameText = w.Text == newMapLinkMessage.Text;
                    var timeoutMin = new TimeSpan(0, Config.FilterDupTimeout, 0);
                    if (newMapLinkMessage.RecordTime < w.RecordTime + timeoutMin)
                    {
                        bool sameX = (int)(w.X * 10) == (int)(newMapLinkMessage.X * 10);
                        bool sameY = (int)(w.Y * 10) == (int)(newMapLinkMessage.Y * 10);
                        bool sameTerritory = w.TerritoryId == newMapLinkMessage.TerritoryId;
                        return sameTerritory && sameX && sameY;
                    }
                    return sameText;
                });
                if (Config.FilterDuplicates && alreadyInList) filteredOut = true;
                if (!filteredOut && Config.FilteredChannels.IndexOf((ushort)type) != -1) filteredOut = true;
                if (!filteredOut)
                {
                    Config.MapLinkMessageList.Add(newMapLinkMessage);
                    if (Config.MapLinkMessageList.Count > Config.MaxRecordings)
                    {
                        var tempList = Config.MapLinkMessageList.OrderBy(e => e.RecordTime);
                        Config.MapLinkMessageList.RemoveRange(0, Config.MapLinkMessageList.Count - Config.MaxRecordings);
                        var infoMsg = $"There are too many records, truncated to the latest {Config.MaxRecordings} records";
                        PluginLog.Information(infoMsg);
                    }
                    Config.Save();
                    if (Config.BringFront)
                    {
                        Native.Impl.Activate();
                    }
                }

                

            }
        }

        public void TeleportToAetheryte(MapLinkMessage maplinkMessage)
        {
            if (!Config.Teleport) return;
            var aetheryteName = GetNearestAetheryte(maplinkMessage);
            if (aetheryteName != "")
            {
                Log($"Teleporting to {aetheryteName}");
                CommandManager.ProcessCommand($"/tp {aetheryteName}");
            }
            else
            {
                LogError($"Cannot find nearest aetheryte of {maplinkMessage.PlaceName}({maplinkMessage.X}, {maplinkMessage.Y}).");
            }
        }

        public void PlaceMapMarker(MapLinkMessage maplinkMessage)
        {
            Log($"Viewing {maplinkMessage.Text}");
            var map = DataManager.GetExcelSheet<TerritoryType>().GetRow(maplinkMessage.TerritoryId).Map;
            var maplink = new MapLinkPayload(maplinkMessage.TerritoryId, map.Row, maplinkMessage.X, maplinkMessage.Y);
            GameGui.OpenMapWithMapLink(maplink);
        }
    }
}
