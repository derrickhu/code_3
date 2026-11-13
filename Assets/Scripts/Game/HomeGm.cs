using System;
using Rescue.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>开发工具里的 GM 开关。真机不出现，跟 Black Rosa 一样只认编辑器和微信模拟器。</summary>
    public static class Gm
    {
        /// <summary>截图验收强制打开。正式包不要设。</summary>
        public static bool Force;

        public static bool Allowed
        {
            get
            {
                if (Force || Application.isEditor) return true;
                try
                {
                    var wx = Type.GetType("WeChatWASM.WX, Wx") ?? Type.GetType("WeChatWASM.WX");
                    var info = wx?.GetMethod("GetSystemInfoSync", Type.EmptyTypes)?.Invoke(null, null);
                    if (info == null) return false;
                    var t = info.GetType();
                    object raw = t.GetField("platform")?.GetValue(info) ?? t.GetProperty("platform")?.GetValue(info);
                    return raw is string name && name.Equals("devtools", StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }

    /// <summary>主界面 GM：跳过还没打的剧情回。</summary>
    public sealed partial class HomeScreen
    {
        Text _gmStatus;

        void BuildGm(RectTransform tr)
        {
            if (!Gm.Allowed) return;
            var btn = Ui.CartoonBtn(tr, "GM", new Vector2(112, 60), new Vector2(-92, -560), Ui.BtnRed, ShowGm, 34);
            btn.SetAsLastSibling();
        }

        public void ShowGm()
        {
            Panel("GM", out var card, new Vector2(820, 1120));
            _gmStatus = Ui.Label(card, GmFrontier(), 40, Pal.Ink, new Vector2(0, 410), new Vector2(720, 70), TextAnchor.MiddleCenter, Fonts.Brush);
            _gmStatus.resizeTextForBestFit = true;
            _gmStatus.resizeTextMinSize = 28;
            _gmStatus.resizeTextMaxSize = 40;
            bool hall = Save.StoryDone;
            Ui.Label(card, hall
                ? "只在开发工具里出现。\n聚义厅按赢下一阵计。关卡全开跳到第二十一阵。"
                : "只在开发工具里出现。\n跳过按赢下这一回计，好汉照常入伙。", 28, Pal.InkSoft, new Vector2(0, 305), new Vector2(700, 90));
            Ui.CartoonBtn(card, hall ? "跳过本阵" : "跳过本回", new Vector2(560, 102), new Vector2(0, 175), Ui.BtnRed, () => GmSkip(1), 44);
            Ui.CartoonBtn(card, hall ? "连跳五阵" : "连跳五回", new Vector2(560, 102), new Vector2(0, 52), Ui.BtnOrange, () => GmSkip(5), 44);
            Ui.CartoonBtn(card, "关卡全开", new Vector2(560, 102), new Vector2(0, -71), Ui.BtnGreen, () => GmSkip(ChapterDef.StoryCount), 44, null, Pal.Ink);
            Ui.CartoonBtn(card, "发道具+模拟跨零点", new Vector2(560, 102), new Vector2(0, -194), Ui.BtnYellow, GmTestRollover, 40, null, Pal.Ink);
            Ui.CartoonBtn(card, "清档", new Vector2(560, 102), new Vector2(0, -317), Ui.BtnDark, () =>
            {
                Confirm("清档", "本地和云端都换成新档\n铜钱、进度、兵器都没了，不能恢复", "清档", WipeAll);
            }, 44);
        }

        void GmTestRollover()
        {
            // 先给满几件主被动（含招贤酒、十字坡灶、萧让代笔、蒙汗药），再模拟过零点结算
            ShopItem.All[12].Give(); // 十字坡灶
            ShopItem.All[13].Give(); // 招贤酒
            ShopItem.All[0].Give();  // 生辰纲
            ShopItem.All[14].Give(); // 萧让代笔
            ShopItem.All[4].Give();  // 蒙汗药
            Save.RolloverItemsForNewDay();
            Save.ConsumeCarryNotice(out var kept, out var coins);
            Save.Write();
            Refresh();
            Ui.Toast($"模拟跨零点完成：保留「{kept}」，旧货折银 +{coins}！", -640f);
        }

        void WipeAll()
        {
            Save.Wipe();
            CloudSync.FlushNow("wipe");
            LiveConfig.SubmitRank(true);
            _selected = 0;
            Refresh();
            if (_gmStatus) _gmStatus.text = GmFrontier();
            Ui.Toast("已换成新档，云端一起盖掉", -640f);
        }

        /// <summary>聚义厅难度到第二十一阵封顶，再往后只是阵数变大。</summary>
        const int EndlessCap = 21;

        public void GmSkip(int count)
        {
            if (Save.StoryDone)
            {
                SkipHall(count);
                return;
            }
            int before = Save.JoinedCount;
            int n = Save.SkipStory(count);
            if (n <= 0) return;
            Save.Write();
            _selected = Mathf.Clamp(Save.D.unlocked, 0, ChapterDef.StoryCount);
            Refresh();
            if (_gmStatus) _gmStatus.text = GmFrontier();
            int fresh = Save.JoinedCount - before;
            string where = Save.StoryDone ? "聚义厅已开" : "现在第" + BattleView.ToCn(Save.D.unlocked + 1) + "回";
            // 默认提示条正好压在最下面的按钮上，放到面板下方。
            Ui.Toast(fresh > 0 ? $"跳过 {n} 回，{fresh} 人入伙，{where}" : $"跳过 {n} 回，{where}", -640f);
        }

        void SkipHall(int count)
        {
            int n = count;
            if (count >= ChapterDef.StoryCount)
            {
                if (Save.D.endlessRound >= EndlessCap)
                {
                    Toast("已经是最难的一阵");
                    return;
                }
                n = EndlessCap - Save.D.endlessRound;
            }
            Save.D.endlessBest = Mathf.Max(Save.D.endlessBest, Save.D.endlessRound + n - 1);
            Save.D.endlessRound += n;
            Save.Write();
            _selected = ChapterDef.StoryCount;
            Refresh();
            if (_gmStatus) _gmStatus.text = GmFrontier();
            Ui.Toast($"跳过 {n} 阵，现在第{BattleView.ToCn(Save.D.endlessRound)}阵", -640f);
        }

        static string GmFrontier()
        {
            if (Save.StoryDone) return "聚义厅 · 第" + BattleView.ToCn(Save.D.endlessRound) + "阵";
            var ch = ChapterDef.All[Save.D.unlocked];
            return "下一回　第" + BattleView.ToCn(ch.Index + 1) + "回 · " + ch.Name;
        }
    }
}
