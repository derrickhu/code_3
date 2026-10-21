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

        void BuildGm(RectTransform tl)
        {
            if (!Gm.Allowed) return;
            var btn = Ui.CartoonBtn(tl, "GM", new Vector2(112, 60), new Vector2(80, -760), Ui.BtnRed, ShowGm, 34);
            btn.SetAsLastSibling();
        }

        public void ShowGm()
        {
            Panel("GM", out var card, new Vector2(820, 880));
            _gmStatus = Ui.Label(card, GmFrontier(), 40, Pal.Ink, new Vector2(0, 250), new Vector2(720, 70), TextAnchor.MiddleCenter, Fonts.Brush);
            _gmStatus.resizeTextForBestFit = true;
            _gmStatus.resizeTextMinSize = 28;
            _gmStatus.resizeTextMaxSize = 40;
            Ui.Label(card, "只在开发工具里出现。\n跳过按赢下这一回计，好汉照常入伙。", 30, Pal.InkSoft, new Vector2(0, 145), new Vector2(700, 100));
            Ui.CartoonBtn(card, "跳过本回", new Vector2(560, 116), new Vector2(0, -10), Ui.BtnRed, () => GmSkip(1), 46);
            Ui.CartoonBtn(card, "连跳五回", new Vector2(560, 116), new Vector2(0, -160), Ui.BtnOrange, () => GmSkip(5), 46);
            Ui.CartoonBtn(card, "关卡全开", new Vector2(560, 116), new Vector2(0, -310), Ui.BtnGreen, () => GmSkip(ChapterDef.StoryCount), 46, null, Pal.Ink);
        }

        public void GmSkip(int count)
        {
            if (Save.StoryDone)
            {
                Toast("蓼儿洼已过，聚义厅开着");
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

        static string GmFrontier()
        {
            if (Save.StoryDone) return "剧情已通 · 聚义厅已开";
            var ch = ChapterDef.All[Save.D.unlocked];
            return "下一回　第" + BattleView.ToCn(ch.Index + 1) + "回 · " + ch.Name;
        }
    }
}
