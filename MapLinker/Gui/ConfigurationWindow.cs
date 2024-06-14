using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text;
using ImGuiNET;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using MapLinker.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Logging;
using static MapLinker.Configuration;
using Dalamud.Interface.Utility.Table;

namespace MapLinker.Gui
{
    public class ConfigurationWindow : Window<MapLinker>
    {

        public Configuration Config => Plugin.Config;
        private readonly string[] _languageList;
        private int _selectedLanguage;
        private Localizer _localizer;
        public List<XivChatType> HiddenChatType = new List<XivChatType> {
            XivChatType.None,
            XivChatType.CustomEmote,
            XivChatType.StandardEmote,
            XivChatType.SystemMessage,
            XivChatType.SystemError,
            XivChatType.GatheringSystemMessage,
            XivChatType.ErrorMessage,
            XivChatType.RetainerSale
        };
        //Additions
            private static List<WhiteListPlayer> playerList = new List<WhiteListPlayer>();
            
            private static int item_current_idx = 0;

        public ConfigurationWindow(MapLinker plugin) : base(plugin)
        {
            _languageList = new string[] { "en", "zh" };
            _localizer = new Localizer(Config.UILanguage);
        }

        protected override void DrawUi()
        {
            if (Plugin.ClientState.LocalPlayer?.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat) == true)
            {
                ImGui.SetNextWindowBgAlpha(Config.CombatOpacity);
                if (Config.CombatHide)
                {
                    return;
                }
                if (Config.CombatClickthru)
                {
                    ImGui.SetNextFrameWantCaptureMouse(false);
                }
            }
            ImGui.SetNextWindowSize(new Vector2(530, 450), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin($"{Plugin.Name} {_localizer.Localize("Panel")}", ref WindowVisible, ImGuiWindowFlags.NoScrollWithMouse|ImGuiWindowFlags.NoCollapse))
            {
                ImGui.End();
                return;
            }
            //DrawPopup();
            if (ImGui.BeginTabBar(_localizer.Localize("TabBar")))
            {
                if (ImGui.BeginTabItem(_localizer.Localize("Settings") + "##Tab"))
                {
                    if (ImGui.BeginChild("##SettingsRegion"))
                    {
                        if (ImGui.CollapsingHeader(_localizer.Localize("General Settings"), ImGuiTreeNodeFlags.DefaultOpen))
                            DrawGeneralSettings();
                        if (ImGui.CollapsingHeader(_localizer.Localize("Filters")))
                            DrawFilters();
                        ImGui.EndChild();
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem(_localizer.Localize("Records") + "##Tab"))
                {
                    if (ImGui.BeginChild("##RecordsRegion"))
                    {
                        DrawMaplinks();
                        ImGui.EndChild();
                    }
                    ImGui.EndTabItem();
                }
                //Additions
                    if (ImGui.BeginTabItem(_localizer.Localize("Player White List") + "##Tab"))
                    {
                        if (ImGui.BeginChild("##PlayerWhiteListRegion"))
                        {
                            DrawPlayerWhiteList();
                            ImGui.EndChild();
                        }
                        ImGui.EndTabItem();
                    }


                ImGui.EndTabBar();
            }
            

           

            
            //ImGui.OpenPopup(_localizer.Localize(Config.PromptWindowGoal));
            ImGui.End();
        }

        

