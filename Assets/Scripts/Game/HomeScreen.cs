using System;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>神秘商人（孙二娘）的货：主动道具战斗里拖上战场放出，被动道具开局就生效。</summary>
    public sealed class ShopItem
    {
        public int Id;
        public string Name, Desc, Rarity, Icon;
        public bool Active;
        public int Price;
        public Func<int> Count;
        public Action Give, Take;

        public bool Owned() => Count() >= (Id == 8 ? 2 : 1);

        public static readonly string[] Rarities = { "稀有", "卓越", "史诗", "传说" };

        /// <summary>稀有绿、卓越蓝、史诗紫、传说橙，和圆底同色系。</summary>
        public static Color ColorOf(string rarity) => rarity switch
        {
            "传说" => Pal.Hex("F08A24"),
            "史诗" => Pal.Hex("B45CE0"),
            "卓越" => Pal.Hex("3FA9E0"),
            _ => Pal.Hex("6FBF3A"),
        };

        public Color RarityColor => ColorOf(Rarity);
        public Color DiscColor => Rarity switch
        {
            "传说" => Pal.Hex("F6C46B"),
            "史诗" => Pal.Hex("C9A6EE"),
            "卓越" => Pal.Hex("A4D8F2"),
            _ => Pal.Hex("C5E08A"),
        };

        /// <summary>抽奖权重：越稀有越难出。</summary>
        public int Weight => Rarity switch { "传说" => 12, "史诗" => 22, "卓越" => 30, _ => 36 };

        static int B(bool v) => v ? 1 : 0;

        public static readonly ShopItem[] All =
        {
            new ShopItem { Id = 0, Name = "生辰纲", Desc = "一进场多30个炊饼，够先多招几回兵", Rarity = "卓越", Icon = "it_gang", Price = Economy.ItemPrices[0], Count = () => B(Save.D.gang), Give = () => { Save.D.gang = true; Save.NotePassive(0); }, Take = () => { Save.D.gang = false; Save.NotePassive(0); } },
            new ShopItem { Id = 1, Name = "替天行道旗", Desc = "你的兵和好汉伤害高两成，整场都算", Rarity = "史诗", Icon = "it_flag", Price = Economy.ItemPrices[1], Count = () => B(Save.D.flag), Give = () => { Save.D.flag = true; Save.NotePassive(1); }, Take = () => { Save.D.flag = false; Save.NotePassive(1); } },
            new ShopItem { Id = 2, Name = "水泊", Desc = "官兵走得慢四分之一，整场都慢", Rarity = "史诗", Icon = "it_marsh", Price = Economy.ItemPrices[2], Count = () => B(Save.D.marsh), Give = () => { Save.D.marsh = true; Save.NotePassive(2); }, Take = () => { Save.D.marsh = false; Save.NotePassive(2); } },
            new ShopItem { Id = 3, Name = "石碣", Desc = "官兵走近宋江，就砸下石碣，旁边的一起伤", Rarity = "传说", Icon = "it_stele", Price = Economy.ItemPrices[3], Count = () => B(Save.D.stele), Give = () => { Save.D.stele = true; Save.NotePassive(3); }, Take = () => { Save.D.stele = false; Save.NotePassive(3); } },
            new ShopItem { Id = 4, Name = "蒙汗药", Desc = "点一下，官兵停住3秒，不走也不打", Rarity = "史诗", Icon = "it_menghan", Active = true, Price = Economy.ItemPrices[4], Count = () => Save.D.menghan, Give = () => { Save.D.menghan++; Save.NoteActive(4); }, Take = () => { Save.D.menghan = Mathf.Max(0, Save.D.menghan - 1); Save.NoteActive(4); } },
            new ShopItem { Id = 5, Name = "天书", Desc = "拖到兵或灶上升一级，满级的不行", Rarity = "传说", Icon = "it_tianshu", Active = true, Price = Economy.ItemPrices[5], Count = () => Save.D.tianshu, Give = () => { Save.D.tianshu++; Save.NoteActive(5); }, Take = () => { Save.D.tianshu = Mathf.Max(0, Save.D.tianshu - 1); Save.NoteActive(5); } },
            new ShopItem { Id = 6, Name = "孙二娘包子", Desc = "多半给宋江加一颗心，不成就掉一颗", Rarity = "卓越", Icon = "bun", Active = true, Price = Economy.ItemPrices[6], Count = () => Save.D.baozi, Give = () => { Save.D.baozi++; Save.NoteActive(6); }, Take = () => { Save.D.baozi = Mathf.Max(0, Save.D.baozi - 1); Save.NoteActive(6); } },
            new ShopItem { Id = 7, Name = "续命丹", Desc = "宋江开局多八颗心，更能扛漏进来的兵", Rarity = "传说", Icon = "it_xuming", Price = Economy.ItemPrices[7], Count = () => B(Save.D.xuming), Give = () => { Save.D.xuming = true; Save.NotePassive(7); }, Take = () => { Save.D.xuming = false; Save.NotePassive(7); } },
            new ShopItem { Id = 8, Name = "方便铲", Desc = "开局多两把铲子，拖到草地上挖出空位", Rarity = "稀有", Icon = "shovel", Price = Economy.ItemPrices[8], Count = () => Save.D.shovels, Give = () => { Save.D.shovels = 2; Save.NotePassive(8); }, Take = () => { Save.D.shovels = 0; Save.NotePassive(8); } },
            new ShopItem { Id = 9, Name = "攻速符", Desc = "点一下，出手快四成，管十二秒，满级也算", Rarity = "史诗", Icon = "it_fu", Active = true, Price = Economy.ItemPrices[9], Count = () => Save.D.fu, Give = () => { Save.D.fu++; Save.NoteActive(9); }, Take = () => { Save.D.fu = Mathf.Max(0, Save.D.fu - 1); Save.NoteActive(9); } },
            new ShopItem { Id = 10, Name = "御敌千里", Desc = "弓和枪射程翻倍，刀和马不加，整场都算", Rarity = "稀有", Icon = "it_far", Price = Economy.ItemPrices[10], Count = () => B(Save.D.far), Give = () => { Save.D.far = true; Save.NotePassive(10); }, Take = () => { Save.D.far = false; Save.NotePassive(10); } },
            new ShopItem { Id = 11, Name = "陷阱", Desc = "点一下，官兵被推回去，还得愣一会儿", Rarity = "稀有", Icon = "it_trap", Active = true, Price = Economy.ItemPrices[11], Count = () => Save.D.trap, Give = () => { Save.D.trap++; Save.NoteActive(11); }, Take = () => { Save.D.trap = Mathf.Max(0, Save.D.trap - 1); Save.NoteActive(11); } },
            new ShopItem { Id = 12, Name = "十字坡灶", Desc = "开局占一块空地自己出炊饼，天书能升灶", Rarity = "卓越", Icon = "it_stove", Price = Economy.ItemPrices[12], Count = () => B(Save.D.stove), Give = () => { Save.D.stove = true; Save.NotePassive(12); }, Take = () => { Save.D.stove = false; Save.NotePassive(12); } },
            new ShopItem { Id = 13, Name = "招贤酒", Desc = "聚义时更容易出好汉的字，小兵还是多", Rarity = "史诗", Icon = "it_wine", Price = Economy.ItemPrices[13], Count = () => B(Save.D.wine), Give = () => { Save.D.wine = true; Save.NotePassive(13); }, Take = () => { Save.D.wine = false; Save.NotePassive(13); } },
            new ShopItem { Id = 14, Name = "萧让代笔", Desc = "拖到单字上补缺的字，拖到兵上把兵变成字", Rarity = "史诗", Icon = "wp_fan", Active = true, Price = Economy.ItemPrices[14], Count = () => Save.D.pen, Give = () => { Save.D.pen++; Save.NoteActive(14); }, Take = () => { Save.D.pen = Mathf.Max(0, Save.D.pen - 1); Save.NoteActive(14); } },
        };
    }

    /// <summary>山寨主界面：座次、章节、出征、好汉谱、十字坡酒店。</summary>
    public sealed partial class HomeScreen : MonoBehaviour
    {
        RectTransform _root;
        Text _coins, _stamina, _loadout;
        int _selected;
        Text _chapterName, _chapterInfo, _rankName, _stars;
        RectTransform _badge, _badgeHolder;
        int _badgeTier = -1;
        RectTransform _shop;
        Text _shopCoins, _shopStamina;
        Action<int> _onFight;
        Image _hudFace, _profileBar;
        Text _profileHint;
        RectTransform _avatarHit, _authHit;
        bool _homeArmed, _boardOwns;
        float _armAt;
        Action _reloadBoard;

        public static HomeScreen Open(Transform host, Action<int> onFight)
        {
            var go = new GameObject("Home");
            go.transform.SetParent(host, false);
            var h = go.AddComponent<HomeScreen>();
            h._onFight = onFight;
            h.Build();
            return h;
        }

        void OnDestroy()
        {
            _reloadBoard = null;
            _authHit = null;
            WxSession.HideProfileButton();
            WxSession.CloseGameClub();
            if (_root) Destroy(_root.gameObject);
        }

        static readonly Color Edge = Pal.Hex("2E2119");
        static readonly Color StarOn = Pal.Hex("FFD84A");
        static readonly Color StarOff = Pal.Hex("6E5D45");

        RectTransform Corner(float x, float y, string name)
        {
            var rt = Ui.Rect(_root, name);
            Ui.Anchor(rt, x, y);
            return rt;
        }

        void Build()
        {
            _root = Ui.Rect(Ui.Root, "Home");
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = _root.offsetMax = Vector2.zero;

            Save.RefreshShopDay();
            PageBg(_root, 0f).SetAsFirstSibling();

            var tl = Corner(0f, 1f, "topLeft");
            BuildHud(tl, out _coins, out _stamina, true);

            MedalBtn(tl, "音", null, new Vector2(80, -226), 92, ToggleSound);
            MedalBtn(tl, "告", "公告", new Vector2(80, -372), 108, ShowNotice);
            _codexDot = MedalBtn(tl, "谱", "好汉谱", new Vector2(80, -534), 108, ShowCodex);
            _chaptersDot = MedalBtn(tl, "回", "回目讲堂", new Vector2(80, -696), 108, ShowChapters);
            _dailyDot = MedalBtn(tl, "战", "每日一战", new Vector2(80, -858), 108, ShowDailyBattle);
            if (!Save.DailyOpen) _dailyDot.transform.parent.GetComponent<Image>().color = new Color(0.62f, 0.6f, 0.58f);
            if (WxSession.InWeChat)
            {
                // 和上面几颗同一间距：圆心每隔 162。点按区域另算，把名字牌也包进去，不把圆心往上抬。
                const float clubY = -1020f;
                MedalBtn(tl, "圈", "游戏圈", new Vector2(80, clubY), 108, () =>
                {
                    if (WxSession.Devtools) Toast("开发者工具里打不开游戏圈，请用手机预览");
                    else if (!WxSession.GameClubReady) Toast("游戏圈暂时打不开，退出重进再试");
                    else Toast("没点到游戏圈按钮，退出重进再试");
                });
                _clubHit = Ui.Rect(tl, "gameClub");
                _clubHit.sizeDelta = new Vector2(168, 156);
                _clubHit.anchoredPosition = new Vector2(80, clubY - 16f);
            }

            var tr = Corner(1f, 1f, "topRight");
            Ui.Medal(tr, 128, new Vector2(-88, -250), "shopDisc");
            var shopFace = Ui.Icon(tr, "merchant_bust", 150, new Vector2(-88, -236));
            Ui.Tap(shopFace, ShowShop);
            MedalLabel(tr, "神秘商人", new Vector2(-88, -332));
            BuildGm(tr);

            var top = Corner(0.5f, 1f, "title");
            var logo = Ui.Icon(top, "home_logo", 760, new Vector2(0, -322), "logo");
            Tween.Pop(logo.transform, 0.5f);
            BuildRankPlate(top, new Vector2(0, -548));

            var hero = Corner(0.5f, 0.5f, "hero");
            hero.SetSiblingIndex(1);
            BuildHero(hero);

            var bottom = Corner(0.5f, 0f, "bottom");
            _loadout = Ui.Label(bottom, "", 30, Ui.GoldText, new Vector2(0, 556), new Vector2(940, 46));
            Ui.Stroke(_loadout, Pal.Hex("120B07"), 2f);
            BuildStart(bottom);

            SideBigIcon(Corner(0f, 0f, "rankEntry"), "榜", "排行榜", new Vector2(130, 200), ShowBoard);
            SideBigIcon(Corner(1f, 0f, "bagEntry"), "兵", "武器背包", new Vector2(-130, 200), ShowArmory);

            Refresh();
            LiveConfig.Refresh();
            LiveConfig.SubmitRank();
            if (!Save.Volatile && Save.ConsumeCarryNotice(out var keptName, out var carryCoins))
            {
                string msg = keptName.Length > 0 && carryCoins > 0 ? $"跨日护航：保留「{keptName}」，余货折铜钱 +{carryCoins}"
                    : keptName.Length > 0 ? $"跨日护航：压寨底牌「{keptName}」继续随行"
                    : $"昨日余货折算铜钱 +{carryCoins}";
                Toast(msg);
            }
            if (!Save.Volatile && Save.RollDaily()) ShowDaily();
        }

        /// <summary>整页底图：梁山水墨，等比铺满到屏幕边，dim 是压在上面的暗度。</summary>
        static RectTransform PageBg(RectTransform parent, float dim)
        {
            var bg = Ui.Rect(parent, "bg");
            Ui.PinBleed(bg);
            var under = bg.gameObject.AddComponent<Image>();
            under.color = Pal.Hex("1A110B");
            under.raycastTarget = false;
            var art = Ui.Rect(bg, "art");
            var img = art.gameObject.AddComponent<Image>();
            img.sprite = Art.Img("home_bg");
            img.raycastTarget = false;
            var fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = Ui.DesignW / Ui.DesignH;
            if (dim > 0f)
            {
                var shade = Ui.Rect(bg, "dim");
                shade.anchorMin = Vector2.zero;
                shade.anchorMax = Vector2.one;
                shade.offsetMin = shade.offsetMax = Vector2.zero;
                var s = shade.gameObject.AddComponent<Image>();
                s.color = Pal.Hex("120B07").A(dim);
                s.raycastTarget = false;
            }
            return bg;
        }

        /// <summary>标题下的座次牌：左嵌勋章，中写座次，右排星；整块点开排行榜。</summary>
        void BuildRankPlate(RectTransform parent, Vector2 pos)
        {
            const float w = 600f, h = 84f;
            var plate = Ui.Rect(parent, "rankPlate");
            plate.anchoredPosition = pos;
            plate.sizeDelta = new Vector2(w, h);
            Ui.Box(plate, new Vector2(w + 8, h + 8), new Vector2(0, -7), Pal.Shadow.A(0.6f), null, "shadow");
            var face = Ui.Skin(plate, Ui.SkinSlot, new Vector2(w, h), Vector2.zero, Color.white, "face");
            face.raycastTarget = true;
            Ui.Tap(face, ShowBoard);

            _rankName = Ui.Label(plate, "", 40, Ui.GoldText, new Vector2(-w / 2 + 92 + 130, 0), new Vector2(260, h), TextAnchor.MiddleLeft, Fonts.Brush);
            _rankName.resizeTextForBestFit = true;
            _rankName.resizeTextMinSize = 28;
            _rankName.resizeTextMaxSize = 40;
            Ui.Stroke(_rankName, Edge, 2f);
            _stars = Ui.Label(plate, "", 50, StarOn, new Vector2(w / 2 - 26 - 140, -2), new Vector2(280, h), TextAnchor.MiddleRight);
            _stars.supportRichText = true;
            Ui.Stroke(_stars, Edge, 2.5f);

            _badgeHolder = Ui.Rect(plate, "badge");
            _badgeHolder.anchoredPosition = new Vector2(-w / 2 + 18, 4);
        }

        const float HudY = -92f;

        /// <summary>顶栏一行：头像、铜钱条、体力条（绿加号看广告补体力）。商人界面也复用。</summary>
        void BuildHud(RectTransform tl, out Text coins, out Text stamina, bool avatar)
        {
            if (avatar)
            {
                var face = CirclePortrait(tl, WxSession.Avatar, new Vector2(80, HudY), 118f, out var hit);
                if (tl.name == "topLeft")
                {
                    _hudFace = face;
                    _avatarHit = hit;
                    var mask = hit.Find("avatarMask");
                    _profileBar = Ui.Box(mask, new Vector2(118, 36), new Vector2(0, -40), new Color(0.12f, 0.07f, 0.04f, 0.82f), null, "nameBar");
                    var hint = Ui.Label(mask, "点我授权", 22, Pal.Hex("FFE7A8"), new Vector2(0, -40), new Vector2(110, 32));
                    hint.resizeTextForBestFit = true;
                    hint.resizeTextMinSize = 14;
                    hint.resizeTextMaxSize = 22;
                    hint.horizontalOverflow = HorizontalWrapMode.Overflow;
                    hint.verticalOverflow = VerticalWrapMode.Truncate;
                    Ui.Stroke(hint, Edge, 1.5f);
                    _profileHint = hint;
                    Ui.Tap(face, OnHomeAvatar);
                }
                else Ui.Tap(face, () => Toast(ProfileLine()));
            }
            float x = avatar ? 186 : 40;
            HudPill(tl, x, 250, "coinBar");
            Ui.Icon(tl, "coin", 92, new Vector2(x + 14, HudY));
            coins = Ui.Label(tl, "", 44, Pal.Hex("FFE7A8"), new Vector2(x + 145, HudY), new Vector2(180, 76));
            coins.fontStyle = FontStyle.Bold;

            x += 280;
            HudPill(tl, x, 330, "staBar");
            Ui.Icon(tl, "ui_bolt", 72, new Vector2(x + 12, HudY));
            stamina = Ui.Label(tl, "", 38, Pal.Hex("FFE7A8"), new Vector2(x + 152, HudY), new Vector2(230, 76));
            stamina.fontStyle = FontStyle.Bold;
            stamina.supportRichText = true;
            var plus = Ui.Icon(tl, "ui_plus", 62, new Vector2(x + 316, HudY));
            Ui.Tap(plus, () =>
            {
                if (Stamina.Current >= Stamina.Max) { Nope("体力已满"); return; }
                AdStub.Show("补充体力", () => { Stamina.Add(5); Refresh(); Toast("体力 +5"); });
            });
            AdStub.Badge(tl, new Vector2(x + 316, HudY - 50), "补充体力");
        }

        /// <summary>顶栏的深色圆角条，和座次牌同一套描边。left 是条的左端。</summary>
        static void HudPill(RectTransform parent, float left, float w, string name)
        {
            const float h = 72f;
            var c = new Vector2(left + w / 2, HudY);
            Ui.Skin(parent, Ui.SkinSlot, new Vector2(w, h), c, Color.white, name);
        }

        /// <summary>金边黑漆圆章入口：章心一个金字，下面挂一块小漆牌写全名。返回右上角的小红点，默认藏着。</summary>
        Image MedalBtn(RectTransform parent, string glyph, string label, Vector2 pos, float size, Action onClick)
        {
            var medal = Ui.Medal(parent, size, pos);
            Ui.Tap(medal, onClick);
            var g = Ui.Label(medal.transform, glyph, Mathf.RoundToInt(size * 0.5f), Ui.GoldText, new Vector2(0, 2), Vector2.one * size, TextAnchor.MiddleCenter, Fonts.Brush, "glyph");
            g.gameObject.AddComponent<Shadow>().effectColor = Color.black.A(0.6f);
            if (label != null) MedalLabel(medal.transform, label, new Vector2(0, -size * 0.5f - 4));
            var dot = Ui.Box(medal.transform, new Vector2(30, 30), new Vector2(size * 0.36f, size * 0.36f), Pal.Hex("E8392B"), Art.Circle, "redDot");
            dot.type = Image.Type.Simple;
            dot.gameObject.SetActive(false);
            return dot;
        }

        static void MedalLabel(Transform parent, string text, Vector2 pos, int font = 28)
        {
            if (text.Length > 3) font = Mathf.Min(font, 24);
            float w = text.Length * font + 26;
            Ui.Skin(parent, Ui.SkinSlot, new Vector2(w, font + 16), pos, Color.white, "labelPlate", 46f / ((font + 16) * 0.5f));
            Ui.Label(parent, text, font, Ui.GoldText, pos + new Vector2(0, 1), new Vector2(w, font + 16), TextAnchor.MiddleCenter, Fonts.Brush, "labelText");
        }

        Image _codexDot, _chaptersDot, _dailyDot;
        RectTransform _clubHit;

        void SideBigIcon(RectTransform parent, string glyph, string text, Vector2 pos, Action onClick)
        {
            var medal = Ui.Medal(parent, 150, pos);
            Ui.Tap(medal, onClick);
            var g = Ui.Label(medal.transform, glyph, 78, Ui.GoldText, new Vector2(0, 3), new Vector2(150, 150), TextAnchor.MiddleCenter, Fonts.Brush, "glyph");
            g.gameObject.AddComponent<Shadow>().effectColor = Color.black.A(0.6f);
            MedalLabel(medal.transform, text, new Vector2(0, -84), 32);
        }

        /// <summary>中央：梁山兄弟劫囚车救宋江的插画，站在空地上；下方左右切换回目。</summary>
        void BuildHero(RectTransform c)
        {
            c.anchoredPosition = new Vector2(0, 40);
            var glow = Ui.Box(c, new Vector2(1000, 600), new Vector2(0, 40), Pal.Hex("FFE9BE").A(0.28f), Art.SoftDot, "heroGlow");
            glow.type = Image.Type.Simple;
            var shadow = Ui.Box(c, new Vector2(760, 90), new Vector2(0, -190), new Color(0.1f, 0.06f, 0.03f, 0.45f), Art.SoftDot, "heroShadow");
            shadow.type = Image.Type.Simple;
            var art = Ui.Icon(c, "home_hero", 860, new Vector2(10, 20), "heroArt");
            art.rectTransform.sizeDelta = new Vector2(860, 473);
            Bob(art.transform, 0f);

            _selected = Mathf.Clamp(Save.D.unlocked, 0, ChapterDef.StoryCount);
            var strip = Ui.Skin(c, Ui.SkinSlot, new Vector2(580, 96), new Vector2(0, -300), Color.white, "chapterStrip");
            _chapterName = Ui.Label(strip.transform, "", 46, Ui.GoldText, new Vector2(0, 2), new Vector2(460, 88), TextAnchor.MiddleCenter, Fonts.Brush);
            _chapterName.resizeTextForBestFit = true;
            _chapterName.resizeTextMinSize = 26;
            _chapterName.resizeTextMaxSize = 46;
            _chapterName.verticalOverflow = VerticalWrapMode.Truncate;
            Ui.Tap(strip, () => { if (_selected >= ChapterDef.StoryCount) ShowTeam(); else ShowChapters(); });
            _chapterInfo = Ui.Label(c, "", 29, Pal.Paper, new Vector2(0, -376), new Vector2(860, 44));
            Ui.Stroke(_chapterInfo, Pal.Hex("120B07"), 2f);
            ArrowBtn(c, "◀", new Vector2(-350, -300), -1);
            ArrowBtn(c, "▶", new Vector2(350, -300), 1);
        }

        void ArrowBtn(RectTransform parent, string glyph, Vector2 pos, int dir)
        {
            var b = Ui.Medal(parent, 92, pos, "arrow");
            Ui.Label(b.transform, glyph, 36, Ui.GoldText, new Vector2(dir * 3, 0), new Vector2(92, 92));
            Ui.Tap(b, () => SelectChapter(_selected + dir));
        }

        void SelectChapter(int i)
        {
            if (i < 0 || i > ChapterDef.StoryCount) return;
            if (i > Save.D.unlocked) { Nope(i >= ChapterDef.StoryCount ? "打完蓼儿洼，聚义厅才开张" : "先赢下上一回才能解锁"); return; }
            _selected = i;
            RefreshChapter();
            Tween.Punch(_chapterName.transform, 0.15f);
        }

        void RefreshChapter()
        {
            if (_selected >= ChapterDef.StoryCount)
            {
                _chapterName.text = $"聚义厅 · 第{BattleView.ToCn(Save.D.endlessRound)}阵";
                var names = new List<string>();
                foreach (var id in Save.Team()) names.Add(HeroDef.Get(id).Name);
                _chapterInfo.text = $"阵容 {string.Join(" ", names)}　最好 {Save.D.endlessBest} 阵　点此换将";
                return;
            }
            var ch = ChapterDef.All[_selected];
            _chapterName.text = "第" + BattleView.ToCn(ch.Index + 1) + "回 · " + ch.Name;
            var nem = HeroDef.Get(ch.Boss.Nemesis).Name;
            bool fresh = ch.Joins.Length > 0 && !Save.Joined(ch.Joins[0]);
            _chapterInfo.text = fresh
                ? $"{ch.Act}　头目 {ch.Boss.Name}　克星 {nem}　胜后 {ch.Joins.Length} 人入伙"
                : $"{ch.Act}　头目 {ch.Boss.Name}　克星 {nem}　对手 {ch.RivalName}";
        }

        void BuildStart(RectTransform bottom)
        {
            var holder = Ui.Rect(bottom, "start");
            holder.anchoredPosition = new Vector2(0, 430);
            holder.sizeDelta = new Vector2(600, 150);
            var glow = Ui.Box(holder, new Vector2(860, 300), Vector2.zero, Pal.Hex("E8573F").A(0.32f), Art.SoftDot, "glow");
            glow.type = Image.Type.Simple;
            Ui.Box(holder, new Vector2(610, 156), new Vector2(0, -10), Pal.Shadow.A(0.7f), null, "shadow");
            var body = Ui.Rect(holder, "body");
            body.sizeDelta = holder.sizeDelta;
            Ui.Skin(body, Ui.SkinBtn, new Vector2(600, 150), Vector2.zero, Pal.Hex("C23A2A"), "face", 1.2f);
            Ui.Skin(body, Ui.SkinRim, new Vector2(600, 150), Vector2.zero, Color.white, "rim", 1.2f);
            foreach (float side in new[] { -1f, 1f })
            {
                var d = Ui.Box(body, new Vector2(30, 30), new Vector2(side * 252, 0), Ui.GoldText, Art.White, "diamond");
                d.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var dd = Ui.Box(body, new Vector2(12, 12), new Vector2(side * 252, 0), Pal.Hex("6B1A12"), Art.White, "diamondHole");
                dd.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            var label = Ui.Label(body, "开始游戏", 70, Pal.Paper, new Vector2(-28, 2), new Vector2(400, 120), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(label, Pal.Hex("5A140C"), 3f);
            Ui.Icon(body, "ui_bolt", 56, new Vector2(168, 4));
            Ui.Stroke(Ui.Label(body, "-" + Stamina.Cost, 28, Ui.GoldText, new Vector2(196, -26), new Vector2(60, 40)), Pal.Hex("5A140C"), 1.5f);
            var hit = Ui.Box(holder, new Vector2(600, 150), Vector2.zero, new Color(0, 0, 0, 0), null, "hit");
            Ui.Tap(hit, () => Fight(), body, 10f);
            Breathe(holder);
        }

        static void Bob(Transform t, float phase)
        {
            var basePos = ((RectTransform)t).anchoredPosition;
            Tween.Run(t, 1.6f, k => { if (t) ((RectTransform)t).anchoredPosition = basePos + new Vector2(0, Mathf.Sin((k + phase) * Mathf.PI * 2) * 6f); },
                () => { if (t) { ((RectTransform)t).anchoredPosition = basePos; Bob(t, phase); } });
        }

        static void Breathe(Transform t)
        {
            Tween.Run(t, 1.4f, k => { if (t) t.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI * 2) * 0.03f); }, () => { if (t) Breathe(t); });
        }

        void OnHomeAvatar()
        {
            if (WxSession.CanAskProfile && !LiveConfig.HasNick)
            {
                _homeArmed = false;
                WxSession.AllowPrivacyRetry();
                if (WxSession.PrivacyAgreed) ArmHome();
                else WxSession.ShowPrivacy(ok => { if (ok) ArmHome(); }, true);
                return;
            }
            Toast(ProfileLine());
        }

        void ArmHome()
        {
            if (_homeArmed || _boardOwns || !_avatarHit) return;
            if (!WxSession.CanAskProfile || !WxSession.PrivacyAgreed || LiveConfig.HasNick) return;
            if (WxSession.PrivacyShowing || AvatarBlocked()) return;
            _homeArmed = true;
            _armAt = Time.unscaledTime;
            WxSession.Cover(this, _avatarHit, OnProfile);
        }

        void SyncProfileButton()
        {
            if (_boardOwns) return;
            if (!WxSession.CanAskProfile || LiveConfig.HasNick)
            {
                if (_homeArmed)
                {
                    _homeArmed = false;
                    WxSession.HideProfileButton();
                }
                return;
            }
            if (WxSession.PrivacyShowing || AvatarBlocked())
            {
                if (_homeArmed)
                {
                    _homeArmed = false;
                    WxSession.HideProfileButton();
                }
                return;
            }
            if (_homeArmed)
            {
                if (!WxSession.ProfileButtonVisible && Time.unscaledTime - _armAt > 2f) _homeArmed = false;
                else return;
            }
            ArmHome();
        }

        bool AvatarBlocked()
        {
            if (!_root) return false;
            for (int i = 0; i < _root.childCount; i++)
            {
                var child = _root.GetChild(i);
                if (!child.gameObject.activeInHierarchy) continue;
                switch (child.name)
                {
                    case "panel":
                    case "confirm":
                    case "shop":
                    case "daily":
                    case "armory":
                    case "forge":
                    case "board":
                    case "itemRead":
                        return true;
                }
            }
            return false;
        }

        static string ProfileLine() => $"{LiveConfig.MyName} · {Save.RankName} · 胜 {Save.D.wins} / 战 {Save.D.battles}";

        void ToggleSound()
        {
            Sfx.On = !Sfx.On;
            Save.D.sfx = Sfx.On;
            Save.Write();
            if (Sfx.On) Sfx.Play(Snd.Tap);
            Toast(Sfx.On ? "声音已打开" : "声音已关闭");
        }

        const string DefaultNotice =
            "各位好汉：\n\n" +
            "《营救宋江》已更新，公告如下。\n\n" +
            "【更新内容】\n" +
            "1. 更新首页与战场画面。\n" +
            "2. 优化官兵行进时的朝向。\n" +
            "3. 修复排行榜名次显示错误。\n\n" +
            "【排行榜】\n" +
            "按战绩由高到低排列。\n" +
            "战绩相同，先达成者居前。\n" +
            "名单上的名次，与底部「我」一致。\n\n" +
            "【温馨提示】\n" +
            "· 登录微信后，同步昵称、头像和进度。\n" +
            "· 体力与每日次数，北京时间零点刷新。\n" +
            "· 若遇异常，退出后重新进入。\n\n" +
            "感谢支持。\n\n" +
            "《营救宋江》运营组\n" +
            "2026年11月7日";

        void ShowNotice()
        {
            string remote = LiveConfig.Notice;
            bool live = !string.IsNullOrEmpty(remote);
            Panel(live ? LiveConfig.NoticeTitle : "公告", out var card, new Vector2(900, 1300));
            // 内衬底边在 -624，正文停在它上面，避免最后一行压到木框。
            var body = Ui.Label(card, live ? remote.Replace("\\n", "\n") : DefaultNotice,
                34, Pal.Ink, new Vector2(0, -36), new Vector2(740, 1040), TextAnchor.UpperLeft);
            body.lineSpacing = 1.02f;
            body.verticalOverflow = VerticalWrapMode.Truncate;
        }

        void Fight() => Fight(_selected);

        public void StartSlot(int slot)
        {
            _selected = Mathf.Clamp(slot, 0, ChapterDef.StoryCount);
            RefreshChapter();
            Fight(_selected);
        }

        void Fight(int slot)
        {
            bool freeDaily = slot == ChapterDef.DailySlot && Save.DailyFreeToday;
            if (!freeDaily && !Stamina.Spend())
            {
                string freeWay = Save.DailyFreeToday ? "\n（提示：左侧「每日一战」今日首局免体力！）"
                    : Save.LoreQuizPending > 0 ? $"\n（提示：左侧「回目讲堂」还有 {Save.LoreQuizPending} 卷免费考校赏钱可领）" : "";
                int left = AdStub.Left("补充体力");
                if (left == 0)
                {
                    int sec = Stamina.SecondsToNext;
                    Nope($"体力不够，{sec / 60}分{sec % 60:00}秒后回 1 点" + freeWay);
                    return;
                }
                string more = left > 0 ? $"\n今天还能补 {left} 次" : "";
                Confirm("体力不够了", "看段广告，补 5 点体力？" + more + freeWay, "看广告", () => AdStub.Show("补充体力", () => { Stamina.Add(5); Refresh(); }));
                return;
            }
            _onFight?.Invoke(slot);
        }

        float _refreshT;
        string _avatarSeen;

        void Update()
        {
            _refreshT -= Time.unscaledDeltaTime;
            if (_refreshT <= 0) { _refreshT = 1f; Refresh(); }
            SyncProfileButton();
            if (_clubHit) WxSession.SyncGameClub(_clubHit, !WxSession.PrivacyShowing && !AvatarBlocked());
        }

        void Refresh()
        {
            bool anyBioOpenNew = false;
            for (int i = 0; i < ChapterDef.Bios.Length && !anyBioOpenNew; i++)
                if (Save.BioOpen(i) && !Save.BioCleared(i)) anyBioOpenNew = true;
            bool anyCamOpenNew = false;
            for (int i = 0; i < ChapterDef.Campaigns.Length && !anyCamOpenNew; i++)
                if (Save.CampaignOpen(i) && !Save.CampaignCleared(i)) anyCamOpenNew = true;
            if (_codexDot) _codexDot.gameObject.SetActive(Save.CodexPending > 0);
            if (_chaptersDot) _chaptersDot.gameObject.SetActive(Save.LoreQuizPending > 0 || anyBioOpenNew || anyCamOpenNew);
            if (_dailyDot) _dailyDot.gameObject.SetActive(Save.DailyOpen && !Save.DailyWonToday);
            _coins.text = Save.D.coins.ToString();
            _stamina.text = StaminaText();
            if (_shopCoins != null) _shopCoins.text = _coins.text;
            if (_shopStamina != null) _shopStamina.text = _stamina.text;
            var next = Save.D.PeekLoadout();
            string carried = BattleScreen.ItemSummary(next);
            string worn = BattleScreen.GearSummary(next);
            if (worn.Length > 0) worn = "兵器架（" + worn + "）";
            string line = carried;
            if (worn.Length > 0) line = carried.Length > 0 ? worn + "　" + carried : worn;
            _loadout.text = line.Length > 0 ? "下一战随身：" + line : "";
            WxSession.EnsureAvatar();
            string avatar = WxSession.Avatar;
            if (_hudFace) Portraits.Set(_hudFace, avatar, true);
            if (_avatarSeen != null && avatar != _avatarSeen && avatar.Length > 0)
                _reloadBoard?.Invoke();
            _avatarSeen = avatar;
            bool showName = LiveConfig.HasNick || (WxSession.CanAskProfile && !LiveConfig.HasNick);
            if (_profileHint)
            {
                _profileHint.gameObject.SetActive(showName);
                if (LiveConfig.HasNick)
                {
                    _profileHint.text = WxSession.Nick;
                    WxSession.ApplyNickFont(_profileHint);
                }
                else _profileHint.text = "点我授权";
            }
            if (_profileBar) _profileBar.gameObject.SetActive(showName);
            _rankName.text = Save.RankName;
            _stars.text = StarRich(Save.D.stars);
            if (_badgeTier != Save.D.tier)
            {
                _badgeTier = Save.D.tier;
                if (_badge) Destroy(_badge.gameObject);
                _badge = Ui.RankBadge(_badgeHolder, Save.D.tier, Vector2.zero, 0.82f);
            }
            RefreshChapter();
        }

        static string StaminaText()
        {
            int s = Stamina.Current;
            if (s >= Stamina.Max) return $"{s} <size=30>/{Stamina.Max}</size>";
            int sec = Stamina.SecondsToNext;
            return $"{s} <size=30>/{Stamina.Max}  {sec / 60}:{sec % 60:00}</size>";
        }

        static string StarRich(int n) =>
            Ui.RankStars(Save.D.tier, n, ColorUtility.ToHtmlStringRGB(StarOn), ColorUtility.ToHtmlStringRGB(StarOff));

        // ─────────────────────────── 浮层 ───────────────────────────

        RectTransform Panel(string title, out RectTransform card, Vector2 size, Action onClose = null)
        {
            var layer = Ui.Fill(_root, new Color(0, 0, 0, 0.72f), true, "panel");
            card = Ui.WoodPanel(Ui.Fit(layer, size.y + 160), size, new Vector2(0, -20));
            Ui.Title(card, title, new Vector2(0, size.y / 2 - 10));
            var l = layer;
            Ui.CloseX(card, new Vector2(-size.x / 2 + 6, size.y / 2 - 4), () => { Destroy(l.gameObject); onClose?.Invoke(); });
            Tween.Pop(card, 0.25f);
            return layer;
        }

        static void Toast(string s) => Ui.Toast(s);
        static void Nope(string s) => Ui.Nope(s);

        void Confirm(string title, string body, string ok, Action onOk)
        {
            var layer = Ui.Fill(_root, new Color(0, 0, 0, 0.72f), true, "confirm");
            var card = Ui.WoodPanel(layer, new Vector2(840, 600), Vector2.zero);
            Ui.Title(card, title, new Vector2(0, 290), 80);
            Ui.Label(card, body, 42, Pal.Ink, new Vector2(0, 50), new Vector2(720, 200));
            Ui.CartoonBtn(card, ok, new Vector2(300, 110), new Vector2(-170, -170), Ui.BtnRed, () => { Destroy(layer.gameObject); onOk(); }, 46,
                ok.Contains("广告") ? "ui_video" : null);
            Ui.CartoonBtn(card, "算了", new Vector2(300, 110), new Vector2(170, -170), Ui.BtnDark, () => Destroy(layer.gameObject), 46);
            Tween.Pop(card, 0.25f);
        }
    }
}
