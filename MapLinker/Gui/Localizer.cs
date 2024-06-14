using Lumina.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLinker.Gui
{
    class Localizer
    {
        public string Language = "en";
        private Dictionary<string, string> zh = new Dictionary<string, string> { };
        public Localizer(string language="en")
        {
            Language = language;
            LoadZh();
        }
        public string Localize(string message)
        {
            if (message == null) return message;
            if (Language == "zh") return zh.ContainsKey(message) ? zh[message] : message;
            return message;
        }
        private void LoadZh()
        {
            zh.Add("Panel", "面板");
            zh.Add("TabBar", "标签栏");
            zh.Add("Settings", "设置");
            zh.Add("General Settings", "通用设置");
            zh.Add("Recording", "记录中");
            zh.Add("Tooltips", "选项说明");
            zh.Add("Reverse sorting", "反向排序");
            zh.Add("Call /tp to teleport to the nearest aetheryte", "使用 /tp 命令进行传送");
            zh.Add("Language:", "语言");
            zh.Add("Print Debug Message", "打印调试消息");
            zh.Add("Print Error Message", "打印错误消息");
            zh.Add("Filters", "过滤设置");
            zh.Add("Records", "记录");
            zh.Add("Filter out duplicates", "过滤重复消息");
            zh.Add("FiltersTable", "过滤表格");
            zh.Add("Debug", "调试");
            zh.Add("Urgent", "紧急");
            zh.Add("Notice", "通知");
            zh.Add("Say", "说话");
            zh.Add("Shout", "喊话");
            zh.Add("TellOutgoing", "发出悄悄话");
            zh.Add("TellIncoming", "收到悄悄话");
            zh.Add("Party", "小队");
            zh.Add("Alliance", "团队");
            zh.Add("FreeCompany", "部队");
            zh.Add("Free", "团队");
            zh.Add("Ls1", "通讯贝1");
            zh.Add("Ls2", "通讯贝2");
            zh.Add("Ls3", "通讯贝3");
            zh.Add("Ls4", "通讯贝4");
            zh.Add("Ls5", "通讯贝5");
            zh.Add("Ls6", "通讯贝6");
            zh.Add("Ls7", "通讯贝7");
            zh.Add("Ls8", "通讯贝8");
            zh.Add("Yell", "呼喊");
            zh.Add("CrossParty", "跨服小队");
            zh.Add("PvPTeam", "PvP小队");
            zh.Add("NoviceNetwork", "新人频道");
            zh.Add("CrossLinkShell1", "跨服通讯贝1");
            zh.Add("CrossLinkShell2", "跨服通讯贝2");
            zh.Add("CrossLinkShell3", "跨服通讯贝3");
            zh.Add("CrossLinkShell4", "跨服通讯贝4");
            zh.Add("CrossLinkShell5", "跨服通讯贝5");
            zh.Add("CrossLinkShell6", "跨服通讯贝6");
            zh.Add("CrossLinkShell7", "跨服通讯贝7");
            zh.Add("CrossLinkShell8", "跨服通讯贝8");
            zh.Add("Echo", "默语");
            zh.Add("Automatically record messages with maplink and retrieve later.", "使用自动记录消息中的地图坐标并在之后检索。");
            zh.Add("Add an option to call /tp to teleport to the nearest aetheryte.\nMake sure you have downloaded Teleporter Plugin.", "调用 /tp 传送至最近的以太水晶。\n请确保已启用 Teleporter 插件。");
            zh.Add("Maplinks", "地图坐标");
            zh.Add("Sender", "发送者");
            zh.Add("Timeout", "超时");
            zh.Add("Maplink within timeout will be filtered by it's maplink instead of full text.", "超时内的Maplink将由其Maplink而不是全文过滤。");
            zh.Add("Bring the game to front with new maplink", "收到 maplink 时激活游戏");
            zh.Add("Message", "消息");
            zh.Add("Time", "时间");
            zh.Add("Retrieve", "检索");
            zh.Add("Teleport", "传送");
            zh.Add("Delete", "删除");
            zh.Add("View", "查看");
            zh.Add("Tele", "传送");
            zh.Add("Del", "删除");
            zh.Add("Export", "导出");
            zh.Add("Clear", "清空");
            zh.Add("Max Records", "最多记录");
            zh.Add("Message Wrap", "消息换行");
            zh.Add("Line Wrap for message column.", "消息列的换行。");
            zh.Add("Target", "目标");
            //Additions
                zh.Add("Player White List", "玩家白名单");
                zh.Add("Prompt Teleport When Flag Is Relayed In Chat", "在聊天中转发标志时提示传送");
                zh.Add("Will create a popup asking if the player would like to be teleported when a flag is put into chat. You can choose to only have this happen on white listed players by selecting the 'Prompt for White List Players Only'", "将创建一个弹出窗口，询问玩家是否愿意在将旗帜放入聊天时被传送。您可以通过选择\"仅限白名单玩家的提示\"来选择仅在白名单玩家上执行此操作");
                zh.Add("Prompt for White List Players Only", "仅白名单玩家提示");
                zh.Add("This setting will only prompt teleport from flags put into chat by white listed players.", "此设置只会提示从白名单玩家放入聊天中的标志传送。");
                zh.Add("Add players to the white list from the zone you are in.", "将玩家从您所在的区域添加到白名单。");
                zh.Add("Player to Add", "要添加的播放器");
                zh.Add("Add To White List", "加入白名单");
                zh.Add("Home World", "家庭世界");
                zh.Add("It is faster to Fly", "飞得更快");
                zh.Add("It is faster to Teleport", "传送速度更快");
                zh.Add("", "");

        }
    }
}