        private void DrawGeneralSettings()
        {

            //Additions
                if(ImGui.Checkbox(_localizer.Localize("Prompt Teleport When Flag Is Relayed In Chat"), ref Config.PromptTeleport)) Config.Save();
                if(Config.ShowTooltips && ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(_localizer.Localize("Will create a popup asking if the player would like to be teleported when a flag is put into chat. You can choose to only have this happen on white listed players by selecting the 'Prompt for White List Players Only'"));
                }
                if (ImGui.Checkbox(_localizer.Localize("Prompt for White List Players Only"), ref Config.WhiteListOnly)) Config.Save();
                if (Config.ShowTooltips && ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(_localizer.Localize("This setting will only prompt teleport from flags put into chat by white listed players."));

                }
            if (ImGui.Checkbox(_localizer.Localize("Recording"), ref Config.Recording)) Config.Save();
            if (Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Automatically record messages with maplink and retrieve later."));
            ImGui.SameLine(ImGui.GetColumnWidth() - 80);
            ImGui.TextUnformatted(_localizer.Localize("Tooltips"));
            ImGui.AlignTextToFramePadding();
            ImGui.SameLine();
            if (ImGui.Checkbox("##hideTooltipsOnOff", ref Config.ShowTooltips)) Config.Save();

            if (ImGui.Checkbox(_localizer.Localize("Call /tp to teleport to the nearest aetheryte"), ref Config.Teleport)) Config.Save();
            if (Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Add an option to call /tp to teleport to the nearest aetheryte.\n" +
                                 "Make sure you have downloaded Teleporter Plugin."));
            if (ImGui.Checkbox(_localizer.Localize("Reverse sorting"), ref Config.SortDesc)) Config.Save();
            if (ImGui.Checkbox(_localizer.Localize("Bring front"), ref Config.BringFront)) Config.Save();
            if (Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Bring the game to front with new maplink"));
            if (ImGui.Checkbox(_localizer.Localize("Message Wrap"), ref Config.MessageWrap)) Config.Save();
            if (Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Line Wrap for message column."));
            if (ImGui.Checkbox(_localizer.Localize("Combat Hide"), ref Config.CombatHide)) Config.Save();
            if (Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Hide during combat."));
            if (ImGui.Checkbox(_localizer.Localize("Combat Click Thru"), ref Config.CombatClickthru)) Config.Save();
            if (Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Click through during combat."));
            if (ImGui.DragInt(_localizer.Localize("Max Records"), ref Config.MaxRecordings, 1, 10, 100)) Config.Save();
            if (ImGui.DragFloat(_localizer.Localize("Combat Opacity"), ref Config.CombatOpacity, 0.01f, 0, 1)) Config.Save();
            if (Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Opacity during combat."));

            ImGui.TextUnformatted(_localizer.Localize("Language:"));
            if (Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Change the UI Language."));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("##hideLangSetting", ref _selectedLanguage, _languageList, _languageList.Length))
            {
                Config.UILanguage = _languageList[_selectedLanguage];
                _localizer.Language = Config.UILanguage;
                Config.Save();
            }
            if (ImGui.Checkbox(_localizer.Localize("Print Debug Message"), ref Config.PrintMessage)) Config.Save();
            if (ImGui.Checkbox(_localizer.Localize("Print Error Message"), ref Config.PrintError)) Config.Save();
        }

        private void DrawFilters()
        {
            if (ImGui.Checkbox(_localizer.Localize("Filter out duplicates"), ref Config.FilterDuplicates)) Config.Save();
            ImGui.SameLine();
            if (ImGui.DragInt(_localizer.Localize("Timeout"), ref Config.FilterDupTimeout, 1, 1, 60)) Config.Save();
            if (Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Maplink within timeout will be filtered by it's maplink instead of full text."));
            ImGui.Columns(4, "FiltersTable", true);
            foreach (ushort chatType in Enum.GetValues(typeof(XivChatType)))
            {
                if (HiddenChatType.IndexOf((XivChatType)chatType) != -1) continue;
                string chatTypeName = Enum.GetName(typeof(XivChatType), chatType);
                bool checkboxClicked = Config.FilteredChannels.IndexOf(chatType) == -1;
                if (ImGui.Checkbox(_localizer.Localize(chatTypeName) + "##filter", ref checkboxClicked))
                {
                    Config.FilteredChannels = Config.FilteredChannels.Distinct().ToList();
                    if (checkboxClicked)
                    {
                        if (Config.FilteredChannels.IndexOf(chatType) != -1)
                            Config.FilteredChannels.Remove(chatType);
                    }
                    else if (Config.FilteredChannels.IndexOf(chatType) == -1)
                    {
                        Config.FilteredChannels.Add(chatType);
                    }
                    Config.FilteredChannels = Config.FilteredChannels.Distinct().ToList();
                    Config.FilteredChannels.Sort();
                    Config.Save();
                }
                ImGui.NextColumn();
            }
            ImGui.Columns(1);

        }

        private void DrawMaplinks()
        {
            // sender, text, time, view, tp, del
            int columns = 5;
            if (Config.Teleport) columns++;
            if (ImGui.Button(_localizer.Localize("Clear")))
            {
                Config.MapLinkMessageList.Clear();
                Config.Save();
            }
            // right alignment ?
            ImGui.SameLine(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(_localizer.Localize("Target")).X - ImGui.GetScrollX() - ImGui.GetStyle().ItemSpacing.X);
            if (ImGui.Button(_localizer.Localize("Target")))
            {
                Plugin.GetTarget();
                Config.Save();
            }
            ImGui.Columns(columns, "Maplinks", true);
            ImGui.Separator();
            ImGui.Text(_localizer.Localize("Sender")); ImGui.NextColumn();
            ImGui.TextWrapped(_localizer.Localize("Message")); ImGui.NextColumn();
            ImGui.Text(_localizer.Localize("Time")); ImGui.NextColumn();
            ImGui.Text(_localizer.Localize("Retrieve")); ImGui.NextColumn();
            if (Config.Teleport)
            {
                ImGui.Text(_localizer.Localize("Teleport")); ImGui.NextColumn();
            }
            ImGui.Text(_localizer.Localize("Delete")); ImGui.NextColumn();
            ImGui.Separator();
            MapLinkMessage toDelete = null;
            List<MapLinkMessage> listToDisplay = Config.MapLinkMessageList;
            if (Config.SortDesc)
            {
                listToDisplay = listToDisplay.OrderByDescending(mlm => mlm.RecordTime).ToList();
            } else
            {
                listToDisplay = listToDisplay.OrderBy(mlm => mlm.RecordTime).ToList();
            }
            for (int i = 0; i < listToDisplay.Count(); i++)
            {
                var maplinkMessage = listToDisplay[i];
                ImGui.Text(maplinkMessage.Sender); ImGui.NextColumn();
                if (Config.MessageWrap)
                {
                    ImGui.PushTextWrapPos();
                    ImGui.TextUnformatted(maplinkMessage.Text); ImGui.NextColumn();
                    ImGui.PopTextWrapPos();
                }
                else
                {
                    ImGui.TextUnformatted(maplinkMessage.Text); ImGui.NextColumn();
                }
                ImGui.Text(maplinkMessage.RecordTime.ToString()); ImGui.NextColumn();
                if(ImGui.Button(_localizer.Localize("View") + "##" + i.ToString() ))
                {
                    Plugin.PlaceMapMarker(maplinkMessage);
                }
                ImGui.NextColumn();
                if (Config.Teleport)
                {
                    if (ImGui.Button(_localizer.Localize("Tele") + "##" + i.ToString()))
                    {
                        Plugin.TeleportToAetheryte(maplinkMessage);
                    }
                    ImGui.NextColumn();
                }
                if (ImGui.Button(_localizer.Localize("Del") + "##" + i.ToString()))
                {
                    toDelete = maplinkMessage;
                }
                ImGui.NextColumn();
                ImGui.Separator();
            }
            ImGui.Columns(1);

            if (null != toDelete)
            {
                Config.MapLinkMessageList.Remove(toDelete);
                Config.Save();
            }

            if (ImGui.Button(_localizer.Localize("Export")))
            {
                string text = "";
                for (int i = 0; i < listToDisplay.Count(); i++)
                {
                    var maplinkMessage = listToDisplay[i];
                    text += $"{maplinkMessage.Sender}|{maplinkMessage.Text}|{maplinkMessage.RecordTime}\n";
                }
                ImGui.SetClipboardText(text.Trim());
                Plugin.ChatGui.Print($"{listToDisplay.Count()} maplinks were copied to clickboard.");
            }

        }

        //Additions
            private void DrawPlayerWhiteList()
            {
                int columns = 3;
                var green = new Vector4(0.16470588235294117f, 0.7215686274509804f, 0.10980392156862745f, .8f);
                var red = new Vector4(1.0f, 0.0f, 0.0f, .8f);
                var blue = new Vector4(0.15294117647058825f, 0.5058823529411764f, 0.9607843137254902f, .8f);
                var orange = new Vector4(0.9607843137254902f, 0.5725490196078431f, 0.15294117647058825f, .8f);
                ImGui.Text(_localizer.Localize("Player White List"));

            if (ImGui.BeginPopup(_localizer.Localize(Config.PromptWindowGoal),ImGuiWindowFlags.Popup))
            {
                if (Config.PromptWindowGoal == "It is faster to Teleport")
                {
                    if (Plugin.ClientState.LocalPlayer?.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat) == true)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, orange);
                        if (ImGui.Button(_localizer.Localize("Teleport After Combat")))
                        {
                            Config.TeleportQueuedLocation = Config.MostRecentMapLink;
                            Config.TeleportQueued = true;
                            ImGui.CloseCurrentPopup();
                            Config.PopupOpen = false;
                            try
                            {
                                //Gui.ConfigWindow.Draw();
                                Visible = false;
                            }
                            catch (Exception e) { PluginLog.Log(e.ToString()); }
                        }
                        try { ImGui.PopStyleColor(); } catch (Exception e) { }
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, green);
                        if (ImGui.Button(_localizer.Localize("Teleport")))
                        {
                            Plugin.TeleportToAetheryte(Config.MostRecentMapLink);
                            Plugin.PlaceMapMarker(Config.MostRecentMapLink);
                            ImGui.CloseCurrentPopup();
                            Config.PopupOpen = false;
                            try
                            {
                                //Gui.ConfigWindow.Draw();
                                Visible = false;
                            }
                            catch (Exception e) { PluginLog.Log(e.ToString()); }
                        }
                        try { ImGui.PopStyleColor(); } catch (Exception e) { }
                    }
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, blue);
                    if (ImGui.Button(_localizer.Localize("Place Map Marker")))
                    {
                        Plugin.PlaceMapMarker(Config.MostRecentMapLink);
                        Config.PopupOpen = false;
                        ImGui.CloseCurrentPopup();
                        try
                        {
                            //Gui.ConfigWindow.Draw();
                            Visible = false;
                        }
                        catch (Exception e) { PluginLog.Log(e.ToString()); }
                    }
                    try { ImGui.PopStyleColor(); } catch (Exception e) { }
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, red);
                    if (ImGui.Button(_localizer.Localize("Close")))
                    {
                        ImGui.CloseCurrentPopup();
                        Config.PopupOpen = false;
                        Visible = false;
                    }
                    try { ImGui.PopStyleColor(); } catch (Exception e) { }
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, blue);
                    if (ImGui.Button(_localizer.Localize("Place Map Marker")))
                    {
                        Plugin.PlaceMapMarker(Config.MostRecentMapLink);
                        Config.PopupOpen = false;
                        ImGui.CloseCurrentPopup();
                        Visible = false;
                    }
                    try { ImGui.PopStyleColor(); } catch (Exception e) { }
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, red);
                    if (ImGui.Button(_localizer.Localize("Close")))
                    {
                        ImGui.CloseCurrentPopup();
                        Config.PopupOpen = false;
                        Visible=false;
                    }
                    try { ImGui.PopStyleColor(); } catch (Exception e) { }
                }
                ImGui.EndPopup();
            }
           
