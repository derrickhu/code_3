using System;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>每日一战：第五回之后开放。今天人人同一张图、同一个头目，首胜给铜钱和一件兵器，不动座次。</summary>
    public sealed partial class HomeScreen
    {
        static readonly Color CellPath = Pal.Hex("B98E5E");
        static readonly Color CellTile = Pal.Hex("F4EBD6");
        static readonly Color CellGrass = Pal.Hex("8DB563");
        static readonly Color CellWater = Pal.Hex("7FB5D6");
        static readonly Color CellRock = Pal.Hex("8E8A86");
        static readonly Color CellMarsh = Pal.Hex("6E9E86");

        public void ShowDailyBattle()
        {
            if (!Save.DailyOpen)
            {
                Nope($"赢下第{BattleView.ToCn(Save.DailyUnlock)}回才开放每日一战，还差 {Save.DailyUnlock - Save.D.unlocked} 回");
                return;
            }
            var ch = ChapterDef.Daily(int.Parse(Save.Today));
            var layer = Panel("每日一战", out var card, new Vector2(920, 1420));
            var day = DateTime.ParseExact(Save.Today, "yyyyMMdd", null);
            Ui.Label(card, $"{day.Month}月{day.Day}日 · 今天人人同一阵", 34, Pal.InkSoft, new Vector2(0, 590), new Vector2(820, 50));
            var name = Ui.Label(card, ch.Name.Replace("每日一战·", ""), 96, Pal.Ink, new Vector2(0, 500), new Vector2(820, 120), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(name, Pal.Hex("FFF6E2"), 3f);

            MapPreview(card, ch, new Vector2(0, 270));

            string skills = ch.Boss.SkillText;
            Ui.Stroke(Ui.Label(card, $"头目 {ch.Boss.Title}{ch.Boss.Name}", 44, Pal.RedDeep, new Vector2(0, 60), new Vector2(820, 64), TextAnchor.MiddleCenter, Fonts.Brush), Pal.Hex("FFF6E2"), 2f);
            var sk = Ui.Label(card, skills, 28, Pal.Ink, new Vector2(0, -20), new Vector2(820, 90));
            sk.lineSpacing = 1.05f;
            var names = new List<string>();
            foreach (var id in ch.Pool) { var d = HeroDef.Get(id); if (!d.Hidden) names.Add(d.Name); }
            Ui.Label(card, $"克星 {HeroDef.Get(ch.Boss.Nemesis).Name}　本阵好汉 {string.Join(" ", names)}", 30, Pal.Gold, new Vector2(0, -100), new Vector2(840, 50));

            bool won = Save.DailyWonToday;
            var box = Ui.Framed(card, new Vector2(800, 200), new Vector2(0, -250), won ? Pal.Hex("E2D2AE") : Pal.Hex("F6E2B0"), won ? Pal.Hex("8C6B4E") : Pal.Hex("D9822B"), 5f, "reward");
            var bt = box.transform;
            if (won)
            {
                Ui.Stroke(Ui.Label(bt, "今天已经赢过", 50, Pal.Willow, new Vector2(0, 36), new Vector2(760, 70), TextAnchor.MiddleCenter, Fonts.Brush), Pal.Hex("FFF6E2"), 2f);
                Ui.Label(bt, "再打只当练手，赢了给 15 铜钱，明天换新阵", 30, Pal.InkSoft, new Vector2(0, -36), new Vector2(760, 50));
            }
            else
            {
                Ui.Stroke(Ui.Label(bt, "今日首胜", 40, Pal.Hex("B5651D"), new Vector2(-250, 40), new Vector2(260, 60), TextAnchor.MiddleCenter, Fonts.Brush), Pal.Hex("FFF6E2"), 2f);
                Ui.Icon(bt, "coin", 84, new Vector2(-300, -36));
                Ui.Stroke(Ui.Label(bt, "×" + Save.DailyCoinsReward, 48, Pal.Hex("F2B53A"), new Vector2(-180, -36), new Vector2(160, 70), TextAnchor.MiddleLeft), Edge, 2.5f).fontStyle = FontStyle.Bold;
                Ui.Icon(bt, Weapons.Icon(WeaponKind.Blade), 110, new Vector2(80, 0));
                Ui.Label(bt, "必掉一件\n入伙好汉的兵器", 30, Pal.Ink, new Vector2(250, 0), new Vector2(260, 100));
            }
            Ui.Label(card, "不动座次，输了也不掉星", 30, Pal.InkSoft, new Vector2(0, -385), new Vector2(800, 46));

            var go = Ui.CartoonBtn(card, "出  战", new Vector2(460, 132), new Vector2(-30, -520), Ui.BtnRed, () => Fight(ChapterDef.DailySlot), 64);
            Ui.Icon(go, "ui_bolt", 56, new Vector2(170, 0));
            Ui.Label(go, "-" + Stamina.Cost, 26, Pal.Hex("FFE7A8"), new Vector2(204, -26), new Vector2(60, 36));
            if (!won) Breathe(go);
        }

        /// <summary>今天这张地图的小样：路、白地、草地、水、石头一眼看清。</summary>
        static void MapPreview(RectTransform parent, ChapterDef ch, Vector2 center)
        {
            const float cell = 58f, gap = 4f;
            var board = new Board(ch.Map);
            float w = Board.Cols * (cell + gap) + gap, h = Board.Rows * (cell + gap) + gap;
            var frame = Ui.Framed(parent, new Vector2(w + 16, h + 16), center, Pal.Hex("5E4532"), Edge, 5f, "mapFrame");
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    var kind = board.Cells[c, r];
                    Color col = kind switch
                    {
                        Cell.Path => CellPath,
                        Cell.Tile => CellTile,
                        Cell.Water => CellWater,
                        Cell.Rock => CellRock,
                        Cell.Marsh => CellMarsh,
                        _ => CellGrass,
                    };
                    var pos = new Vector2(-w / 2 + gap + c * (cell + gap) + cell / 2, -h / 2 + gap + r * (cell + gap) + cell / 2);
                    var img = Ui.Box(frame.transform, new Vector2(cell, cell), pos, col, null, "cell");
                    img.pixelsPerUnitMultiplier = 2f;
                    var at = new CellPos(c, r);
                    if (at.Equals(board.Spawn)) Ui.Label(img.transform, "兵", 36, Pal.Paper, Vector2.zero, new Vector2(cell, cell), TextAnchor.MiddleCenter, Fonts.Brush);
                    else if (at.Equals(board.Goal)) Ui.Label(img.transform, "宋", 40, Pal.RedDeep, Vector2.zero, new Vector2(cell, cell), TextAnchor.MiddleCenter, Fonts.Brush);
                }
        }
    }
}