            if (ImGui.IsPopupOpen(_localizer.Localize(Config.PromptWindowGoal)))
            {
                ImGui.Text("Test");
            }
            else
            {
                if (Config.PopupOpen)
                {
                    ImGui.OpenPopup(_localizer.Localize(Config.PromptWindowGoal));
                }
               
            }
            for (int n = 0; n < MapLinker.Obj.Length; n++)
                {
                    if (MapLinker.Obj[n] != null)
                    {
                        if (MapLinker.Obj[n].ObjectKind.ToString() == "Player")
                        {


                            var check = true;
                            if (playerList.Count > 0)
                            {

                                for (var g = 0; g < playerList.Count; g++)
                                {
                                    if (playerList[g].Name == MapLinker.Obj[n].Name.ToString())
                                    {
                                        check = false;
                                    }

                                }
                            }
                            if (check)
                            {
                                PlayerCharacter ThisPlayer = (PlayerCharacter)MapLinker.Obj[n];
                                var p = new WhiteListPlayer();
                                p.Name = MapLinker.Obj[n].Name.ToString();
                                p.HomeWorld = ThisPlayer.HomeWorld.GameData.Name.ToString();
                                playerList.Add(p);
                            }


                        }
                    }

                }

                if (playerList.Count > 0)
                {
                    try
                    {
                        string combo_preview_value = playerList[item_current_idx].Name;
                        if (ImGui.BeginCombo(_localizer.Localize("Player to Add"), combo_preview_value))
                        {


                            var lengthObj = playerList.Count;
                            for (int n = 0; n < lengthObj; n++)
                            {


                                bool is_selected = item_current_idx == n;


                                if (ImGui.Selectable(playerList[n].Name, true))
                                    item_current_idx = n;


                                if (is_selected)

                                    ImGui.SetItemDefaultFocus();
                                // Set the initial focus when opening the combo (scrolling + keyboard navigation focus)

                            }
                            ImGui.EndCombo();

                        }
                    }
                    catch (Exception e)
                    {
                        PluginLog.Log(e.ToString());
                        if (item_current_idx != 0)
                        {
                            item_current_idx--;

                        }
                        else
                        {
                            item_current_idx = 0;
                        }


                    }
                }
                ImGui.SameLine();
                
                ImGui.PushStyleColor(ImGuiCol.Button, green);
                if (ImGui.Button(_localizer.Localize("Add To White List")))
                {

                    Config.PlayerTPWhiteList.Add(playerList[item_current_idx]);
                }
                try
                {
                    ImGui.PopStyleColor();
                }
                catch (Exception e) { }

                ImGui.Text(_localizer.Localize("Add players to the white list from the zone you are in."));
                ImGui.Separator();
                ImGui.Columns(columns, _localizer.Localize("Player White List"), true);
                ImGui.Text(_localizer.Localize("Player")); ImGui.NextColumn();
                ImGui.TextWrapped(_localizer.Localize("Home World")); ImGui.NextColumn();
                ImGui.Text(_localizer.Localize("Delete")); ImGui.NextColumn();
                ImGui.Separator();
                for(int i=0; i<Config.PlayerTPWhiteList.Count; i++){
                    ImGui.Text(Config.PlayerTPWhiteList[i].Name); ImGui.NextColumn();
                    ImGui.Text(Config.PlayerTPWhiteList[i].HomeWorld); ImGui.NextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Button, red);
                    if (ImGui.Button(_localizer.Localize("Delete") + "##" + i.ToString()))
                    {
                        PluginLog.Log("Removing "+Config.PlayerTPWhiteList[i].Name);
                        Config.PlayerTPWhiteList.RemoveAt(i);
                    }
                    try{ ImGui.PopStyleColor(); }catch(Exception e) { }
                    ImGui.NextColumn();
                    //ImGui.Text("Remove at " + i);
                    //ImGui.NextColumn();
                    ImGui.Separator();

                }   

        }
            private void DrawPopup()
            {
                int columns = 3;
                var green = new Vector4(0.16470588235294117f, 0.7215686274509804f, 0.10980392156862745f, .8f);
                var red = new Vector4(1.0f, 0.0f, 0.0f, .8f);
                var blue = new Vector4(0.15294117647058825f, 0.5058823529411764f, 0.9607843137254902f, .8f);
                var orange = new Vector4(0.9607843137254902f, 0.5725490196078431f, 0.15294117647058825f, .8f);
                

                if (ImGui.BeginPopup(_localizer.Localize(Config.PromptWindowGoal), ImGuiWindowFlags.Popup))
                {
                    if (Config.PromptWindowGoal == "It is faster to Teleport")
                    {
                        if (Plugin.ClientState.LocalPlayer?.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat) == true)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, orange);
                            if (ImGui.Button(_localizer.Localize("Teleport and Place Map Marker")))
                            {
                                Plugin.SetTeleportQueue(true, Config.MostRecentMapLink);
                                //Config.TeleportQueuedLocation = Config.MostRecentMapLink;
                                //Config.TeleportQueued = true;
                                ImGui.CloseCurrentPopup();
                                Config.PopupOpen = false;
                            }
                            try { ImGui.PopStyleColor(); } catch (Exception e) { }
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, green);
                            if (ImGui.Button(_localizer.Localize("Teleport and Place Map Marker")))
                            {
                                Plugin.TeleportToAetheryte(Config.MostRecentMapLink);
                                Plugin.PlaceMapMarker(Config.MostRecentMapLink);
                                ImGui.CloseCurrentPopup();
                                Config.PopupOpen = false;
                            }
                            try { ImGui.PopStyleColor(); } catch (Exception e) { }
                        }
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Button, blue);
                        if (ImGui.Button(_localizer.Localize("Place Map Marker")))
                        {
                            Plugin.PlaceMapMarker(Config.MostRecentMapLink);
                            Config.PopupOpen = false;
                            ImGui.CloseCurrentPopup();
                        }
                        try { ImGui.PopStyleColor(); } catch (Exception e) { }
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Button, red);
                        if (ImGui.Button(_localizer.Localize("Close")))
                        {
                            ImGui.CloseCurrentPopup();
                            Config.PopupOpen = false;
                        }
                        try { ImGui.PopStyleColor(); } catch (Exception e) { }
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, blue);
                        if (ImGui.Button(_localizer.Localize("Place Map Marker")))
                        {
                            Plugin.PlaceMapMarker(Config.MostRecentMapLink);
                            Config.PopupOpen = false;
                            ImGui.CloseCurrentPopup();
                        }
                        try { ImGui.PopStyleColor(); } catch (Exception e) { }
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Button, red);
                        if (ImGui.Button(_localizer.Localize("Close")))
                        {
                            ImGui.CloseCurrentPopup();
                            Config.PopupOpen = false;
                        }
                        try { ImGui.PopStyleColor(); } catch (Exception e) { }
                    }
                    ImGui.EndPopup();
                }       
            }
    }
}