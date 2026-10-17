using System;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;

namespace Rescue
{
    /// <summary>战场的世界空间表现与输入。只读 Match 状态渲染，玩家操作走 Side 的公开接口。</summary>
    public sealed partial class BattleView : MonoBehaviour
    {
        public const float Left = -4f;
        public const float MyBottom = -4.25f;
        public const float OppBottom = 1.13f;
        public const float CampY = MyBottom - 0.47f;
        public const float ButtonY = CampY - 1.22f;
        /// <summary>主动道具一行，压在技能和聚义下面。</summary>
        public const float ItemY = ButtonY - 1.06f;
        public const float TopY = 6.39f;
        const float WallY = (MyBottom + Board.Rows + OppBottom) * 0.5f;
        const float HudHalf = 0.40f;
        const float SkillX = -2.75f;
        const float SkillD = 1.00f;
        const float ItemGap = 1.0f;
        const float ItemScale = 0.72f;
        /// <summary>可见宽度正好是 8 格。再为了装高度而缩小，两侧就会露出地面。</summary>
        const float WidthNeed = Board.Cols;
        public static Vector3 CamBase = new Vector3(0f, -0.1f, -10f);
        const int MaxShovelAds = 1;
        const float RecruitW = 2.7f;
        const float RecruitH = 1.0f;

        public Match M;
        public bool Paused;
        public float Speed = 1f;
        public bool InputEnabled = true;

        public event Action<SimEvent> OnEvent;
        public event Action OnPauseClicked;
        public event Action<Piece> OnDragStart;
        public event Action OnPlaced;

        Camera _cam;
        Transform _root, _fxRoot;
        float _acc;
        float _shake;
        int _frame;

        readonly Transform[][,] _cellVis = { new Transform[Board.Cols, Board.Rows], new Transform[Board.Cols, Board.Rows] };
        readonly Cell[][,] _cellKind = { new Cell[Board.Cols, Board.Rows], new Cell[Board.Cols, Board.Rows] };
        readonly SpriteRenderer[,] _hl = new SpriteRenderer[Board.Cols, Board.Rows];
        readonly TextMesh[,] _hlMark = new TextMesh[Board.Cols, Board.Rows];
        readonly Dictionary<int, PieceView> _pieces = new Dictionary<int, PieceView>();
        readonly Dictionary<int, EnemyView> _enemies = new Dictionary<int, EnemyView>();
        readonly Dictionary<int, FrameView> _frames = new Dictionary<int, FrameView>();
        readonly GoalView[] _goals = new GoalView[2];
        readonly List<Fx> _fx = new List<Fx>();
        readonly SpriteRenderer[] _campSlots = new SpriteRenderer[Tuning.CampSlots];

        SpriteRenderer _recruitBtn, _recruitFill, _pauseBtn, _mengBtn, _shuBtn, _baoziBtn, _fuBtn, _trapBtn, _rescueBtn;
        TextMesh _recruitCost, _recruitLabel, _bunsText, _waveText, _midText, _mengText, _shuText, _baoziText, _fuText, _trapText, _titleText;
        Transform _shovelRoot;
        float _shovelSlide;
        bool _shovelForce;
        const float ShovelOfferX = 3.47f;
        const int ShovelOfferEmpty = 2;
        /// <summary>白地比格子小一圈，圆角，格与格之间留出草地。边上和中间同一块。</summary>
        const float TilePad = 0.88f;
        Transform _bunIcon, _rescueRoot;
        static readonly Vector3 _bunIconScale = new Vector3(0.5f, 0.5f, 1f);
        int _shovelAds;
        TextMesh _tipText;
        SpriteRenderer _tipBg;
        float _tipT;
        float _recruitShown;
        float _recruitFlash;
        bool _recruitWasFull;

        // 点选属性卡 + 攻击范围。卡贴在山寨下面，不压棋盘，射程圈才能看全。
        Transform _card;
        SpriteRenderer _cardBg, _cardShadow, _cardPaper, _cardSeal, _cardRule, _cardIcon;
        TextMesh _cardTitle, _cardLevel, _cardBody, _cardGlyph;
        Vector2 _cardSize;
        float _cardT;
        PieceView _picked;

        // 闲置提示
        readonly List<SpriteRenderer> _hintDots = new List<SpriteRenderer>();
        SpriteRenderer _hintBox;
        float _idleT, _hintRecalc;
        bool _hintOn;
        Vector2 _hintFrom, _hintTo;

        // 拖拽状态
        PieceView _drag;
        int _dragCamp = -1;
        CellPos _dragCell;
        Vector2 _downPos;
        bool _dragMoved;
        readonly TextMesh[] _campPlus = new TextMesh[Tuning.CampSlots];
        readonly List<SpriteRenderer> _dragDots = new List<SpriteRenderer>();
        bool _rowRefresh;

        // ─────────────────────────── 布局 ───────────────────────────

        /// <summary>
        /// 宽度永远刚好是 8 格，左右贴边。不够高就收紧界面，不再把整张棋盘缩小。
        /// 真机上标题让开微信胶囊，多出来的高度留在底下。
        /// </summary>
        public static void FitCamera(Camera cam)
        {
            var rect = cam.rect;
            float vw = Mathf.Max(1f, rect.width * Screen.width), vh = Mathf.Max(1f, rect.height * Screen.height);
            float bar = 0f, menuTop = 0f, menuBottom = 0f;
            if (rect.width >= 0.999f && rect.height >= 0.999f)
                Ui.ScreenInsets(out bar, out menuTop, out menuBottom, out _, out _);
            float upp = WidthNeed / vw;
            float topWorld = menuBottom > 0f
                ? TopY + (menuTop + menuBottom) * 0.5f * upp
                : TopY + HudHalf + 0.02f + bar * upp;
            cam.orthographicSize = vh * upp * 0.5f;
            CamBase = new Vector3(0f, topWorld - cam.orthographicSize, -10f);
            cam.transform.position = CamBase;
        }

        public static Vector2 World(int side, V2 p) =>
            side == 0 ? new Vector2(Left + p.x, MyBottom + p.y) : new Vector2(Left + (Board.Cols - p.x), OppBottom + (Board.Rows - p.y));

        public static Vector2 CellWorld(int side, int c, int r) => World(side, new CellPos(c, r).Center);
        public static Vector2 CampWorld(int i) => new Vector2(-2.3f + i * 1.12f, CampY);
        public Vector2 BunsWorld => _bunIcon.position;

        public void Build(Match m, Camera cam)
        {
            M = m;
            _cam = cam;
            _root = new GameObject("World").transform;
            _root.SetParent(transform, false);
            _fxRoot = new GameObject("Fx").transform;
            _fxRoot.SetParent(transform, false);

            var paper = Gfx.Sprite(_root, Art.Paper, Pal.Paper, new Vector2(0, 0), new Vector2(30, 30), -10, "paper");
            paper.drawMode = SpriteDrawMode.Tiled;
            paper.size = new Vector2(30, 30);
            paper.transform.localScale = Vector3.one;

            BuildFrame();
            for (int s = 0; s < 2; s++) BuildBoard(s);
            BuildScouts();
            BuildCamp();
            BuildHud();
            BuildTip();
            BuildCard();
            for (int s = 0; s < 2; s++) _goals[s] = new GoalView(this, s);
            BuildAmbient();
            SyncAll(true);
        }

        /// <summary>原作是一整张棋盘：上半对手、下半自己，中间一道城墙。</summary>
        void BuildFrame()
        {
            float top = OppBottom + Board.Rows, bottom = MyBottom;
            var center = new Vector2(0, (top + bottom) * 0.5f);
            var size = new Vector2(Board.Cols, top - bottom);
            // 棋盘贴着屏幕左右边缘，深色边框会在两侧露出一条黑边，所以只在底下留一道阴影。
            Gfx.Sprite(_root, Art.White, Pal.Shadow, center + new Vector2(0f, -0.1f), new Vector2(size.x, size.y + 0.16f), 0, "board_shadow");

            float wallH = OppBottom - (MyBottom + Board.Rows);
            var wall = Gfx.Sprite(_root, Art.Img("tex_stone"), Color.white, new Vector2(0, WallY), Vector2.one, 9, "wall");
            wall.drawMode = SpriteDrawMode.Tiled;
            wall.transform.localScale = Vector3.one;
            wall.size = new Vector2(Board.Cols, wallH);
            Gfx.Sprite(_root, Art.White, Pal.Ink.A(0.75f), new Vector2(0, WallY + wallH * 0.5f), new Vector2(Board.Cols, 0.05f), 10);
            Gfx.Sprite(_root, Art.White, Pal.Ink.A(0.75f), new Vector2(0, WallY - wallH * 0.5f), new Vector2(Board.Cols, 0.05f), 10);
            for (float x = Left + 0.25f; x < -Left; x += 0.5f)
            {
                Gfx.Sprite(_root, Art.White, Pal.InkSoft.A(0.85f), new Vector2(x, WallY + wallH * 0.5f + 0.06f), new Vector2(0.26f, 0.1f), 10);
                Gfx.Sprite(_root, Art.White, Pal.InkSoft.A(0.85f), new Vector2(x, WallY - wallH * 0.5f - 0.06f), new Vector2(0.26f, 0.1f), 10);
            }
            DecorateWall(WallY, wallH);
        }

        void BuildBoard(int side)
        {
            var b = M.Sides[side].Board;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    BuildCell(side, c, r);
                    if (side == 0)
                    {
                        var pos = CellWorld(side, c, r);
                        _hl[c, r] = Gfx.Sprite(_root, Art.Rounded, Color.clear, pos, new Vector2(0.95f, 0.95f), 14, "hl");
                        _hlMark[c, r] = Gfx.Text(_root, "", Fonts.Kai, 0.26f, Pal.RedDeep, pos + new Vector2(0, -0.33f), 15);
                    }
                }

            BuildSpawn(CellWorld(side, b.Spawn.c, b.Spawn.r));
        }

        void BuildCell(int side, int c, int r)
        {
            var b = M.Sides[side].Board;
            var kind = b.Cells[c, r];
            _cellKind[side][c, r] = kind;
            if (_cellVis[side][c, r] != null) Destroy(_cellVis[side][c, r].gameObject);
            var t = new GameObject("cell").transform;
            t.SetParent(_root, false);
            t.localPosition = CellWorld(side, c, r);
            _cellVis[side][c, r] = t;
            var full = new Vector2(1.01f, 1.01f);
            switch (kind)
            {
                case Cell.Path:
                    Gfx.Sprite(t, Art.Ground("tex_earth", c, r), Color.white, Vector2.zero, full, 8);
                    PathEdges(t, side, b, c, r);
                    break;
                case Cell.Grass:
                case Cell.Marsh:
                    Gfx.Sprite(t, Art.Ground("tex_grass", c, r), Color.white, Vector2.zero, full, 8);
                    break;
                case Cell.Tile:
                    Gfx.Sprite(t, Art.Ground("tex_grass", c, r), Color.white, Vector2.zero, full, 8);
                    Gfx.Sprite(t, Art.Rounded, Pal.Shadow, new Vector2(0.02f, -0.03f), new Vector2(TilePad, TilePad), 9);
                    Gfx.Sprite(t, Art.Rounded, Pal.Tile, Vector2.zero, new Vector2(TilePad, TilePad), 10);
                    break;
                case Cell.Water:
                    Gfx.Sprite(t, Art.White, Pal.Water, Vector2.zero, full, 8);
                    for (int i = 0; i < 3; i++)
                        Gfx.Sprite(t, Art.Stroke, Color.white.A(0.5f), new Vector2(0, 0.22f - i * 0.22f), new Vector2(0.72f, 0.18f), 9);
                    break;
                case Cell.Rock:
                    Gfx.Sprite(t, Art.Ground("tex_grass", c, r), Color.white, Vector2.zero, full, 8);
                    break;
            }
            Decorate(t, side, kind, c, r);
            BleedEdge(t, side, c, r, kind);
        }

        /// <summary>最外侧一列的地面向外铺出去，万一画面略宽，两侧仍是地面而不是露底。</summary>
        void BleedEdge(Transform t, int side, int c, int r, Cell kind)
        {
            if (c != 0 && c != Board.Cols - 1) return;
            int outward = (side == 0 ? 1 : -1) * (c == 0 ? -1 : 1);
            var sprite = kind == Cell.Water ? Art.White : Art.Ground(kind == Cell.Path ? "tex_earth" : "tex_grass", c, r);
            var color = kind == Cell.Water ? Pal.Water : Color.white;
            for (int i = 1; i <= 3; i++)
                Gfx.Sprite(t, sprite, color, new Vector2(outward * i, 0f), new Vector2(1.01f, 1.01f), 8);
        }

        void PathEdges(Transform t, int side, Board b, int c, int r)
        {
            var self = CellWorld(side, c, r);
            int salt = 0;
            foreach (var d in new[] { new CellPos(1, 0), new CellPos(-1, 0), new CellPos(0, 1), new CellPos(0, -1) })
            {
                salt++;
                int nc = c + d.c, nr = r + d.r;
                if (Board.InBounds(nc, nr) && b.Cells[nc, nr] == Cell.Path) continue;
                var delta = (CellWorld(side, nc, nr) - self) * 0.5f;
                var edge = Gfx.Sprite(t, Art.Stroke, Pal.PathEdge.A(0.8f), delta * 0.94f, new Vector2(1.0f, 0.07f), 9);
                edge.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Abs(delta.x) > 0.01f ? 90 : 0);
                PathPebbles(t, delta, side, c, r, salt);
            }
        }

        void BuildCamp()
        {
            Gfx.Sprite(_root, Art.Rounded, Pal.Ink.A(0.85f), new Vector2(-0.52f, CampY), new Vector2(6.88f, 1.22f), 1);
            Gfx.Sprite(_root, Art.Rounded, Pal.PaperDark, new Vector2(-0.52f, CampY), new Vector2(6.76f, 1.1f), 2);
            Gfx.Text(_root, "山寨", Fonts.Brush, 0.4f, Pal.RedDeep, new Vector2(-3.43f, CampY), 5);
            for (int i = 0; i < _campSlots.Length; i++)
            {
                _campSlots[i] = Gfx.Sprite(_root, Art.Rounded, Pal.Paper.A(0.9f), CampWorld(i), new Vector2(0.98f, 0.98f), 4, "slot");
                _campPlus[i] = Gfx.Text(_root, "", Fonts.Kai, 0.48f, Pal.InkSoft, CampWorld(i), 6);
            }

            _rescueRoot = new GameObject("rescue").transform;
            _rescueRoot.SetParent(_root, false);
            _rescueRoot.localPosition = new Vector2(ShovelOfferX, CampY);
            Gfx.Sprite(_rescueRoot, Art.Rounded, Pal.Shadow, new Vector2(0.03f, -0.07f), new Vector2(0.84f, 1.08f), 49);
            _rescueBtn = Gfx.Sprite(_rescueRoot, Art.Rounded, Pal.GoldBright, Vector2.zero, new Vector2(0.84f, 1.08f), 50);
            Gfx.Sprite(_rescueRoot, Art.Frame, Pal.Gold, Vector2.zero, new Vector2(0.84f, 1.08f), 51);
            Gfx.Text(_rescueRoot, "救", Fonts.Brush, 0.5f, Pal.RedDeep, new Vector2(0, 0.12f), 52);
            Gfx.Sprite(_rescueRoot, Art.Rounded, Pal.Red, new Vector2(0, -0.34f), new Vector2(0.78f, 0.28f), 52);
            Gfx.Sprite(_rescueRoot, Art.Img("ui_video"), Color.white, new Vector2(-0.2f, -0.34f), Vector2.one * 0.26f, 53);
            Gfx.Text(_rescueRoot, "广告", Fonts.Kai, 0.17f, Pal.Paper, new Vector2(0.12f, -0.34f), 53);
            _rescueRoot.gameObject.SetActive(false);

            _shovelRoot = new GameObject("shovelOffer").transform;
            _shovelRoot.SetParent(_root, false);
            _shovelRoot.localPosition = new Vector2(ShovelOfferX, CampY);
            Gfx.Sprite(_shovelRoot, Art.Rounded, Pal.Shadow, new Vector2(0.03f, -0.07f), new Vector2(0.84f, 0.92f), 49);
            Gfx.Sprite(_shovelRoot, Art.Rounded, Pal.Hex("E89A2C"), Vector2.zero, new Vector2(0.84f, 0.92f), 50, "shovelAd");
            Gfx.Sprite(_shovelRoot, Art.Rounded, Pal.Hex("FFC94A"), new Vector2(0, 0.03f), new Vector2(0.74f, 0.8f), 51);
            Gfx.Sprite(_shovelRoot, Art.Img("shovel"), Color.white, new Vector2(0.04f, -0.02f), Vector2.one * 0.62f, 52);
            Gfx.Sprite(_shovelRoot, Art.Img("ui_video"), Color.white, new Vector2(-0.24f, 0.27f), Vector2.one * 0.3f, 53);
            Gfx.Text(_shovelRoot, "x" + Tuning.AdShovels, Fonts.Kai, 0.26f, Pal.Ink, new Vector2(0.23f, -0.27f), 53);
            Gfx.Text(_shovelRoot, "x" + Tuning.AdShovels, Fonts.Kai, 0.26f, Color.white, new Vector2(0.21f, -0.25f), 54);
            _shovelRoot.gameObject.SetActive(false);
        }

        /// <summary>白地快用完时挂在山寨右边，一直等到玩家看掉；手里有铲子或要救命时先收起来。</summary>
        void TickShovelOffer(float dt, bool rescue)
        {
            var p = M.Player;
            bool want = _shovelAds < MaxShovelAds && !M.Over && !rescue && (_shovelForce || ShovelMoment(p));
            bool shown = _shovelRoot.gameObject.activeSelf;
            if (!want)
            {
                if (shown) _shovelRoot.gameObject.SetActive(false);
                return;
            }
            if (!shown) SlideInShovel();
            _shovelSlide = Mathf.Min(1f, _shovelSlide + dt / 0.35f);
            float x = Mathf.Lerp(ShovelOfferX + 1.4f, ShovelOfferX, Tween.OutBack(_shovelSlide));
            float wiggle = Mathf.Sin(Time.time * 9f) * 7f * (Mathf.Sin(Time.time * 1.6f) > 0.4f ? 1f : 0f);
            _shovelRoot.localPosition = new Vector2(x, CampY + Mathf.Sin(Time.time * 3f) * 0.03f);
            _shovelRoot.localRotation = Quaternion.Euler(0, 0, wiggle);
        }

        static bool HasShovel(Side p)
        {
            if (p.ShovelStock > 0) return true;
            foreach (var q in p.Camp) if (q != null && q.Type == PieceType.Shovel) return true;
            return false;
        }

        /// <summary>白地最多还剩两格、还有草地能开、手里没铲子。</summary>
        static bool ShovelMoment(Side p)
        {
            if (!p.Board.AnyDiggable() || HasShovel(p)) return false;
            return p.EmptyCells() <= ShovelOfferEmpty;
        }

        /// <summary>截图流程用：不等时机直接挂出来。</summary>
        public void ShowShovelOffer()
        {
            _shovelForce = true;
            SlideInShovel();
        }

        void SlideInShovel()
        {
            _shovelSlide = 0f;
            _shovelRoot.gameObject.SetActive(true);
            _shovelRoot.localPosition = new Vector2(ShovelOfferX + 1.4f, CampY);
        }

        void BuildHud()
        {
            Gfx.Sprite(_root, Art.Rounded, Pal.Shadow, new Vector2(0.2f, ButtonY - 0.09f), new Vector2(RecruitW, RecruitH), 48);
            Gfx.Sprite(_root, Art.Rounded, Pal.Ink, new Vector2(0.2f, ButtonY), new Vector2(RecruitW + 0.16f, RecruitH + 0.16f), 49, "recruitRim");
            _recruitBtn = Gfx.Sprite(_root, Art.Rounded, Pal.Hex("3A342C"), new Vector2(0.2f, ButtonY), new Vector2(RecruitW, RecruitH), 50, "recruit");
            _recruitFill = Gfx.Sprite(_root, Art.Progress, Pal.Hex("C4894A"), new Vector2(0.2f, ButtonY), new Vector2(RecruitW, RecruitH), 51, "recruitFill");
            _recruitFill.enabled = false;
            _recruitLabel = Gfx.Text(_root, "聚  义", Fonts.Brush, 0.5f, Pal.Paper, new Vector2(0.2f, ButtonY + 0.14f), 52);
            _recruitCost = Gfx.Text(_root, "", Fonts.Kai, 0.26f, Pal.Paper, new Vector2(0.32f, ButtonY - 0.27f), 52);
            Gfx.Sprite(_root, Art.Img("bun"), Color.white, new Vector2(-0.06f, ButtonY - 0.27f), Vector2.one * 0.26f, 52);

            BuildSkill();

            _mengBtn = ItemButton(0, ItemSlot(0), "it_menghan", "蒙汗药", Pal.Hex("C9A6EE"), out _mengText);
            _baoziBtn = ItemButton(1, ItemSlot(1), "bun", "包子", Pal.Hex("A4D8F2"), out _baoziText);
            _shuBtn = ItemButton(2, ItemSlot(2), "it_tianshu", "天书", Pal.Hex("F6C46B"), out _shuText);
            _fuBtn = ItemButton(3, ItemSlot(3), "it_fu", "攻速符", Pal.Hex("C9A6EE"), out _fuText);
            _trapBtn = ItemButton(4, ItemSlot(3), "it_trap", "陷阱", Pal.Hex("C5E08A"), out _trapText);

            _pauseBtn = Gfx.Sprite(_root, Art.Img("ui_pause"), Color.white, new Vector2(-3.5f, TopY), new Vector2(0.8f, 0.8f), 50, "pause");

            Gfx.Sprite(_root, Art.Rounded, Pal.Ink.A(0.85f), new Vector2(-2.1f, TopY), new Vector2(1.7f, 0.64f), 49);
            _bunIcon = Gfx.Sprite(_root, Art.Img("bun"), Color.white, new Vector2(-2.75f, TopY), Vector2.one * 0.54f, 51, "bun").transform;
            _bunsText = Gfx.Text(_root, "0", Fonts.Kai, 0.4f, Pal.Paper, new Vector2(-1.9f, TopY), 52);

            // 右上角留给微信胶囊，对手名并进波次那一行。
            _titleText = Gfx.Text(_root, M.Chapter.Name, Fonts.Brush, 0.48f, Pal.Ink, new Vector2(0.15f, TopY + 0.10f), 52);
            _waveText = Gfx.Text(_root, "", Fonts.Kai, 0.27f, Pal.InkSoft, new Vector2(0.15f, TopY - 0.18f), 52);

            Gfx.Sprite(_root, Art.Rounded, Pal.Ink.A(0.85f), new Vector2(0, WallY), new Vector2(3.3f, 0.34f), 11);
            Gfx.Sprite(_root, Art.Rounded, Pal.Paper, new Vector2(0, WallY), new Vector2(3.2f, 0.28f), 12);
            _midText = Gfx.Text(_root, "", Fonts.Kai, 0.26f, Pal.Ink, new Vector2(0, WallY), 13);
        }

        /// <summary>圆形道具钮：稀有度色圆底 + 图标 + 右上角红色数量角标 + 底部名字条，外圈金环。</summary>
        SpriteRenderer ItemButton(int index, Vector2 pos, string icon, string name, Color disc, out TextMesh text)
        {
            var btn = Gfx.Sprite(_root, Art.Circle, disc, pos, Vector2.one, 50, "item");
            var t = btn.transform;
            Gfx.Sprite(t, Art.Circle, Pal.Shadow, new Vector2(0.03f, -0.07f), Vector2.one * 1.04f, 47);
            Gfx.Sprite(t, Art.Circle, Pal.Ink, Vector2.zero, Vector2.one * 1.04f, 48);
            Gfx.Sprite(t, Art.Ring, Pal.GoldBright, Vector2.zero, Vector2.one * 1.12f, 49, "ring");
            Gfx.Sprite(t, Art.Img(icon), Color.white, new Vector2(0, 0.06f), Vector2.one * 0.72f, 54);
            _itemCd[index] = new CooldownRing(t, 0.96f, 52);
            Gfx.Sprite(t, Art.Rounded, Pal.Ink.A(0.82f), new Vector2(0, -0.38f), new Vector2(0.9f, 0.26f), 56);
            Gfx.Text(t, name, Fonts.Kai, 0.17f, Pal.Paper, new Vector2(0, -0.38f), 57);
            Gfx.Sprite(t, Art.Circle, Pal.Paper, new Vector2(0.38f, 0.36f), Vector2.one * 0.36f, 58);
            Gfx.Sprite(t, Art.Circle, Pal.Hex("D7362B"), new Vector2(0.38f, 0.36f), Vector2.one * 0.31f, 59);
            text = Gfx.Text(t, "", Fonts.Kai, 0.22f, Pal.Paper, new Vector2(0.38f, 0.35f), 60);
            _itemCdText[index] = Gfx.Text(t, "", Fonts.Kai, 0.30f, Pal.GoldBright, Vector2.zero, 61, name: "cd");
            _itemCdText[index].gameObject.SetActive(false);
            return btn;
        }

        void BuildTip()
        {
            _tipBg = Gfx.Sprite(_root, Art.Rounded, Pal.Ink.A(0.9f), Vector2.zero, new Vector2(3, 1), 80, "tipbg");
            _tipText = Gfx.Text(_root, "", Fonts.Kai, 0.27f, Pal.Paper, Vector2.zero, 81);
            _tipBg.enabled = false;
            _tipText.text = "";
        }

        void BuildCard()
        {
            BuildRange();
            _card = new GameObject("card").transform;
            _card.SetParent(_root, false);
            _cardShadow = Gfx.Sprite(_card, Art.Rounded, Pal.Shadow, Vector2.zero, new Vector2(3.6f, 2f), 80, "cardShadow");
            _cardBg = Gfx.Sprite(_card, Art.Rounded, Pal.Ink, Vector2.zero, new Vector2(3.6f, 2f), 81, "cardRim");
            _cardPaper = Gfx.Sprite(_card, Art.Rounded, Pal.Paper, Vector2.zero, new Vector2(3.4f, 1.8f), 82, "cardPaper");
            _cardSeal = Gfx.Sprite(_card, Art.Rounded, Pal.Tile, Vector2.zero, new Vector2(0.68f, 0.68f), 83, "cardSeal");
            _cardGlyph = Gfx.Text(_card, "", Fonts.Brush, 0.40f, Pal.RedDeep, Vector2.zero, 84);
            _cardIcon = Gfx.Sprite(_card, Art.White, Color.white, Vector2.zero, Vector2.one * 0.56f, 85, "cardIcon");
            _cardIcon.enabled = false;
            _cardTitle = Gfx.Text(_card, "", Fonts.Brush, 0.32f, Pal.RedDeep, Vector2.zero, 84, TextAnchor.MiddleLeft);
            _cardLevel = Gfx.Text(_card, "", Fonts.Kai, 0.22f, Pal.InkSoft, Vector2.zero, 84, TextAnchor.MiddleRight);
            _cardRule = Gfx.Sprite(_card, Art.White, Pal.Gold.A(0.55f), Vector2.zero, new Vector2(4f, 0.018f), 84, "cardRule");
            _cardBody = Gfx.Text(_card, "", Fonts.Kai, 0.22f, Pal.Ink, Vector2.zero, 84, TextAnchor.UpperLeft);
            _cardBody.lineSpacing = 1f;
            HideCard();
        }

        // ─────────────────────────── 主循环 ───────────────────────────

        void Update()
        {
            if (M == null) return;
            _frame++;
            if (_hitStop > 0f && !Paused)
            {
                _hitStop -= Time.deltaTime;
                FlushEvents();
            }
            else if (!Paused && !M.Over)
            {
                _acc += Mathf.Min(Time.deltaTime, 0.1f) * Speed;
                const float step = 1f / 60f;
                while (_acc >= step)
                {
                    _acc -= step;
                    M.Step(step);
                    FlushEvents();
                    if (M.Over) break;
                }
            }
            else FlushEvents();

            if (InputEnabled && !Paused) HandleInput();
            SyncAll(false);
            if (!Paused) TickLater(Time.deltaTime);
            UpdateFx(Time.deltaTime);
            TickMap(Time.deltaTime);
            TickScouts(Paused ? 0f : Time.deltaTime);
            UpdateTip(Time.deltaTime);
            UpdateCard(Time.deltaTime);
            TickRange(Time.deltaTime);
            UpdateIdleHint(Time.deltaTime);

            _shake = Mathf.Max(0, _shake - Time.deltaTime * 2.5f);
            _cam.transform.position = CamBase + (Vector3)(UnityEngine.Random.insideUnitCircle * _shake * 0.18f);
        }

        public void ExternalTick() => FlushEvents();

        void FlushEvents()
        {
            if (M.Events.Count == 0) return;
            var list = new List<SimEvent>(M.Events);
            M.Events.Clear();
            foreach (var ev in list)
            {
                Handle(ev);
                OnEvent?.Invoke(ev);
            }
        }

        // ─────────────────────────── 同步 ───────────────────────────

        void SyncAll(bool snap)
        {
            var p = M.Player;
            _bunsText.text = p.Buns.ToString();
            bool dismiss = _drag != null && _dragMoved;
            bool over = dismiss && OverRecruit(PointerWorld());
            bool can = p.CanRecruit && !dismiss;
            float ratio = p.RecruitCost <= 0 ? 0f : Mathf.Clamp01(p.Buns / (float)p.RecruitCost);
            if (ratio < _recruitShown - 0.02f) _recruitShown = ratio;
            else _recruitShown = Mathf.MoveTowards(_recruitShown, ratio, Time.deltaTime * 2.4f);
            bool full = _recruitShown >= 0.999f && ratio >= 0.999f;
            if (full && !_recruitWasFull) _recruitFlash = 1f;
            _recruitWasFull = full;
            _recruitFlash = Mathf.Max(0f, _recruitFlash - Time.deltaTime * 2.2f);
            float pulse = can ? 1f + Mathf.Sin(Time.time * 6f) * 0.025f : over ? 1.08f : 1f;
            _recruitBtn.size = new Vector2(RecruitW, RecruitH) * pulse;
            _recruitBtn.color = dismiss ? (over ? Pal.Ink : Pal.InkSoft) : Pal.Hex("3A342C");
            _recruitFill.enabled = !dismiss && _recruitShown > 0.02f;
            if (_recruitFill.enabled)
            {
                var sprite = full ? Art.Rounded : Art.Progress;
                if (_recruitFill.sprite != sprite) _recruitFill.sprite = sprite;
                float w = (full ? RecruitW : Mathf.Max(0.36f, RecruitW * _recruitShown)) * pulse;
                float h = RecruitH * pulse;
                float left = 0.2f - RecruitW * pulse * 0.5f;
                _recruitFill.size = new Vector2(w, h);
                _recruitFill.transform.localPosition = new Vector2(left + w * 0.5f, ButtonY);
                var body = Pal.Hex("8A5649");
                var ready = Pal.Hex("BD6247");
                var hot = Pal.Hex("E88862");
                float breathe = can ? 0.5f + 0.5f * Mathf.Sin(Time.time * 4.5f) : 0f;
                _recruitFill.color = full ? Color.Lerp(ready, hot, Mathf.Max(_recruitFlash, breathe)) : body;
            }
            _recruitLabel.text = dismiss ? "遣散" : p.EmptyCampSlots == 0 ? "寨满" : "聚  义";
            _recruitCost.text = dismiss ? "+" + Side.DismissRefund(_drag.Piece) : p.RecruitCost.ToString();

            SyncItem(0, _mengBtn, _mengText, p.Items.MengHan, p.MengCd, Tuning.CdMeng);
            SyncItem(1, _baoziBtn, _baoziText, p.Items.Baozi, p.BaoziCd, Tuning.CdBaozi);
            SyncItem(2, _shuBtn, _shuText, p.Items.TianShu, p.ShuCd, Tuning.CdShu);
            SyncItem(3, _fuBtn, _fuText, p.Items.Fu, p.FuCd, Tuning.CdFu);
            SyncItem(4, _trapBtn, _trapText, p.Items.Trap, p.TrapCd, Tuning.CdTrap);
            PlaceItems();
            SyncSkill();

            bool rescue = p.AtTheGate && !p.RescueUsed;
            if (!Paused) TickShovelOffer(Time.deltaTime, rescue);
            if (rescue != _rescueRoot.gameObject.activeSelf)
            {
                _rescueRoot.gameObject.SetActive(rescue);
                if (rescue) Tween.Pop(_rescueRoot, 0.3f);
            }
            if (rescue) _rescueRoot.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 8f) * 0.06f);

            _waveText.text = (M.Wave > 0 ? $"第 {M.Wave} 波" : "备战") + " · 对手" + M.Chapter.RivalName;
            float next = M.NextWaveIn;
            _midText.text = p.ShieldT > 0 ? $"宋江护体 {p.ShieldT:0} 秒"
                : M.Wave == 0 ? $"官兵 {next:0} 秒后到" : next > 0.1f ? $"下一波 {next:0} 秒" : $"第 {M.Wave} 波来袭";

            for (int s = 0; s < 2; s++) SyncSide(s, snap);
            SyncCamp(snap);
            SyncEnemies();
            for (int s = 0; s < 2; s++) _goals[s].Sync();
            CleanupStale();
        }

        void SyncItem(int i, SpriteRenderer btn, TextMesh text, int n, float cd, float max)
        {
            if (btn.gameObject.activeSelf != n > 0) btn.gameObject.SetActive(n > 0);
            text.text = n.ToString();
            bool cooling = n > 0 && cd > 0.05f;
            if (_itemCooling[i] && !cooling && n > 0)
            {
                _itemFlash[i] = 1f;
                Sfx.Play(Snd.Tap, 0.4f, 1.45f);
            }
            _itemCooling[i] = cooling;
            _itemFlash[i] = Mathf.Max(0f, _itemFlash[i] - Time.deltaTime * 2.6f);
            if (n > 0)
            {
                float breathe = cooling ? 1f : 1f + Mathf.Sin(Time.time * 4f) * 0.025f;
                float pop = 1f + 0.12f * _itemFlash[i];
                btn.transform.localScale = Vector3.one * ItemScale * breathe * pop;
            }
            float remain = max <= 0f ? 0f : Mathf.Clamp01(cd / max);
            if (cooling) _itemCd[i].ShowVeil(remain);
            else _itemCd[i].Hide();
            var label = _itemCdText[i];
            if (label.gameObject.activeSelf != cooling) label.gameObject.SetActive(cooling);
            if (cooling) label.text = Mathf.CeilToInt(cd).ToString();
        }

        static Vector2 ItemSlot(int i) => new Vector2(-1.5f + i * ItemGap, ItemY);

        /// <summary>带进来的主动道具在技能和聚义下面排一行，从左往右挤着放。</summary>
        void PlaceItems()
        {
            var btns = new[] { _mengBtn, _baoziBtn, _shuBtn, _fuBtn, _trapBtn };
            int shown = 0;
            foreach (var b in btns) if (b.gameObject.activeSelf) shown++;
            shown = Mathf.Min(shown, 4);
            float x0 = 0.2f - (shown - 1) * ItemGap * 0.5f;
            int s = 0;
            for (int i = 0; i < btns.Length; i++)
            {
                if (!btns[i].gameObject.activeSelf) continue;
                if (s >= 4)
                {
                    btns[i].gameObject.SetActive(false);
                    continue;
                }
                btns[i].transform.localPosition = new Vector2(x0 + s * ItemGap, ItemY);
                s++;
            }
        }

        // ─────────────────────────── 技能钮 ───────────────────────────

        Transform _skillRoot;
        SpriteRenderer _skillDisc, _skillRing, _skillGlow, _skillIcon;
        TextMesh _skillName, _skillSub;
        CooldownRing _skillCd;
        readonly CooldownRing[] _itemCd = new CooldownRing[5];
        readonly TextMesh[] _itemCdText = new TextMesh[5];
        readonly bool[] _itemCooling = new bool[5];
        readonly float[] _itemFlash = new float[5];
        bool _skillWasReady, _skillHinted;

        /// <summary>聚义左边的圆钮：带头好汉的大招。冷却时暖色从十二点转满一圈，转好了亮起来，玩家点了才放。</summary>
        void BuildSkill()
        {
            _skillRoot = new GameObject("skill").transform;
            _skillRoot.SetParent(_root, false);
            _skillRoot.localPosition = new Vector2(SkillX, ButtonY);
            Gfx.Sprite(_skillRoot, Art.Circle, Pal.Shadow, new Vector2(0.04f, -0.09f), Vector2.one * (SkillD + 0.08f), 47);
            _skillGlow = Gfx.Sprite(_skillRoot, Art.Blob, Pal.GoldBright.A(0f), Vector2.zero, Vector2.one * (SkillD + 0.7f), 47);
            Gfx.Sprite(_skillRoot, Art.Circle, Pal.Ink, Vector2.zero, Vector2.one * (SkillD + 0.08f), 48);
            _skillDisc = Gfx.Sprite(_skillRoot, Art.Circle, Pal.Hex("3A342C"), Vector2.zero, Vector2.one * SkillD, 49, "skill");
            _skillCd = new CooldownRing(_skillRoot, SkillD * 0.96f, 50);
            _skillRing = Gfx.Sprite(_skillRoot, Art.Ring, Pal.InkSoft, Vector2.zero, Vector2.one * (SkillD + 0.12f), 52);
            _skillIcon = Gfx.Sprite(_skillRoot, Art.White, Color.white, new Vector2(0, 0.08f), Vector2.one * 0.74f, 53, "skillIcon");
            _skillIcon.enabled = false;
            _skillName = Gfx.Text(_skillRoot, "技", Fonts.Brush, 0.34f, Pal.Paper, new Vector2(0, 0.1f), 54);
            Gfx.Sprite(_skillRoot, Art.Rounded, Pal.Ink.A(0.85f), new Vector2(0, -SkillD * 0.42f), new Vector2(SkillD * 0.95f, 0.3f), 55);
            _skillSub = Gfx.Text(_skillRoot, "", Fonts.Kai, 0.19f, Pal.Paper, new Vector2(0, -SkillD * 0.42f), 56);
        }

        void SyncSkill()
        {
            var p = M.Player;
            var h = p.LeadHero;
            bool ready = h != null && h.UltCd <= 0 && !M.Over;
            float progress = 0f;
            if (h != null && h.UltCd > 0.04f)
            {
                float max = Mathf.Max(0.05f, p.UltMax(h));
                progress = 1f - Mathf.Clamp01(h.UltCd / max);
                _skillCd.ShowFill(progress);
            }
            else _skillCd.Hide();
            if (h == null)
            {
                _skillIcon.enabled = false;
                _skillName.text = "技";
                Gfx.SetHeight(_skillName, 0.34f);
                _skillSub.text = "凑好汉";
            }
            else
            {
                var icon = SkillIcon(h.Def);
                if (_skillIcon.sprite != icon) _skillIcon.sprite = icon;
                _skillIcon.enabled = true;
                _skillIcon.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.78f);
                _skillName.text = "";
                _skillSub.text = ready ? ShortUlt(h.Def.UltName) : $"{Mathf.CeilToInt(h.UltCd)} 秒";
            }
            _skillDisc.color = ready ? Pal.Hex("C0392B") : Pal.Hex("3A342C");
            _skillRing.color = ready ? Pal.GoldBright : Color.Lerp(Pal.InkSoft, Pal.Hex("E88862"), progress);
            _skillName.color = ready ? Pal.GoldBright : Pal.Paper.A(h == null ? 0.45f : 1f);
            float breathe = ready ? 0.5f + 0.5f * Mathf.Sin(Time.time * 5f) : 0f;
            _skillGlow.color = Pal.GoldBright.A(0.55f * breathe);
            _skillRoot.localScale = Vector3.one * (ready ? 1f + 0.05f * breathe : 1f);
            if (ready && !_skillWasReady)
            {
                Sfx.Play(Snd.Merge, 0.5f, 1.3f);
                if (!_skillHinted)
                {
                    _skillHinted = true;
                    Tip(new Vector2(SkillX + 1.2f, ButtonY + 1.0f), h.Def.Name + "的大招好了，点我放！");
                }
            }
            _skillWasReady = ready;
        }

        void PressSkill()
        {
            var p = M.Player;
            var h = p.LeadHero;
            var tipAt = new Vector2(SkillX + 1.2f, ButtonY + 1.0f);
            if (h == null) { Sfx.Play(Snd.Deny); Tip(tipAt, "先凑出一位好汉，才有大招"); return; }
            bool cooling = h.UltCd > 0.05f;
            bool cast = !cooling && p.CastLeadUlt();
            Sfx.Play(cast ? Snd.Tap : Snd.Deny);
            ShowSkillCard(h, cast, cooling);
        }

        void ShowSkillCard(HeroInstance h, bool cast, bool cooling)
        {
            var d = h.Def;
            string state = cast ? "已经放出"
                : cooling ? $"还要 {Mathf.CeilToInt(h.UltCd)} 秒"
                : "附近没有官兵，留着等会儿放";
            int cd = Mathf.Max(1, Mathf.CeilToInt(M.Player.UltMax(h)));
            string body = FitLines(26, 5, $"冷却 {cd} 秒　{state}", d.Ult, d.Skill);
            var seal = d.Disha ? Pal.Hex("E4D4F2") : Pal.TileHero;
            PlaceCard(string.IsNullOrEmpty(d.UltName) ? "大招" : d.UltName, d.Name, body, "", seal, SkillIcon(d));
        }

        static string ShortUlt(string name)
        {
            if (string.IsNullOrEmpty(name)) return "大招";
            return name.Length <= 4 ? name : name.Substring(0, 2);
        }

        /// <summary>技能钮上的图：招式本身能看懂的用招式图，其余用这名好汉的兵器。</summary>
        static Sprite SkillIcon(HeroDef d)
        {
            switch (d.UltKind)
            {
                case UltKind.Heal: return Art.Heart;
                case UltKind.Shield: return Art.Img("ui_shield");
                case UltKind.Rally: return Art.Img("ui_flag");
                case UltKind.Buns: return Art.Img("bun");
                case UltKind.Upgrade: return Art.Img("it_tianshu");
                case UltKind.Steal: return Art.Img("coin");
                case UltKind.HasteAll: return Art.Img("ui_bolt");
                case UltKind.SlowAll: return Art.Img("ui_cloud");
                case UltKind.Shovel: return Art.Img("shovel");
                case UltKind.Storm: return Art.Img("ui_bolt");
                case UltKind.Lasso: return Art.Img("wp_rope");
            }
            string weapon = d.WKind switch
            {
                WeaponKind.Blade => "wp_blade",
                WeaponKind.Spear => "wp_spear",
                WeaponKind.Bow => "wp_bow",
                WeaponKind.Axe => "wp_axe",
                WeaponKind.Club => "wp_club",
                WeaponKind.Whip => "wp_whip",
                WeaponKind.Hammer => "wp_hammer",
                WeaponKind.Fork => "wp_fork",
                WeaponKind.Sword => "wp_sword",
                WeaponKind.Stone => "wp_stone",
                WeaponKind.Rope => "wp_rope",
                WeaponKind.Staff => "wp_staff",
                WeaponKind.Halberd => "wp_halberd",
                WeaponKind.Cannon => "wp_cannon",
                WeaponKind.Fist => "wp_fist",
                _ => null,
            };
            return Art.Img(weapon ?? "ui_swords");
        }

        void SyncSide(int s, bool snap)
        {
            var side = M.Sides[s];
            var b = side.Board;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    if (_cellKind[s][c, r] != b.Cells[c, r]) BuildCell(s, c, r);
                    var piece = b.Pieces[c, r];
                    if (piece == null) continue;
                    var v = GetPiece(piece, s);
                    var hero = side.HeroAt(c, r);
                    v.Hero = hero;
                    if (v != _drag) v.Target = CellWorld(s, c, r);
                    if (snap) v.Root.localPosition = v.Target;
                }

            foreach (var h in side.Heroes)
            {
                if (!_frames.TryGetValue(h.Id, out var f))
                {
                    f = new FrameView(this, h, s);
                    _frames[h.Id] = f;
                }
                f.Seen = _frame;
                f.Update(h);
            }
        }

        void SyncCamp(bool snap)
        {
            var p = M.Player;
            for (int i = 0; i < p.Camp.Length; i++)
            {
                var piece = p.Camp[i];
                if (piece == null) continue;
                bool fresh = !_pieces.ContainsKey(piece.Id);
                var v = GetPiece(piece, 0);
                v.Hero = null;
                if (v != _drag) v.Target = CampWorld(i);
                if (fresh) v.Root.localPosition = v.Target;
                if (_rowRefresh && v != _drag) v.PopStagger(i * 0.075f);
                else if (fresh) v.PopIn();
                if (snap) v.Root.localPosition = v.Target;
            }
            _rowRefresh = false;
        }

        PieceView GetPiece(Piece p, int side)
        {
            if (!_pieces.TryGetValue(p.Id, out var v))
            {
                v = new PieceView(_root, p, side);
                _pieces[p.Id] = v;
            }
            v.Seen = _frame;
            return v;
        }

        void SyncEnemies()
        {
            for (int s = 0; s < 2; s++)
            {
                foreach (var e in M.Sides[s].Enemies)
                {
                    if (e.Dead) continue;
                    if (!_enemies.TryGetValue(e.Id, out var v))
                    {
                        v = new EnemyView(_root, e, s);
                        _enemies[e.Id] = v;
                    }
                    v.Seen = _frame;
                    v.Sync(e, World(s, e.Pos), Time.deltaTime);
                }
            }
        }

        void CleanupStale()
        {
            List<int> dead = null;
            foreach (var kv in _pieces)
                if (kv.Value.Seen != _frame) (dead ??= new List<int>()).Add(kv.Key);
            if (dead != null)
                foreach (var id in dead)
                {
                    var v = _pieces[id];
                    _pieces.Remove(id);
                    if (v == _drag) _drag = null;
                    if (v == _picked) { v.SetPicked(false); _picked = null; HideCard(); }
                    v.FadeOutAndDestroy();
                }

            dead = null;
            foreach (var kv in _enemies)
                if (kv.Value.Seen != _frame) (dead ??= new List<int>()).Add(kv.Key);
            if (dead != null)
                foreach (var id in dead) { Destroy(_enemies[id].Root.gameObject); _enemies.Remove(id); }

            dead = null;
            foreach (var kv in _frames)
                if (kv.Value.Seen != _frame) (dead ??= new List<int>()).Add(kv.Key);
            if (dead != null)
                foreach (var id in dead) { _frames[id].Destroy(); _frames.Remove(id); }

            foreach (var v in _pieces.Values) v.Tick(Time.deltaTime, v == _drag);
        }

        // ─────────────────────────── 事件表现 ───────────────────────────

        void Handle(SimEvent ev)
        {
            bool mine = ev.Side == 0;
            switch (ev.Type)
            {
                case Ev.Recruit:
                    if (mine)
                    {
                        Sfx.Play(Snd.Recruit);
                        if (M.Player.EmptyCampSlots == 0) After(0.28f, () => Sfx.Play(Snd.CampFull));
                        _rowRefresh = true;
                        SweepCamp();
                    }
                    break;
                case Ev.Merge:
                {
                    if (mine) Sfx.Play(Snd.Merge, 0.8f, 0.9f + ev.Val * 0.08f);
                    var pos = ev.P.x < 0 ? (Vector2?)null : World(ev.Side, ev.P);
                    if (ev.Side == 0 && ev.P.x < 0 && _pieces.TryGetValue(ev.A, out var pv)) pos = pv.Root.localPosition;
                    if (pos.HasValue)
                    {
                        Ring(pos.Value, 1.3f, Pal.GoldBright, 0.4f, 45);
                        FloatText(pos.Value + new Vector2(0, 0.45f), "升" + ToCn((int)ev.Val), Pal.Gold, 0.32f, 0.8f);
                    }
                    if (_pieces.TryGetValue(ev.A, out var mv)) mv.Punch(0.3f);
                    break;
                }
                case Ev.HeroFormed:
                {
                    var pos = World(ev.Side, ev.P);
                    Ring(pos, 3.2f, Pal.GoldBright, 0.6f, 45);
                    Splat(pos, 2.2f, Pal.GoldBright.A(0.45f), 0.9f);
                    if (mine) { Sfx.Play(Snd.Hero); _shake = Mathf.Max(_shake, 0.8f); }
                    else FloatText(pos + new Vector2(0, -0.7f), "对手召出 " + ev.Text, Pal.RedDeep, 0.3f, 1.6f);
                    break;
                }
                case Ev.HeroLevel:
                {
                    var pos = World(ev.Side, ev.P);
                    Ring(pos, 2.4f, Pal.GoldBright, 0.5f, 45);
                    FloatText(pos + new Vector2(0, 0.6f), ev.Text + " 升" + ToCn((int)ev.Val) + "级", Pal.Gold, mine ? 0.36f : 0.26f, 1.2f);
                    if (_frames.TryGetValue(ev.A, out var fv)) fv.Glow();
                    if (mine) Sfx.Play(Snd.Merge, 0.9f, 1.3f);
                    break;
                }
                case Ev.Attack:
                    UnitAttackFx(ev);
                    break;
                case Ev.HeroAttack:
                    HeroAttackFx(ev);
                    break;
                case Ev.Hit:
                    if (_enemies.TryGetValue(ev.A, out var hv)) hv.Flash();
                    if (mine) DamageNumber(World(ev.Side, ev.P), ev.Val, ev.Flag);
                    if (mine)
                    {
                        bool heavy = ev.Flag == 1 || ev.Flag == 2;
                        Sfx.Play(heavy ? Snd.Crit : Snd.Hit, heavy ? 0.3f : 0.1f, ev.Flag == 3 ? 0.7f : 1f);
                    }
                    break;
                case Ev.Kill:
                {
                    var pos = World(ev.Side, ev.P);
                    KillFx(ev);
                    if (mine)
                    {
                        Sfx.Play(Snd.Kill, 0.35f);
                        if (ev.Val > 0) BunFly(pos, (int)ev.Val);
                        if (ev.Flag == 2) { _shake = 1f; Sfx.Play(Snd.Ult); }
                    }
                    break;
                }
                case Ev.Leak:
                    if (ev.Flag == 1)
                    {
                        var at = World(ev.Side, ev.P);
                        InkSwirl(at, 1.2f, Pal.Red.A(0.8f), 0.5f);
                        if (mine) FloatText(at + Vector2.up * 0.5f, "挡！", Pal.Red, 0.32f, 0.6f);
                        break;
                    }
                    _goals[ev.Side].Hurt();
                    if (mine) { Sfx.Play(Snd.Leak); _shake = Mathf.Max(_shake, 0.7f); }
                    break;
                case Ev.Rain:
                    if (mine) { Sfx.Play(Snd.Rain); CoinRain(); }
                    break;
                case Ev.Ult:
                {
                    var pos = World(ev.Side, ev.P);
                    bool boss = ev.Flag == 1;
                    float r = Mathf.Min(ev.Val, 6f);
                    bool custom = !boss && HeroUltFx(ev);
                    if (r > 0.1f)
                    {
                        Ring(pos, r * 2f, boss ? Pal.Red : Pal.Ink, 0.5f, 44);
                        if (!boss && !custom) InkSwirl(pos, Mathf.Clamp(r * 2f, 1.6f, 7f), Pal.Ink.A(0.8f), 0.8f);
                    }
                    string word = ev.Text ?? "";
                    int colon = word.IndexOf('：');
                    if (colon > 0 && !boss) word = word.Substring(0, colon) + "！";
                    if (!string.IsNullOrEmpty(word)) FloatText(pos + new Vector2(0, 0.7f), word, boss ? Pal.Red : Pal.RedDeep, boss ? 0.58f : (mine ? 0.46f : 0.3f), boss ? 1.6f : 1.2f);
                    if (boss)
                    {
                        Sfx.Play(Snd.Warn, 0.9f);
                        _shake = Mathf.Max(_shake, 1.35f);
                        InkSwirl(pos, 3.2f, Pal.Red.A(0.8f), 0.7f);
                        Ring(pos, 4.2f, Pal.RedDeep, 0.55f, 46);
                        if (_enemies.TryGetValue(ev.A, out var bossView)) bossView.Flash();
                    }
                    if (mine && !boss)
                    {
                        Sfx.Play(Snd.Ult, 0.45f);
                        var ud = HeroDef.Get((HeroId)ev.B);
                        Sfx.Play(UltSound(ud), 0.85f, ud.Look == Look.Poison ? 0.8f : 1f);
                        _shake = Mathf.Max(_shake, 0.9f);
                    }
                    if (_frames.TryGetValue(ev.A, out var fv)) fv.Glow();
                    break;
                }
                case Ev.Dig:
                    DigFx(World(ev.Side, ev.P), ev.Flag == 1);
                    if (mine) Sfx.Play(Snd.Staff);
                    break;
                case Ev.Shovels:
                    if (mine)
                    {
                        FloatText(CampWorld(2) + Vector2.up * 0.9f, "铲子 +" + ev.Val, Pal.Willow, 0.38f, 1.2f);
                        Sfx.Play(Snd.Recruit, 0.8f, 1.2f);
                    }
                    break;
                case Ev.Rescue:
                    RescueFx(ev.Side);
                    break;
                case Ev.Baozi:
                {
                    var at = CellWorld(ev.Side, M.Sides[ev.Side].Board.Goal.c, M.Sides[ev.Side].Board.Goal.r);
                    bool good = ev.Flag == 1;
                    FloatText(at + Vector2.up * 0.9f, good ? "包子真香！回一颗心" : "吃坏肚子！掉一颗心", good ? Pal.Willow : Pal.Red, 0.32f, 1.6f);
                    Splat(at, 1.6f, (good ? Pal.GoldBright : Pal.Red).A(0.4f), 0.8f);
                    if (!good) _goals[ev.Side].Hurt();
                    Sfx.Play(good ? Snd.Rain : Snd.Leak);
                    break;
                }
                case Ev.Buns:
                    if (mine) BunFly(World(ev.Side, ev.P), (int)ev.Val);
                    break;
                case Ev.Heal:
                {
                    var at = CellWorld(ev.Side, M.Sides[ev.Side].Board.Goal.c, M.Sides[ev.Side].Board.Goal.r);
                    Splat(at, 1.8f, Pal.Willow.A(0.45f), 0.8f);
                    Ring(at, 2.2f, Pal.Willow, 0.5f, 45);
                    FloatText(at + Vector2.up * 0.9f, "回血 +" + ev.Val, Pal.Willow, mine ? 0.36f : 0.28f, 1.3f);
                    if (mine) Sfx.Play(Snd.UltHeal, 0.8f);
                    break;
                }
                case Ev.Steal:
                {
                    var at = World(ev.Side, ev.P);
                    FloatText(at + Vector2.up * 0.7f, (mine ? "偷来炊饼 +" : "被偷炊饼 -") + ev.Val, mine ? Pal.Gold : Pal.Red, 0.34f, 1.2f);
                    if (mine) BunFly(at, (int)ev.Val);
                    Sfx.Play(Snd.Rain, mine ? 0.6f : 0.3f, 1.3f);
                    break;
                }
                case Ev.Bond:
                {
                    var at = World(ev.Side, ev.P);
                    Ring(at, 3.6f, Pal.Hex("D9822B"), 0.65f, 45);
                    Splat(at, 2.6f, Pal.GoldBright.A(0.35f), 0.9f);
                    if (!mine) FloatText(at + new Vector2(0, -0.7f), "对手羁绊 " + ev.Text, Pal.RedDeep, 0.28f, 1.4f);
                    break;
                }
                case Ev.Dismiss:
                    if (mine)
                    {
                        var at = ev.P.x < 0 ? (Vector2)_recruitBtn.transform.localPosition : World(0, ev.P);
                        Splat(at, 0.9f, Pal.InkSoft.A(0.5f), 0.5f);
                        FloatText(at + Vector2.up * 0.6f, "遣散 +" + ev.Val, Pal.InkSoft, 0.34f, 0.9f);
                        Sfx.Play(Snd.Tap, 0.8f, 0.7f);
                    }
                    break;
                case Ev.ItemUsed:
                    if (ev.Flag == 1) { Ring(World(ev.Side, ev.P), 2.4f, Pal.InkSoft, 0.5f, 44); Splat(World(ev.Side, ev.P), 1.6f, Pal.InkSoft.A(0.6f), 0.9f); }
                    if (ev.Flag == 4 && mine)
                    {
                        _shake = 1.25f;
                        Ring(new Vector2(0f, MyBottom + 2.4f), 6.5f, Pal.Ink, 0.5f, 46);
                        FloatText(new Vector2(0f, MyBottom + 3.1f), "跌进陷阱", Pal.Ink, 0.42f, 1.1f);
                    }
                    if (mine) { Sfx.Play(Snd.Ult); _shake = Mathf.Max(_shake, ev.Flag == 4 ? 1.25f : 0.6f); }
                    if (mine && ev.Flag != 1) ItemSplash(ev.Text);
                    break;
                case Ev.BossSpawn:
                    Sfx.Play(Snd.Warn);
                    _shake = Mathf.Max(_shake, 1.2f);
                    break;
                case Ev.EliteWarn:
                    Sfx.Play(Snd.Warn, 0.6f);
                    break;
            }
        }

        /// <summary>道具发动：图标从按钮飞到我方棋盘中间放大，背后光芒转一圈，落地一圈冲击波。</summary>
        void ItemSplash(string name)
        {
            string icon;
            SpriteRenderer btn;
            switch (name)
            {
                case "攻速符": icon = "it_fu"; btn = _fuBtn; break;
                case "蒙汗药": icon = "it_menghan"; btn = _mengBtn; break;
                case "陷阱": icon = "it_trap"; btn = _trapBtn; break;
                case "天书": icon = "it_tianshu"; btn = _shuBtn; break;
                default: return;
            }
            Vector2 from = btn.transform.localPosition;
            var to = new Vector2(0f, MyBottom + 3f);
            var holder = new GameObject("itemSplash").transform;
            holder.SetParent(_fxRoot, false);
            holder.localPosition = from;
            var rays = Gfx.Sprite(holder, Art.Img("ui_rays"), Color.white.A(0.75f), Vector2.zero, Vector2.one * 0.1f, 90, "rays");
            var ic = Gfx.Sprite(holder, Art.Img(icon), Color.white, Vector2.zero, Vector2.one * 0.8f, 91, "icon");
            bool landed = false;
            Tween.Run(holder, 1.15f, k =>
            {
                if (!holder) return;
                float fly = Mathf.Clamp01(k / 0.3f);
                holder.localPosition = Vector2.Lerp(from, to, Tween.OutCubic(fly));
                float s = k < 0.3f ? Mathf.Lerp(0.8f, 2.2f, Tween.OutBack(fly)) : 2.2f + (k - 0.3f) * 0.5f;
                ic.transform.localScale = Vector3.one * s;
                rays.transform.localScale = Vector3.one * (s * 1.7f * fly);
                rays.transform.localRotation = Quaternion.Euler(0, 0, -k * 220f);
                float a = k < 0.75f ? 1f : 1f - (k - 0.75f) / 0.25f;
                ic.color = Color.white.A(a);
                rays.color = Color.white.A(0.75f * a);
                if (!landed && k >= 0.3f)
                {
                    landed = true;
                    Ring(to, 4.5f, Pal.GoldBright, 0.45f, 89);
                    InkDrops(to, 8);
                }
            }, () => { if (holder) Destroy(holder.gameObject); });
        }

        // ─────────────────────────── 特效 ───────────────────────────

        sealed class Fx
        {
            public GameObject Go;
            public SpriteRenderer Sr;
            public TextMesh Tm;
            public float Age, Life;
            public Action<Fx, float> Step;
            public Vector2 A, B;
            public float F;
            public Color C;
        }

        Fx NewSpriteFx(Sprite s, Color c, Vector2 pos, Vector2 size, int order, float life, Action<Fx, float> step)
        {
            var fx = new Fx { Life = life, Step = step, C = c };
            fx.Sr = Gfx.Sprite(_fxRoot, s, c, pos, size, order, "fx");
            fx.Go = fx.Sr.gameObject;
            _fx.Add(fx);
            return fx;
        }

        void UpdateFx(float dt)
        {
            for (int i = _fx.Count - 1; i >= 0; i--)
            {
                var f = _fx[i];
                f.Age += dt;
                float k = Mathf.Clamp01(f.Age / f.Life);
                if (f.Go == null) { _fx.RemoveAt(i); continue; }
                f.Step(f, k);
                if (k >= 1f)
                {
                    Destroy(f.Go);
                    _fx.RemoveAt(i);
                }
            }
        }

        void Ring(Vector2 pos, float size, Color c, float life, int order)
        {
            NewSpriteFx(Art.Ring, c, pos, Vector2.one * 0.2f, order, life, (f, k) =>
            {
                float s = Mathf.Lerp(0.2f, size, Tween.OutCubic(k));
                f.Go.transform.localScale = new Vector3(s, s, 1);
                f.Sr.color = f.C.A(f.C.a * (1 - k));
            });
        }

        void Splat(Vector2 pos, float size, Color c, float life)
        {
            var spr = UnityEngine.Random.value < 0.5f ? Art.Blob : Art.Blob2;
            var fx = NewSpriteFx(spr, c, pos, Vector2.one * size * 0.5f, 33, life, (f, k) =>
            {
                float s = Mathf.Lerp(size * 0.5f, size, Tween.OutCubic(Mathf.Min(1, k * 3)));
                f.Go.transform.localScale = new Vector3(s, s, 1);
                f.Sr.color = f.C.A(f.C.a * (1 - k * k));
            });
            fx.Go.transform.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360f));
        }

        /// <summary>击杀时飞溅的墨点。</summary>
        void InkDrops(Vector2 pos, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var dir = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.3f, 0.75f);
                float s = UnityEngine.Random.Range(0.07f, 0.15f);
                var fx = NewSpriteFx(Art.Circle, Pal.Ink.A(0.75f), pos, Vector2.one * s, 34, 0.45f, (f, k) =>
                {
                    f.Go.transform.localPosition = f.A + (f.B - f.A) * Tween.OutCubic(k);
                    f.Sr.color = f.C.A(f.C.a * (1 - k * k));
                });
                fx.A = pos;
                fx.B = pos + dir;
            }
        }

        /// <summary>好汉出手时在棋盘上闪现兵器：刀抡一圈、枪往前捅、弓拉满。</summary>
        void WeaponFlash(Vector2 pos, string img, Vector2 dir, UnitKind style, float size)
        {
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var fx = NewSpriteFx(Art.Img(img), Color.white, pos, Vector2.one * size, 43, 0.3f, (f, k) =>
            {
                float a = Mathf.Clamp01(Mathf.Min(k / 0.15f, (1 - k) / 0.3f));
                f.Sr.color = Color.white.A(a);
                var t = f.Go.transform;
                switch (style)
                {
                    case UnitKind.Dao: t.localRotation = Quaternion.Euler(0, 0, ang - 45f + Mathf.Lerp(90f, -90f, Tween.OutCubic(k))); break;
                    case UnitKind.Qiang: t.localPosition = f.A + dir * Mathf.Sin(k * Mathf.PI) * 0.6f; t.localRotation = Quaternion.Euler(0, 0, ang - 45f); break;
                    default:
                        t.localRotation = Quaternion.Euler(0, 0, ang - 180f);
                        t.localScale = new Vector3(size * (1f - 0.25f * Mathf.Sin(Mathf.Min(1, k * 1.6f) * Mathf.PI)), size, 1);
                        break;
                }
            });
            fx.A = pos;
        }

        /// <summary>大招：一圈墨旋风边转边扩散。</summary>
        void InkSwirl(Vector2 pos, float size, Color c, float life)
        {
            float spin = UnityEngine.Random.value < 0.5f ? 1f : -1f;
            NewSpriteFx(Art.Swirl, c, pos, Vector2.one * size * 0.4f, 44, life, (f, k) =>
            {
                float s = Mathf.Lerp(0.4f, 1.05f, Tween.OutCubic(k)) * size;
                f.Go.transform.localScale = new Vector3(s, s, 1);
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, spin * -540f * Tween.OutCubic(k));
                f.Sr.color = f.C.A(f.C.a * (k < 0.6f ? 1f : (1 - k) / 0.4f));
            });
        }

        void DigFx(Vector2 pos, bool water)
        {
            var dirt = water ? Pal.Water : Pal.Earth;
            Splat(pos, 1.3f, dirt.A(0.55f), 0.7f);
            for (int i = 0; i < 10; i++)
            {
                var dir = new Vector2(UnityEngine.Random.Range(-0.8f, 0.8f), UnityEngine.Random.Range(0.3f, 1.1f));
                var fx = NewSpriteFx(Art.Circle, dirt, pos, Vector2.one * UnityEngine.Random.Range(0.08f, 0.16f), 46, 0.6f, (f, k) =>
                {
                    f.Go.transform.localPosition = f.A + (f.B - f.A) * k + new Vector2(0, -k * k * 1.4f);
                    f.Sr.color = f.C.A(1 - k);
                });
                fx.A = pos;
                fx.B = pos + dir;
            }
            NewSpriteFx(Art.Img("shovel"), Color.white, pos + new Vector2(0.25f, 0.35f), Vector2.one * 0.8f, 47, 0.45f, (f, k) =>
            {
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(30f, -50f, Tween.OutCubic(k)));
                f.Sr.color = Color.white.A(k < 0.7f ? 1 : (1 - k) / 0.3f);
            });
            FloatText(pos + Vector2.up * 0.55f, water ? "开出水寨" : "开荒", Pal.Willow, 0.3f, 0.8f);
        }

        void RescueFx(int side)
        {
            var b = M.Sides[side].Board;
            var goal = World(side, b.Goal.Center);
            for (int i = 0; i < 7; i++)
            {
                float from = b.PathLength * Mathf.Lerp(0.95f, 0.3f, i / 6f);
                float to = Mathf.Max(Tuning.SpawnCover, from - b.PathLength * 0.55f);
                var a = World(side, b.PosAt(from));
                var c = World(side, b.PosAt(to));
                var fx = NewSpriteFx(Art.SoftDot, Pal.Paper, a, new Vector2(0.7f, 0.16f), 46, 0.42f, (f, k) =>
                {
                    f.Go.transform.localPosition = Vector2.Lerp(f.A, f.B, Tween.OutCubic(k));
                    f.Sr.color = Pal.Paper.A(1f - k);
                });
                fx.A = a;
                fx.B = c;
                fx.Go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(c.y - a.y, c.x - a.x) * Mathf.Rad2Deg);
            }
            FloatText(goal + Vector2.up * 0.55f, "推回去", Pal.Red, 0.4f, 0.9f);
            if (side == 0)
            {
                _shake = 0.7f;
                Sfx.Play(Snd.UltWind);
                _goals[0].Say("有惊无险！", 2.2f);
            }
        }

        public void FloatText(Vector2 pos, string s, Color c, float h, float life)
        {
            var fx = new Fx { Life = life, C = c, A = pos };
            fx.Tm = Gfx.Text(_fxRoot, s, Fonts.Brush, h, c, pos, 48, TextAnchor.MiddleCenter, "ftx");
            fx.Go = fx.Tm.gameObject;
            var outline = Gfx.Text(fx.Go.transform, s, Fonts.Brush, h, Pal.Paper.A(0.85f), new Vector2(0.025f, -0.025f), 47);
            outline.transform.localScale = Vector3.one;
            outline.characterSize = fx.Tm.characterSize / fx.Go.transform.localScale.x;
            fx.Step = (f, k) =>
            {
                f.Go.transform.localPosition = f.A + new Vector2(0, Tween.OutCubic(k) * 0.6f);
                float a = k < 0.7f ? 1 : 1 - (k - 0.7f) / 0.3f;
                f.Tm.color = f.C.A(a);
                outline.color = Pal.Paper.A(0.85f * a);
                float s = k < 0.15f ? Tween.OutBack(k / 0.15f) : 1f;
                f.Go.transform.localScale = new Vector3(s, s, 1);
            };
            _fx.Add(fx);
        }

        void DamageNumber(Vector2 pos, float val, int flag)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(val));
            bool big = flag > 0 || n >= 18;
            float h = flag == 2 || n >= 40 ? 0.52f : big ? 0.38f : 0.26f;
            string s = flag == 2 ? "克" + n : flag == 3 ? "半" + n : flag == 1 ? n + "!" : n.ToString();
            Color c = flag == 2 ? Pal.Red : flag == 1 ? Pal.Gold : flag == 3 ? Pal.Hex("8A5A44") : Pal.Ink;
            var at = pos + new Vector2(UnityEngine.Random.Range(-0.2f, 0.2f), 0.4f);
            var fx = new Fx { Life = big ? 0.8f : 0.5f, C = c, A = at };
            fx.Tm = Gfx.Text(_fxRoot, s, Fonts.Kai, h, c, at, 49, TextAnchor.MiddleCenter, "dmg");
            fx.Go = fx.Tm.gameObject;
            var rim = new TextMesh[4];
            for (int i = 0; i < 4; i++)
            {
                var o = new Vector2(i % 2 == 0 ? -1 : 1, i < 2 ? -1 : 1) * 0.022f;
                rim[i] = Gfx.Text(fx.Go.transform, s, Fonts.Kai, h, Pal.Paper, o, 48);
                rim[i].transform.localScale = Vector3.one;
                rim[i].characterSize = fx.Tm.characterSize / fx.Go.transform.localScale.x;
            }
            float drift = UnityEngine.Random.Range(-0.25f, 0.25f);
            fx.Step = (f, k) =>
            {
                f.Go.transform.localPosition = f.A + new Vector2(drift * k, Tween.OutCubic(k) * (big ? 0.7f : 0.4f));
                float pop = k < 0.14f ? Mathf.Lerp(big ? 2.2f : 1.6f, 1f, Tween.OutCubic(k / 0.14f)) : 1f;
                f.Go.transform.localScale = Vector3.one * pop;
                float a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                f.Tm.color = f.C.A(a);
                foreach (var r in rim) r.color = Pal.Paper.A(0.9f * a);
            };
            _fx.Add(fx);
        }

        void BunFly(Vector2 from, int n)
        {
            var target = (Vector2)_bunIcon.position;
            NewSpriteFx(Art.Img("bun"), Color.white, from, Vector2.one * 0.3f, 47, 0.6f, (f, k) =>
            {
                float e = k * k;
                var p = Vector2.Lerp(f.A, f.B, e) + new Vector2(0, Mathf.Sin(k * Mathf.PI) * 0.8f);
                f.Go.transform.localPosition = p;
                if (k >= 0.99f) Tween.Punch(_bunIcon, 0.15f, 0.25f, _bunIconScale);
            }).A = from;
            _fx[_fx.Count - 1].B = target;
            FloatText(from + new Vector2(0, 0.35f), "+" + n, Pal.Gold, n > 1 ? 0.3f : 0.24f, 0.6f);
        }

        void CoinRain()
        {
            for (int i = 0; i < 36; i++)
            {
                var start = new Vector2(UnityEngine.Random.Range(-4f, 4f), MyBottom + Board.Rows + UnityEngine.Random.Range(0f, 2.5f));
                var end = start + new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), -UnityEngine.Random.Range(5f, 8f));
                var fx = NewSpriteFx(Art.Img("coin"), Color.white, start, Vector2.one * 0.28f, 47, UnityEngine.Random.Range(0.9f, 1.5f), (f, k) =>
                {
                    f.Go.transform.localPosition = Vector2.Lerp(f.A, f.B, k);
                    f.Go.transform.localRotation = Quaternion.Euler(0, 0, k * 360f * f.F);
                    f.Sr.color = Color.white.A(k < 0.8f ? 1 : (1 - k) * 5);
                });
                fx.A = start;
                fx.B = end;
                fx.F = UnityEngine.Random.Range(-1.5f, 1.5f);
            }
        }

        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);

        // ─────────────────────────── 输入 ───────────────────────────

        Vector2 PointerWorld()
        {
            var v = _cam.ScreenToViewportPoint(Input.mousePosition);
            if (v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f) return new Vector2(9999f, 9999f);
            return _cam.ScreenToWorldPoint(Input.mousePosition);
        }

        static bool In(Vector2 p, Vector2 center, Vector2 size) =>
            Mathf.Abs(p.x - center.x) <= size.x * 0.5f && Mathf.Abs(p.y - center.y) <= size.y * 0.5f;

        static bool TryMyCell(Vector2 w, out int c, out int r)
        {
            c = Mathf.FloorToInt(w.x - Left);
            r = Mathf.FloorToInt(w.y - MyBottom);
            return Board.InBounds(c, r);
        }

        int CampAt(Vector2 w)
        {
            for (int i = 0; i < Tuning.CampSlots; i++)
                if (In(w, CampWorld(i), new Vector2(1.1f, 1.15f))) return i;
            return -1;
        }

        void ShowAd(string reason, Action onReward)
        {
            Paused = true;
            AdStub.Show(reason, () =>
            {
                Paused = false;
                onReward();
            }, () => Paused = false);
        }

        void HandleInput()
        {
            var p = M.Player;
            var w = PointerWorld();

            if (Input.GetMouseButtonDown(0))
            {
                _idleT = 0;
                if (Ui.PointerOverUi()) return;
                if (OverCard(w)) { _downPos = w; _dragMoved = false; return; }
                _downPos = w;
                _dragMoved = false;
                if (In(w, _recruitBtn.transform.localPosition, new Vector2(2.8f, 1.1f)))
                {
                    if (p.Recruit()) { Sfx.Play(Snd.Tap); Tween.Punch(_recruitBtn.transform, 0.15f, 0.12f); }
                    else
                    {
                        bool full = p.EmptyCampSlots == 0;
                        Sfx.Play(full ? Snd.CampFull : Snd.Deny);
                        Tip(_recruitBtn.transform.localPosition + Vector3.up * 1.1f, full ? "山寨满了，先把字拖上战场" : "炊饼不够，多杀几个官兵");
                    }
                    return;
                }
                if (In(w, _pauseBtn.transform.localPosition, new Vector2(0.9f, 0.9f))) { Sfx.Play(Snd.Tap); OnPauseClicked?.Invoke(); return; }
                if (In(w, _skillRoot.localPosition, Vector2.one * (SkillD + 0.2f))) { PressSkill(); return; }
                var itemHit = new Vector2(ItemGap * 0.95f, 0.95f);
                if (HitItem(_mengBtn, w, itemHit)) { PressActive(() => p.UseMengHan(), () => p.MengCd, Tuning.CdMeng, () => p.Items.MengHan, "蒙汗药", "全场官兵昏睡 3 秒", "it_menghan"); return; }
                if (HitItem(_baoziBtn, w, itemHit)) { PressActive(() => p.UseBaozi(), () => p.BaoziCd, Tuning.CdBaozi, () => p.Items.Baozi, "包子", "五成半回一颗心，否则掉一颗", "bun"); return; }
                if (HitItem(_shuBtn, w, itemHit)) { PressActive(() => p.UseTianShu(), () => p.ShuCd, Tuning.CdShu, () => p.Items.TianShu, "天书", "场上最低级的兵升一级", "it_tianshu"); return; }
                if (HitItem(_fuBtn, w, itemHit)) { PressActive(() => p.UseFu(), () => p.FuCd, Tuning.CdFu, () => p.Items.Fu, "攻速符", "单位攻速 +40%，持续十二秒", "it_fu"); return; }
                if (HitItem(_trapBtn, w, itemHit)) { PressActive(() => p.UseTrap(), () => p.TrapCd, Tuning.CdTrap, () => p.Items.Trap, "陷阱", "敌人跌回去，一时爬不上来", "it_trap"); return; }
                if (_shovelRoot.gameObject.activeSelf && In(w, _shovelRoot.localPosition, new Vector2(0.95f, 1.1f))) { Sfx.Play(Snd.Tap); ShovelAd(); return; }
                if (_rescueRoot.gameObject.activeSelf && In(w, _rescueRoot.localPosition, new Vector2(0.95f, 1.2f))) { Sfx.Play(Snd.Tap); RescueAd(); return; }

                int slot = CampAt(w);
                if (slot >= 0 && p.Camp[slot] != null && _pieces.TryGetValue(p.Camp[slot].Id, out var cv))
                {
                    BeginDrag(cv, slot, default);
                    return;
                }
                if (TryMyCell(w, out var c, out var r) && p.Board.Pieces[c, r] != null && _pieces.TryGetValue(p.Board.Pieces[c, r].Id, out var bv))
                {
                    BeginDrag(bv, -1, new CellPos(c, r));
                    return;
                }
                if (TryMyCell(w, out c, out r) && p.Board.Diggable(c, r))
                {
                    Sfx.Play(Snd.Deny);
                    Tip(CellWorld(0, c, r) + Vector2.up * 0.9f, "草地要先用铲子开荒才能摆字");
                }
            }

            if (_drag != null)
            {
                if ((w - _downPos).magnitude > 0.18f) _dragMoved = true;
                if (_dragMoved) { _drag.Target = w + new Vector2(0, 0.55f); HideCard(); }
                ShowHighlights(_drag.Piece, w);

                if (Input.GetMouseButtonUp(0)) EndDrag(w);
            }
        }

        static bool HitItem(SpriteRenderer btn, Vector2 w, Vector2 size) =>
            btn.gameObject.activeSelf && In(w, btn.transform.localPosition, size);

        void PressActive(Func<bool> use, Func<float> cd, float max, Func<int> count, string name, string desc, string icon)
        {
            if (cd() > 0.05f)
            {
                Sfx.Play(Snd.Deny);
                ShowItemCard(name, desc, icon, count(), cd(), max, false, true);
                return;
            }
            bool ok = use();
            Sfx.Play(ok ? Snd.Tap : Snd.Deny);
            ShowItemCard(name, desc, icon, count(), cd(), max, ok, false);
        }

        void ShowItemCard(string name, string desc, string icon, int count, float cd, float max, bool used, bool cooling)
        {
            string state = cooling ? $"还要 {Mathf.CeilToInt(cd)} 秒"
                : used ? "已经用掉"
                : name == "天书" ? "场上没有能升级的兵" : "现在用不了";
            string body = FitLines(26, 4, desc, $"还剩 {Mathf.Max(0, count)} 个　冷却 {Mathf.CeilToInt(max)} 秒", state);
            PlaceCard(name, "道具", body, "", Pal.Tile, Art.Img(icon));
        }

        public void ShovelAd()
        {
            var p = M.Player;
            if (M.Over) return;
            var tipAt = new Vector2(ShovelOfferX - 0.8f, CampY + 1f);
            if (_shovelAds >= MaxShovelAds) { Tip(tipAt, "这一局的铲子广告看完了"); return; }
            if (!p.Board.AnyDiggable()) { Tip(tipAt, "地已经全开出来了"); return; }
            _shovelRoot.gameObject.SetActive(false);
            _shovelForce = false;
            ShowAd("鲁智深送铲子", () =>
            {
                _shovelAds++;
                p.GrantShovels(Tuning.AdShovels);
                Tip(new Vector2(0, CampY + 1.3f), $"恭喜获得 {Tuning.AdShovels} 把铲子！\n拖到草地上开荒");
            });
        }

        public void RescueAd()
        {
            var p = M.Player;
            if (p.RescueUsed || M.Over) return;
            ShowAd("公孙胜作法", () => p.Rescue());
        }

        bool OverRecruit(Vector2 w) => In(w, _recruitBtn.transform.localPosition, new Vector2(2.9f, 1.3f));

        void BeginDrag(PieceView v, int campSlot, CellPos cell)
        {
            _drag = v;
            _dragCamp = campSlot;
            _dragCell = cell;
            v.SetDragging(true);
            Sfx.Play(Snd.Tap, 0.55f);
            OnDragStart?.Invoke(v.Piece);
        }

        void EndDrag(Vector2 w)
        {
            var p = M.Player;
            var v = _drag;
            _drag = null;
            v.SetDragging(false);
            ClearHighlights();

            if (!_dragMoved)
            {
                ShowInfo(v);
                return;
            }

            var drop = w + new Vector2(0, 0.35f);
            bool ok = false;
            if (OverRecruit(w))
            {
                ok = _dragCamp >= 0 ? p.DismissCamp(_dragCamp) : p.DismissCell(_dragCell.c, _dragCell.r);
                if (ok) return;
            }
            int slot = CampAt(drop);
            if (slot < 0) slot = CampAt(w);
            if (slot >= 0)
            {
                ok = _dragCamp >= 0 ? p.CampToCamp(_dragCamp, slot) : p.CellToCamp(_dragCell.c, _dragCell.r, slot);
            }
            else if (TryMyCell(drop, out var c, out var r) || TryMyCell(w, out c, out r))
            {
                ok = _dragCamp >= 0 ? p.CampToCell(_dragCamp, c, r) : p.CellToCell(_dragCell.c, _dragCell.r, c, r);
                if (ok) { Sfx.Play(Snd.Place, 0.8f); OnPlaced?.Invoke(); }
                else if (v.Piece.Type == PieceType.Shovel) Tip(CellWorld(0, c, r) + Vector2.up * 0.9f, "铲子只能挖草地");
                else if (p.Board.Diggable(c, r)) Tip(CellWorld(0, c, r) + Vector2.up * 0.9f, "这块是草地，先用铲子开荒");
            }
            if (ok && slot >= 0) Sfx.Play(Snd.Tap, 0.7f);
            if (!ok) { Sfx.Play(Snd.Deny); v.Punch(0.1f); }
        }

        void ShowHighlights(Piece piece, Vector2 w)
        {
            var p = M.Player;
            var b = p.Board;
            bool hasHover = TryMyCell(w + new Vector2(0, 0.35f), out var hc, out var hr);
            var nameTargets = piece.Type == PieceType.HeroChar ? NameTargets(piece.Char) : null;
            float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * 8);

            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    var t = b.Pieces[c, r];
                    Color col = Color.clear;
                    string mark = "";
                    if (piece.Type == PieceType.Shovel)
                    {
                        if (b.Diggable(c, r)) { col = Pal.Valid.A(0.35f + 0.25f * blink); mark = "挖"; }
                    }
                    else if (b.Placeable(c, r))
                    {
                        if (t != null && ReferenceEquals(t, piece)) col = Color.clear;
                        else if (Side.CanMerge(piece, t)) { col = Pal.GoldBright.A(0.55f + 0.25f * blink); mark = "合"; }
                        else if (t == null) { col = Pal.Valid.A(0.42f); mark = "+"; }
                        if (nameTargets != null && nameTargets.TryGetValue(new CellPos(c, r), out var nm))
                        {
                            col = Pal.Red.A(0.35f + 0.2f * blink);
                            mark = nm;
                        }
                    }
                    if (hasHover && c == hc && r == hr && col.a > 0) col = col.A(Mathf.Min(1, col.a + 0.35f));
                    _hl[c, r].color = col;
                    _hlMark[c, r].text = mark;
                }
            for (int i = 0; i < _campPlus.Length; i++)
                _campPlus[i].text = M.Player.Camp[i] == null ? "+" : "";
            if (_dragMoved && piece.Type == PieceType.Unit && hasHover && b.Placeable(hc, hr))
            {
                var d = UnitDef.Get(piece.Kind);
                float range = p.Items.Far && d.Range >= 2f ? d.Range * 2f : d.Range;
                ShowRange(CellWorld(0, hc, hr), range, RangeStyle.Unit, true);
            }
            else if (_rangeFromDrag) HideRange();
            DrawDragLine(w);
        }

        void DrawDragLine(Vector2 w)
        {
            if (!_dragMoved)
            {
                HideDragLine();
                return;
            }
            var from = _dragCamp >= 0 ? CampWorld(_dragCamp) : CellWorld(0, _dragCell.c, _dragCell.r);
            var to = w + new Vector2(0, 0.55f);
            var d = to - from;
            float len = d.magnitude;
            if (len < 0.25f) { HideDragLine(); return; }
            const float gap = 0.28f;
            int n = Mathf.Min(24, Mathf.FloorToInt(len / gap));
            while (_dragDots.Count < n)
                _dragDots.Add(Gfx.Sprite(_root, Art.Circle, Pal.InkSoft, Vector2.zero, Vector2.one * 0.12f, 60, "dragDot"));
            for (int i = 0; i < _dragDots.Count; i++)
            {
                var dot = _dragDots[i];
                dot.enabled = i < n;
                if (i >= n) continue;
                dot.transform.localPosition = Vector2.Lerp(from, to, (i + 1f) / (n + 1f));
                dot.color = Pal.InkSoft.A(0.9f);
            }
        }

        void HideDragLine()
        {
            foreach (var dot in _dragDots) dot.enabled = false;
        }

        void SweepCamp()
        {
            var bar = Gfx.Sprite(_root, Art.White, Pal.GoldBright.A(0.9f), new Vector2(-3.4f, CampY), new Vector2(0.35f, 1.05f), 60, "campSweep");
            Tween.Run(bar, 0.42f, k =>
            {
                if (!bar) return;
                bar.transform.localPosition = new Vector2(Mathf.Lerp(-3.4f, 2.8f, k), CampY);
                bar.color = Pal.GoldBright.A(0.9f * (1f - k));
            }, () => { if (bar) Destroy(bar.gameObject); });
            for (int i = 0; i < _campSlots.Length; i++)
            {
                var slot = _campSlots[i];
                Tween.Run(slot, 0.28f, k =>
                {
                    if (!slot) return;
                    slot.color = Color.Lerp(Pal.GoldBright, Pal.Paper, k);
                }, null, i * 0.075f);
            }
            FloatText(new Vector2(-0.4f, CampY + 0.85f), "整排刷新", Pal.Gold, 0.38f, 0.9f);
        }

        /// <summary>拖金字时：哪些空格放下去能和场上已有的字凑成（或接近）好汉名。</summary>
        Dictionary<CellPos, string> NameTargets(char ch)
        {
            var p = M.Player;
            var b = p.Board;
            var result = new Dictionary<CellPos, string>();
            NameMatcher.ReadDirs(false, out var dh, out var dv);
            foreach (var def in p.Pool)
            {
                for (int k = 0; k < def.Name.Length; k++)
                {
                    if (def.Name[k] != ch) continue;
                    for (int c = 0; c < Board.Cols; c++)
                        for (int r = 0; r < Board.Rows; r++)
                        {
                            var q = b.Pieces[c, r];
                            if (q == null || q.Type != PieceType.HeroChar || p.HeroAt(c, r) != null) continue;
                            int j = def.Name.IndexOf(q.Char);
                            if (j < 0 || j == k) continue;
                            foreach (var d in new[] { dh, dv })
                            {
                                int tc = c + d.c * (k - j), tr = r + d.r * (k - j);
                                if (!b.Placeable(tc, tr)) continue;
                                var occupant = b.Pieces[tc, tr];
                                if (occupant != null && occupant.Type != PieceType.Unit) continue;
                                result[new CellPos(tc, tr)] = def.Name;
                            }
                        }
                }
            }
            return result;
        }

        void ClearHighlights()
        {
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    _hl[c, r].color = Color.clear;
                    _hlMark[c, r].text = "";
                }
            for (int i = 0; i < _campPlus.Length; i++) if (_campPlus[i] != null) _campPlus[i].text = "";
            if (_rangeFromDrag) HideRange();
            HideDragLine();
        }

        // ─────────────────────────── 属性卡 ───────────────────────────

        void ShowInfo(PieceView v)
        {
            var piece = v.Piece;
            string title, level, body;
            float range = 0;
            var style = RangeStyle.Unit;
            Vector2 center = v.Root.localPosition;
            Color seal = Pal.Tile;
            if (piece.Type == PieceType.Unit)
            {
                var d = UnitDef.Get(piece.Kind);
                title = d.Glyph + "兵";
                level = $"Lv.{piece.Level}";
                range = M.Player.Items.Far && d.Range >= 2f ? d.Range * 2f : d.Range;
                body = $"类型：{d.TypeLabel}　射程：{range:0.#}格\n攻击力：{d.DamageAt(piece.Level):0.#}　攻速：{1f / d.CooldownAt(piece.Level):0.0}次/秒\n满级{UnitDef.MaxLevel}级 · {d.Role}";
            }
            else if (piece.Type == PieceType.Shovel)
            {
                title = "方便铲";
                level = "道具";
                seal = Pal.Earth;
                body = "拖到草地上，开出一块能摆字的白地\n招兵偶尔会掉，也能看广告领";
            }
            else if (v.Hero != null)
            {
                var h = v.Hero;
                var d = h.Def;
                title = d.Name;
                level = $"Lv.{h.Level}";
                seal = d.Disha ? Pal.Hex("E4D4F2") : Pal.TileHero;
                center = World(0, h.Pos);
                bool armed = M.Player.Armed(d);
                int wlv = armed && M.Player.Items.WeaponLv != null && (int)d.Id < M.Player.Items.WeaponLv.Length ? Mathf.Max(1, M.Player.Items.WeaponLv[(int)d.Id]) : 1;
                float wmul = armed ? HeroDef.ArmedMul(wlv, d.WGrade) : 1f;
                float bond = M.Player.BondBonus(d.Id);
                wmul *= 1f + bond;
                string xp = h.Level >= UnitDef.MaxLevel ? "已满级" : $"经验值 {Mathf.FloorToInt(h.Xp)}/{HeroInstance.XpNeed(h.Level)}　满级{UnitDef.MaxLevel}级";
                string atk = d.NoAttack ? "不出手" : $"攻击力：{d.DamageAt(h.Level) * (h.OnWater ? d.WaterBonus : 1f) * wmul:0.#}　攻速：{1f / d.CooldownAt(h.Level):0.0}次/秒";
                string gear = armed ? $"兵器 {d.Weapon}（{Weapons.Grades[Mathf.Clamp(d.WGrade, 0, 3)]}）伤害 +{Mathf.RoundToInt((HeroDef.ArmedMul(wlv, d.WGrade) - 1f) * 100f)}%　{Weapons.AffixText(d.WKind)}" : "";
                string bonds = "";
                foreach (var bd in M.Player.ActiveBonds) if (System.Array.IndexOf(bd.Members, d.Id) >= 0) bonds += (bonds.Length > 0 ? "、" : "") + bd.Name;
                if (bonds.Length > 0) gear = (gear.Length > 0 ? gear + "　" : "") + $"羁绊 {bonds}　伤害 +{Mathf.RoundToInt(bond * 100f)}%";
                string star = d.Rank > 0 ? d.Star : "隐藏好汉";
                body = FitLines(26, 5, $"{star}·{d.Nick}　类型：{d.TypeLabel}", atk, xp, d.Skill, gear);
                range = d.NoAttack ? 0f : d.RangeWith(M.Player.Items.Far);
                style = d.Disha ? RangeStyle.Disha : RangeStyle.Hero;
            }
            else
            {
                var names = new List<string>();
                foreach (var h in M.Player.Pool)
                    if (h.Name.IndexOf(piece.Char) >= 0) names.Add(h.Hidden ? "？？？" : h.Name);
                title = "「" + piece.Char + "」";
                level = $"Lv.{piece.Level}";
                seal = Pal.Hex("F8E7B0");
                body = FitLines(26, 5, "金字，还没成名", "可组成：" + string.Join("、", names), "按名字顺序左到右或上到下摆好", "凑成好汉后，击杀官兵才一起升级");
            }

            PlaceCard(title, level, body, piece.Glyph.ToString(), seal, null);

            SetPicked(v);
            if (range > 0f && piece.Type != PieceType.Shovel) ShowRange(center, range, style, false);
            else HideRange();
            _cardT = 4f;
            v.Punch(0.15f);
        }

        /// <summary>纸签钉在山寨下沿。原点在纸签顶边中点，弹出时只往下长，不会拱进棋盘。</summary>
        void PlaceCard(string title, string level, string body, string glyph, Color seal, Sprite icon)
        {
            const float width = 7.56f;
            const float pitch = 0.26f;
            int lines = 1;
            foreach (var ch in body) if (ch == '\n') lines++;
            if (string.IsNullOrEmpty(body)) lines = 0;
            float h = Mathf.Clamp(0.72f + lines * pitch, 1.28f, 2.02f);

            _card.gameObject.SetActive(true);
            _card.localPosition = new Vector2(0f, CampY - 0.66f);
            _cardSize = new Vector2(width, h);
            float cy = -h * 0.5f;
            _cardShadow.transform.localPosition = new Vector2(0.04f, cy - 0.05f);
            _cardShadow.size = _cardSize;
            _cardBg.transform.localPosition = new Vector2(0f, cy);
            _cardBg.size = _cardSize;
            _cardPaper.transform.localPosition = new Vector2(0f, cy);
            _cardPaper.size = _cardSize - new Vector2(0.1f, 0.1f);

            const float sealS = 0.68f;
            float sealX = -width * 0.5f + 0.18f + sealS * 0.5f;
            float sealY = -0.18f - sealS * 0.5f;
            _cardSeal.transform.localPosition = new Vector2(sealX, sealY);
            _cardSeal.size = new Vector2(sealS, sealS);
            _cardSeal.color = seal;
            _cardGlyph.text = icon == null ? glyph : "";
            _cardGlyph.transform.localPosition = new Vector2(sealX, sealY - 0.02f);
            _cardIcon.enabled = icon != null;
            if (icon != null)
            {
                _cardIcon.sprite = icon;
                _cardIcon.transform.localPosition = new Vector2(sealX, sealY);
                _cardIcon.transform.localScale = Vector3.one * (sealS * 0.86f);
            }

            float textL = sealX + sealS * 0.5f + 0.16f;
            _cardTitle.text = title;
            _cardTitle.transform.localPosition = new Vector2(textL, -0.32f);
            _cardLevel.text = level;
            _cardLevel.transform.localPosition = new Vector2(width * 0.5f - 0.2f, -0.32f);
            float ruleW = width * 0.5f - 0.2f - textL;
            _cardRule.transform.localPosition = new Vector2(textL + ruleW * 0.5f, -0.54f);
            _cardRule.transform.localScale = new Vector3(Mathf.Max(0.4f, ruleW), 0.04f, 1f);
            _cardBody.text = body;
            _cardBody.transform.localPosition = new Vector2(textL, -0.60f);
            Tween.Pop(_card, 0.18f);
        }

        bool OverCard(Vector2 w)
        {
            if (_card == null || !_card.gameObject.activeSelf) return false;
            var top = (Vector2)_card.localPosition;
            return w.x >= top.x - _cardSize.x * 0.5f && w.x <= top.x + _cardSize.x * 0.5f
                && w.y <= top.y + 0.05f && w.y >= top.y - _cardSize.y;
        }

        static string FitLines(int width, int maxLines, params string[] parts)
        {
            var sb = new System.Text.StringBuilder();
            int lines = 0;
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                int col = 0;
                foreach (var ch in part)
                {
                    if (ch == '\n')
                    {
                        if (lines + 1 >= maxLines) return sb.ToString();
                        sb.Append('\n');
                        lines++;
                        col = 0;
                        continue;
                    }
                    if (col >= width)
                    {
                        if (lines + 1 >= maxLines) return sb.ToString();
                        sb.Append('\n');
                        lines++;
                        col = 0;
                    }
                    if (col == 0 && sb.Length > 0 && sb[sb.Length - 1] != '\n')
                    {
                        if (lines + 1 >= maxLines) return sb.ToString();
                        sb.Append('\n');
                        lines++;
                    }
                    sb.Append(ch);
                    col++;
                }
            }
            return sb.ToString();
        }

        /// <summary>验收截图：在指定位置画出某个射程圆。</summary>
        public void PreviewRange(float range, Vector2 center, bool hero = false, bool disha = false)
        {
            HideRange();
            ShowRange(center, range, disha ? RangeStyle.Disha : hero ? RangeStyle.Hero : RangeStyle.Unit, false);
        }

        void SetPicked(PieceView v)
        {
            if (_picked != null && _picked != v) _picked.SetPicked(false);
            _picked = v;
            if (v != null) v.SetPicked(true);
        }

        /// <summary>验收截图用：点开场上第一个字的属性卡。</summary>
        public void ShowInfoForAnyPiece()
        {
            PieceView best = null;
            foreach (var v in _pieces.Values)
                if (v.Side == 0 && v.Piece.Type == PieceType.Unit && v.Target.y > CampY + 0.6f
                    && (best == null || v.Piece.Level > best.Piece.Level)) best = v;
            if (best == null)
                foreach (var v in _pieces.Values)
                    if (v.Side == 0) { best = v; break; }
            if (best != null) ShowInfo(best);
        }

        void HideCard()
        {
            _card.gameObject.SetActive(false);
            if (!_rangeFromDrag) HideRange();
            SetPicked(null);
            _cardT = 0;
        }

        void UpdateCard(float dt)
        {
            if (_cardT <= 0) return;
            _cardT -= dt;
            if (_cardT <= 0 || (Input.GetMouseButtonDown(0) && _cardT < 3.8f)) HideCard();
        }

        // ─────────────────────────── 闲置提示 ───────────────────────────

        /// <summary>原作玩家发呆时会画一条虚线：山寨里哪个字该放哪。</summary>
        void UpdateIdleHint(float dt)
        {
            if (!InputEnabled || _drag != null || M.Over || M.Hold || Paused) { _idleT = 0; SetHint(false); return; }
            _idleT += dt;
            if (_idleT < 5f) { SetHint(false); return; }
            _hintRecalc -= dt;
            if (_hintRecalc <= 0)
            {
                _hintRecalc = 1f;
                _hintOn = FindHint(out _hintFrom, out _hintTo);
            }
            SetHint(_hintOn);
            if (!_hintOn) return;

            var d = _hintTo - _hintFrom;
            float len = d.magnitude;
            const float gap = 0.3f;
            int n = Mathf.Min(40, Mathf.FloorToInt(len / gap));
            while (_hintDots.Count < n) _hintDots.Add(Gfx.Sprite(_root, Art.Circle, Color.white, Vector2.zero, Vector2.one * 0.1f, 70, "hintDot"));
            float off = Mathf.Repeat(Time.time * 0.9f, gap);
            for (int i = 0; i < _hintDots.Count; i++)
            {
                var dot = _hintDots[i];
                dot.enabled = i < n;
                if (i >= n) continue;
                float t = (i * gap + off) / len;
                dot.transform.localPosition = Vector2.Lerp(_hintFrom, _hintTo, t);
                dot.color = Pal.Ink.A(0.75f * Mathf.Sin(t * Mathf.PI));
            }
            if (_hintBox == null) _hintBox = Gfx.Sprite(_root, Art.Dashed, Pal.Ink, Vector2.zero, Vector2.one * 0.95f, 70, "hintBox");
            _hintBox.enabled = true;
            _hintBox.transform.localPosition = _hintTo;
            _hintBox.transform.localScale = Vector3.one * (0.95f + 0.06f * Mathf.Sin(Time.time * 6f));
            _hintBox.color = Pal.Ink.A(0.6f + 0.3f * Mathf.Sin(Time.time * 6f));
        }

        void SetHint(bool on)
        {
            if (on) return;
            foreach (var d in _hintDots) d.enabled = false;
            if (_hintBox != null) _hintBox.enabled = false;
        }

        bool FindHint(out Vector2 from, out Vector2 to)
        {
            from = to = default;
            var p = M.Player;
            var b = p.Board;
            for (int i = 0; i < p.Camp.Length; i++)
            {
                var q = p.Camp[i];
                if (q == null) continue;
                from = CampWorld(i);
                if (q.Type == PieceType.Shovel)
                {
                    int best = -1;
                    for (int c = 0; c < Board.Cols; c++)
                        for (int r = 0; r < Board.Rows; r++)
                        {
                            if (!b.Diggable(c, r)) continue;
                            int score = 0;
                            foreach (var n in b.Neighbors4(new CellPos(c, r))) if (b.Cells[n.c, n.r] == Cell.Path) score += 2;
                            foreach (var n in b.Neighbors4(new CellPos(c, r))) if (b.Placeable(n.c, n.r)) score++;
                            if (score > best) { best = score; to = CellWorld(0, c, r); }
                        }
                    if (best >= 0) return true;
                    continue;
                }
                for (int c = 0; c < Board.Cols; c++)
                    for (int r = 0; r < Board.Rows; r++)
                        if (Side.CanMerge(q, b.Pieces[c, r])) { to = CellWorld(0, c, r); return true; }
            }
            for (int i = 0; i < p.Camp.Length; i++)
            {
                var q = p.Camp[i];
                if (q == null || q.Type != PieceType.Unit) continue;
                for (int c = 0; c < Board.Cols; c++)
                    for (int r = 0; r < Board.Rows; r++)
                        if (b.Placeable(c, r) && b.Pieces[c, r] == null) { from = CampWorld(i); to = CellWorld(0, c, r); return true; }
            }
            return false;
        }

        // ─────────────────────────── 提示气泡 ───────────────────────────

        public void Tip(Vector2 pos, string s)
        {
            _tipText.text = s;
            int lines = 1;
            int maxLen = 0, cur = 0;
            foreach (var ch in s)
            {
                if (ch == '\n') { lines++; maxLen = Mathf.Max(maxLen, cur); cur = 0; }
                else cur++;
            }
            maxLen = Mathf.Max(maxLen, cur);
            var size = new Vector2(maxLen * 0.29f + 0.5f, lines * 0.38f + 0.3f);
            pos.x = Mathf.Clamp(pos.x, -3.94f + size.x * 0.5f, 3.94f - size.x * 0.5f);
            pos.y = Mathf.Min(pos.y, 6.4f - size.y * 0.5f);
            _tipBg.transform.localPosition = pos;
            _tipBg.size = size;
            _tipText.transform.localPosition = pos;
            _tipBg.enabled = true;
            _tipT = 2.8f;
            Tween.Pop(_tipBg.transform, 0.2f);
        }

        void UpdateTip(float dt)
        {
            if (_tipT <= 0) return;
            _tipT -= dt;
            if (_tipT <= 0 || (Input.GetMouseButtonDown(0) && _tipT < 2.6f))
            {
                _tipT = 0;
                _tipBg.enabled = false;
                _tipText.text = "";
            }
        }

        public static string ToCn(int n)
        {
            string[] d = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
            if (n >= 0 && n <= 10) return d[n];
            if (n < 20) return "十" + d[n - 10];
            if (n < 100) return d[n / 10] + "十" + (n % 10 == 0 ? "" : d[n % 10]);
            return n.ToString();
        }

        public Vector2 RecruitButtonPos => _recruitBtn.transform.localPosition;
        /// <summary>军师贴士的上沿：HUD 下面、对手半区顶上，换成屏幕像素。</summary>
        public Vector2 HintScreenTop => _cam.WorldToScreenPoint(new Vector3(0f, TopY - HudHalf - 0.1f, 0f));

        public bool TryFindEmptyCell(out Vector2 pos)
        {
            var b = M.Player.Board;
            for (int r = Board.Rows - 1; r >= 0; r--)
                for (int c = 0; c < Board.Cols; c++)
                    if (b.Placeable(c, r) && b.Pieces[c, r] == null) { pos = CellWorld(0, c, r); return true; }
            pos = default;
            return false;
        }

        // ─────────────────────────── 子视图 ───────────────────────────

        sealed class PieceView
        {
            public readonly Transform Root;
            public readonly Piece Piece;
            public readonly int Side;
            public Vector2 Target;
            public int Seen;
            public HeroInstance Hero;

            const float AnimDur = 0.34f;
            const float WeaponSize = 0.82f;
            readonly SpriteRenderer _tile, _shadow, _edge, _icon, _weapon;
            readonly TextMesh _glyph, _level;
            readonly Transform _glyphT;
            Color _glyphColor = Pal.Ink;
            float _animT;
            Vector2 _lungeOff;
            UnitKind _animKind;
            Vector2 _animDir;
            float _punch;
            int _lastLevel = -1;
            bool _dragging;
            readonly int _baseOrder = 20;

            public PieceView(Transform parent, Piece p, int side)
            {
                Piece = p;
                Side = side;
                Root = new GameObject("piece" + p.Id).transform;
                Root.SetParent(parent, false);
                _shadow = Gfx.Sprite(Root, Art.Rounded, Pal.Shadow, new Vector2(0.04f, -0.07f), new Vector2(0.92f, 0.92f), 20);
                _tile = Gfx.Sprite(Root, Art.Rounded, Pal.Tile, Vector2.zero, new Vector2(0.92f, 0.92f), 21);
                _edge = Gfx.Sprite(Root, Art.Frame, Pal.Ink.A(0.22f), Vector2.zero, new Vector2(0.92f, 0.92f), 21);
                _glyph = Gfx.Text(Root, p.Type == PieceType.Shovel ? "" : p.Glyph.ToString(), Fonts.Brush, 0.74f, Pal.Ink, new Vector2(0, -0.03f), 22);
                _glyphT = _glyph.transform;
                _level = Gfx.Text(Root, "", Fonts.Kai, 0.25f, Pal.InkSoft, new Vector2(0.31f, 0.31f), 23);
                if (p.Type == PieceType.Shovel)
                    _icon = Gfx.Sprite(Root, Art.Img("shovel"), Color.white, Vector2.zero, Vector2.one * 0.72f, 22);
                if (p.Type == PieceType.Unit)
                {
                    _weapon = Gfx.Sprite(Root, Art.Img(WeaponImg(p.Kind)), Color.white, Vector2.zero, Vector2.one * WeaponSize, 24);
                    _weapon.enabled = false;
                }
                Refresh();
            }

            static string WeaponImg(UnitKind k) => k switch
            {
                UnitKind.Dao => "dao",
                UnitKind.Qiang => "spear",
                UnitKind.Gong => "bow",
                _ => "horse",
            };

            void Refresh()
            {
                bool hero = Piece.Type == PieceType.HeroChar;
                bool formed = Hero != null;
                if (Piece.Type == PieceType.Shovel)
                {
                    _tile.color = Color.Lerp(Pal.Tile, Pal.Raft, 0.35f);
                    _edge.color = Pal.Willow.A(0.6f);
                    _level.text = "";
                    return;
                }
                _level.text = Piece.Level.ToString();
                if (hero)
                {
                    bool disha = formed && Hero.Def.Disha;
                    _tile.color = formed ? (disha ? Color.Lerp(Pal.Tile, Pal.Purple, 0.18f) : Pal.TileHero) : Pal.Tile;
                    _edge.color = (formed ? (disha ? Pal.Purple : Pal.Gold) : Pal.GoldBright).A(formed ? 0.9f : 0.7f);
                    _glyphColor = formed ? (disha ? Pal.Purple : Pal.RedDeep) : Pal.Gold;
                    _level.color = Pal.Gold;
                }
                else
                {
                    _tile.color = Color.Lerp(Pal.Tile, Pal.ForLevel(Piece.Level), Piece.Level > 1 ? 0.08f : 0f);
                    _edge.color = Pal.ForLevel(Piece.Level).A(Piece.Level > 1 ? 0.45f : 0.22f);
                    _glyphColor = Pal.ForLevel(Piece.Level);
                    _level.color = Pal.ForLevel(Piece.Level);
                }
                _glyph.color = _glyphColor;
            }

            float _popK = 1f;

            public void PopIn() => _popK = 0f;

            float _popDelay;

            /// <summary>聚义后整排错开弹出，空位和原有的字一起刷新。</summary>
            public void PopStagger(float delay)
            {
                _popDelay = Mathf.Max(0.02f, delay);
                _popK = 0f;
            }

            public void Punch(float amount) => _punch = Mathf.Max(_punch, amount);

            bool _pickedPiece;
            public void SetPicked(bool on)
            {
                if (_pickedPiece == on) return;
                _pickedPiece = on;
                if (!on)
                {
                    _edge.size = new Vector2(0.86f, 0.86f);
                    _lastLevel = -1;
                }
            }

            public void Attack(UnitKind kind, Vector2 dir)
            {
                _animT = 1f;
                _animKind = kind;
                _animDir = dir.sqrMagnitude > 0.001f ? dir : Vector2.up;
            }

            public void SetDragging(bool on)
            {
                _dragging = on;
                int o = on ? 60 : _baseOrder;
                _shadow.sortingOrder = o;
                _tile.sortingOrder = o + 1;
                _edge.sortingOrder = o + 1;
                _glyph.GetComponent<MeshRenderer>().sortingOrder = o + 2;
                if (_icon != null) _icon.sortingOrder = o + 2;
                _level.GetComponent<MeshRenderer>().sortingOrder = o + 3;
                if (_weapon != null) _weapon.sortingOrder = o + 4;
                Root.localScale = Vector3.one * (on ? 1.12f : 1f);
                _shadow.transform.localPosition = on ? new Vector2(0.1f, -0.18f) : new Vector2(0.04f, -0.07f);
            }

            public void Tick(float dt, bool dragging)
            {
                if (_lastLevel != Piece.Level * 10 + (Hero != null ? 1 : 0))
                {
                    _lastLevel = Piece.Level * 10 + (Hero != null ? 1 : 0);
                    Refresh();
                }
                if (_popDelay > 0f)
                {
                    _popDelay -= dt;
                    Root.localPosition = Target;
                    Root.localScale = Vector3.zero;
                    return;
                }
                var cur = (Vector2)Root.localPosition - _lungeOff;
                cur = Vector2.Lerp(cur, Target, 1f - Mathf.Exp(-dt * (dragging ? 30f : 16f)));
                _lungeOff = Vector2.zero;
                if (_animT > 0 && !dragging)
                {
                    float lk = 1f - _animT;
                    float reach = _animKind switch
                    {
                        UnitKind.Dao => 0.16f,
                        UnitKind.Qiang => 0.22f,
                        UnitKind.Gong => -0.09f,
                        _ => 0.12f,
                    };
                    _lungeOff = _animDir * reach * Mathf.Sin(Mathf.Min(1f, lk * 1.6f) * Mathf.PI);
                }
                Root.localPosition = cur + _lungeOff;

                _punch = Mathf.Max(0, _punch - dt * 1.5f);
                _popK = Mathf.Min(1f, _popK + dt / 0.3f);
                float pop = _popK < 1f ? Tween.OutBack(_popK) : 1f;
                Root.localScale = Vector3.one * ((_dragging ? 1.12f : 1f) * (1f + Mathf.Sin(_punch * 20f) * _punch * 0.5f) * pop);

                float sx = 1, sy = 1, morph = 0;
                if (_animT > 0 && _animKind == UnitKind.Qi)
                {
                    _animT = Mathf.Max(0, _animT - dt / AnimDur);
                    float q = Mathf.Sin((1f - _animT) * Mathf.PI);
                    sx += q * 0.18f;
                    sy -= q * 0.12f;
                    if (_weapon != null) _weapon.enabled = false;
                }
                else if (_animT > 0 && _weapon != null)
                {
                    _animT = Mathf.Max(0, _animT - dt / AnimDur);
                    float k = 1f - _animT;
                    morph = Mathf.Clamp01(Mathf.Min(k / 0.15f, _animT / 0.25f));
                    float ang = Mathf.Atan2(_animDir.y, _animDir.x) * Mathf.Rad2Deg;
                    Vector2 wp = Vector2.zero;
                    float wr = 0, wsx = 1;
                    bool flip = false;
                    switch (_animKind)
                    {
                        case UnitKind.Dao:
                            wr = ang - 45f + Mathf.Lerp(80f, -80f, Tween.OutCubic(k));
                            wp = _animDir * 0.12f;
                            break;
                        case UnitKind.Qiang:
                            wr = ang - 45f;
                            wp = _animDir * Mathf.Sin(k * Mathf.PI) * 0.55f;
                            break;
                        case UnitKind.Gong:
                            wr = ang - 180f;
                            wsx = 1f - 0.28f * Mathf.Sin(Mathf.Min(1f, k * 1.6f) * Mathf.PI);
                            break;
                        default:
                            flip = _animDir.x > 0;
                            wp = _animDir * Mathf.Sin(k * Mathf.PI) * 0.35f + Vector2.up * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f)) * 0.12f;
                            wr = Mathf.Sin(k * Mathf.PI * 2f) * 10f;
                            break;
                    }
                    _weapon.enabled = morph > 0.01f;
                    var wt = _weapon.transform;
                    wt.localPosition = wp;
                    wt.localRotation = Quaternion.Euler(0, 0, wr);
                    float ws = WeaponSize * (0.6f + 0.4f * morph);
                    wt.localScale = new Vector3(ws * wsx * (flip ? -1f : 1f), ws, 1);
                    _weapon.color = Color.white.A(morph);
                }
                else if (_weapon != null) _weapon.enabled = false;

                if (Piece.Type == PieceType.HeroChar && Hero == null && !_dragging)
                {
                    float breathe = Mathf.Sin(Time.time * 2f + Piece.Id) * 0.04f;
                    sx += breathe;
                    sy += breathe;
                }
                float g = 1f - morph * 0.85f;
                _glyphT.localPosition = new Vector2(0, -0.02f);
                _glyphT.localScale = new Vector3(sx * g, sy * g, 1);
                _glyph.color = _glyphColor.A(1f - morph * 0.9f);
                if (_pickedPiece)
                {
                    _edge.color = Pal.GoldBright;
                    _edge.size = new Vector2(1.02f, 1.02f);
                }
            }

            public void FadeOutAndDestroy()
            {
                var root = Root;
                Tween.Run(root, 0.18f, k => { if (root) root.localScale = Vector3.one * (1 - k); }, () => { if (root) UnityEngine.Object.Destroy(root.gameObject); });
            }
        }

        sealed class FrameView
        {
            readonly SpriteRenderer _frame, _glow, _xpBg, _xpFg, _armBg;
            readonly TextMesh _nick, _seal, _arm;
            readonly SpriteRenderer _sealBg;
            readonly Transform _root;
            readonly Color _tint, _tintBright;
            public int Seen;
            float _pulse, _glowT;
            float _barW;

            public FrameView(BattleView v, HeroInstance h, int side)
            {
                _root = new GameObject("hero" + h.Id).transform;
                _root.SetParent(v._root, false);
                bool disha = h.Def.Disha;
                _tint = disha ? Pal.Purple : Pal.Gold;
                _tintBright = disha ? Pal.Hex("A57BD1") : Pal.GoldBright;
                _glow = Gfx.Sprite(_root, Art.Frame, _tintBright.A(0f), Vector2.zero, Vector2.one, 17);
                _frame = Gfx.Sprite(_root, Art.Frame, _tintBright, Vector2.zero, Vector2.one, 24);
                _nick = Gfx.Text(_root, h.Def.Nick, Fonts.Kai, 0.2f, disha ? Pal.Purple : Pal.RedDeep, Vector2.zero, 25);
                _sealBg = Gfx.Sprite(_root, Art.Rounded, disha ? Pal.Purple : Pal.Red, Vector2.zero, new Vector2(0.26f, 0.26f), 25);
                _seal = Gfx.Text(_root, disha ? "煞" : "罡", Fonts.Brush, 0.2f, Pal.Paper, Vector2.zero, 26);
                _xpBg = Gfx.Sprite(_root, Art.White, Pal.Ink.A(0.45f), Vector2.zero, new Vector2(1, 0.06f), 25);
                _xpFg = Gfx.Sprite(_root, Art.White, _tintBright, Vector2.zero, new Vector2(1, 0.06f), 26);
                bool armed = side == 0 && !string.IsNullOrEmpty(h.Def.Weapon)
                    && v.M.Player.Items.Armed != null && v.M.Player.Items.Armed.Contains((int)h.Def.Id);
                _armBg = null;
                _arm = null;
                if (armed)
                {
                    _armBg = Gfx.Sprite(_root, Art.Rounded, Ui.GradeColor(h.Def.WGrade), Vector2.zero, new Vector2(0.72f, 0.32f), 27);
                    _arm = Gfx.Text(_root, "用" + h.Def.WeaponTag, Fonts.Kai, 0.2f, Pal.Paper, Vector2.zero, 28);
                }
                _side = side;
                Place(h);
                Tween.Pop(_root, 0.4f);
                _glowT = 1.5f;
            }

            readonly int _side;

            void Place(HeroInstance h)
            {
                Vector2 min = new Vector2(99, 99), max = new Vector2(-99, -99);
                foreach (var c in h.Cells)
                {
                    var p = CellWorld(_side, c.c, c.r);
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
                var center = (min + max) * 0.5f;
                var size = max - min + new Vector2(1.02f, 1.02f);
                _root.localPosition = center;
                _frame.size = size;
                _glow.size = size + new Vector2(0.3f, 0.3f);
                _nick.transform.localPosition = new Vector2(0, -size.y * 0.5f + 0.02f);
                _sealBg.transform.localPosition = new Vector2(size.x * 0.5f - 0.1f, size.y * 0.5f - 0.1f);
                _seal.transform.localPosition = _sealBg.transform.localPosition;
                _barW = size.x - 0.5f;
                _xpBg.transform.localPosition = new Vector2(-0.12f, size.y * 0.5f - 0.03f);
                _xpBg.transform.localScale = new Vector3(_barW, 0.06f, 1);
                if (_arm != null)
                {
                    var p = new Vector2(-size.x * 0.5f + 0.42f, size.y * 0.5f - 0.22f);
                    _armBg.transform.localPosition = p;
                    _arm.transform.localPosition = p;
                }
            }

            public void Update(HeroInstance h)
            {
                Place(h);
                float dt = Time.deltaTime;
                _pulse = Mathf.Max(0, _pulse - dt * 4f);
                _glowT = Mathf.Max(0, _glowT - dt);
                float a = Mathf.Max(_pulse * 0.5f, Mathf.Min(1, _glowT) * 0.8f, 0.15f + 0.1f * Mathf.Sin(Time.time * 3f));
                _glow.color = _tintBright.A(a);
                _frame.color = Color.Lerp(_tint, _tintBright, 0.5f + 0.5f * Mathf.Sin(Time.time * 3f));
                float k = h.Level >= UnitDef.MaxLevel ? 1f : Mathf.Clamp01(h.Xp / HeroInstance.XpNeed(h.Level));
                _xpFg.transform.localScale = new Vector3(_barW * k, 0.06f, 1);
                _xpFg.transform.localPosition = _xpBg.transform.localPosition - new Vector3(_barW * (1 - k) * 0.5f, 0, 0);
            }

            public void Pulse() => _pulse = 1f;
            public void Glow() => _glowT = 1f;

            public void Destroy() => UnityEngine.Object.Destroy(_root.gameObject);
        }

        sealed class EnemyView
        {
            public readonly Transform Root;
            public int Seen;
            readonly TextMesh _glyph, _cc;
            readonly SpriteRenderer _hpBg, _hpFg, _aura, _shadow;
            readonly float _barW;
            readonly Color _baseColor;
            float _flash;
            readonly bool _big;
            readonly Vector2 _jitter;

            public EnemyView(Transform parent, Enemy e, int side)
            {
                Root = new GameObject("enemy" + e.Id).transform;
                Root.SetParent(parent, false);
                bool boss = e.Boss != null;
                bool elite = e.Def.Kind == EnemyKind.Bu;
                float h = boss ? 0.55f : elite ? 0.62f : e.Def.Kind == EnemyKind.Jia ? 0.5f : 0.42f;
                GlyphHeight = h;
                _big = boss || elite;
                uint hash = (uint)e.Id * 2654435761u;
                _jitter = new Vector2(((hash >> 8) & 0xff) / 255f - 0.5f, ((hash >> 16) & 0xff) / 255f - 0.5f) * (_big ? 0f : 0.28f);
                _baseColor = boss ? Pal.RedDeep : elite ? Pal.Hex("3B2050") : Pal.Ink;
                _shadow = Gfx.Sprite(Root, Art.SoftDot, Pal.Shadow, new Vector2(0, -h * 0.45f), new Vector2(h * 1.1f, h * 0.35f), 29);
                if (boss || elite)
                    _aura = Gfx.Sprite(Root, Art.Blob, (boss ? Pal.Red : Pal.Hex("3B2050")).A(0.35f), Vector2.zero, Vector2.one * (boss ? 1.6f : 0.95f), 30);
                if (e.Def.Kind == EnemyKind.Jia)
                    _aura = Gfx.Sprite(Root, Art.Circle, Pal.InkSoft.A(0.25f), Vector2.zero, Vector2.one * 0.6f, 30);
                _glyph = Gfx.Text(Root, e.Glyph, Fonts.Brush, h, _baseColor, Vector2.zero, 32);
                _cc = Gfx.Text(Root, "", Fonts.Kai, 0.28f, Pal.Hex("5B7FA6"), new Vector2(0.42f, 0.12f), 35);
                _barW = boss ? 1.3f : elite ? 0.8f : 0.45f;
                float y = boss ? 0.55f : 0.4f;
                _hpBg = Gfx.Sprite(Root, Art.White, Pal.Ink.A(0.55f), new Vector2(0, y), new Vector2(_barW + 0.04f, 0.09f), 33);
                _hpFg = Gfx.Sprite(Root, Art.White, Pal.Hp, new Vector2(0, y), new Vector2(_barW, 0.06f), 34);
                Root.gameObject.SetActive(false);
            }

            bool _out;

            public void Flash() => _flash = 1f;

            public string GlyphText => _glyph.text;
            public float GlyphHeight { get; }
            public Color GlyphColor => _baseColor;

            Vector2 _kick;

            /// <summary>受击后仰：沿攻击方向被推开一下再弹回。</summary>
            public void Kick(Vector2 dir, float amount)
            {
                if (_big) amount *= 0.35f;
                _kick += dir * amount;
                if (_kick.sqrMagnitude > 0.16f) _kick = _kick.normalized * 0.4f;
                _flash = 1f;
            }

            public void Sync(Enemy e, Vector2 pos, float dt)
            {
                Root.localPosition = pos + _jitter + _kick + new Vector2(0, Mathf.Abs(Mathf.Sin(Time.time * 9f + e.Id)) * 0.04f);
                if (e.InBrush)
                {
                    _out = false;
                    if (Root.gameObject.activeSelf) Root.gameObject.SetActive(false);
                    return;
                }
                if (!_out)
                {
                    _out = true;
                    Root.gameObject.SetActive(true);
                    Root.localScale = Vector3.one * 0.2f;
                    Tween.Run(Root, 0.2f, k => { if (Root) Root.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, k); });
                }
                _kick = Vector2.Lerp(_kick, Vector2.zero, 1f - Mathf.Exp(-dt * 14f));
                float k = Mathf.Clamp01(e.Hp / e.MaxHp);
                bool showBar = _big || k < 0.999f;
                _hpBg.enabled = _hpFg.enabled = showBar;
                _hpFg.transform.localScale = new Vector3(_barW * k, 0.06f, 1);
                _hpFg.transform.localPosition = new Vector2(-_barW * (1 - k) * 0.5f, _hpFg.transform.localPosition.y);
                _flash = Mathf.Max(0, _flash - dt * 8f);
                bool haste = e.HasteT > 0 && e.StunT <= 0;
                var c = e.StunT > 0 ? Color.Lerp(_baseColor, Pal.Hex("5B7FA6"), 0.7f)
                    : haste ? Color.Lerp(_baseColor, Pal.Red, 0.62f) : _baseColor;
                _cc.text = e.StunT > 0 ? "眩" : haste ? "速" : "";
                _cc.color = e.StunT > 0 ? Pal.Hex("5B7FA6") : Pal.Red;
                _glyph.color = Color.Lerp(c, Pal.Red, _flash * 0.8f);
                float squash = _flash * 0.22f;
                _glyph.transform.localScale = new Vector3(1f + squash, 1f - squash * 0.6f, 1f) * (haste ? 1.14f : 1f);
                _glyph.transform.localRotation = Quaternion.Euler(0, 0, -_kick.x * 40f);
            }
        }

        sealed class GoalView
        {
            readonly BattleView _v;
            readonly int _side;
            readonly Transform _root;
            readonly List<SpriteRenderer> _hearts = new List<SpriteRenderer>();
            readonly TextMesh _danger, _glyph;
            readonly SpriteRenderer _bubble, _shield;
            readonly TextMesh _bubbleText;
            readonly SpriteRenderer _bg;
            readonly float _heartY, _inward;
            float _hurt;
            float _bubbleT;
            int _lastHearts;

            public GoalView(BattleView v, int side)
            {
                _v = v;
                _side = side;
                var s = v.M.Sides[side];
                var b = s.Board;
                var pos = CellWorld(side, b.Goal.c, b.Goal.r);
                _root = new GameObject("goal" + side).transform;
                _root.SetParent(v._root, false);
                _root.localPosition = pos;
                _bg = Gfx.Sprite(_root, Art.White, Pal.Tile, Vector2.zero, Vector2.one, 24);
                Gfx.Sprite(_root, Art.White, Pal.RedDeep, new Vector2(0f, side == 0 ? -0.46f : 0.46f), new Vector2(1f, 0.08f), 25);
                _glyph = Gfx.Text(_root, "宋", Fonts.Brush, 0.55f, Pal.RedDeep, new Vector2(0, -0.06f), 27);
                Gfx.Sprite(_root, Art.Img("hat"), Color.white, new Vector2(0, 0.16f), Vector2.one * 0.62f, 28);
                _shield = Gfx.Sprite(_root, Art.Swirl, Pal.Red.A(0.75f), Vector2.zero, Vector2.one * 1.9f, 29);
                _shield.enabled = false;
                // 心留在这一格里，贴着格子上沿，不再飘到旁边的草地上。
                _heartY = side == 0 ? 0.28f : -0.28f;
                _lastHearts = s.Hearts;
                _inward = pos.x > 0f ? -1f : 1f;
                _danger = Gfx.Text(_root, "危", Fonts.Brush, 0.6f, Pal.Red, new Vector2(_inward, 0), 30);
                _danger.gameObject.SetActive(false);
                float by = side == 0 ? 1.45f : -1.45f;
                _bubble = Gfx.Sprite(_root, Art.Rounded, Color.white, new Vector2(_inward * 1.2f, by), new Vector2(2.2f, 0.55f), 70);
                _bubbleText = Gfx.Text(_root, "", Fonts.Kai, 0.26f, Pal.Ink, new Vector2(_inward * 1.2f, by), 71);
                _bubble.enabled = false;
            }

            public void Hurt()
            {
                _hurt = 1f;
                var s = _v.M.Sides[_side];
                if (s.Hearts < _lastHearts)
                {
                    _lastHearts = s.Hearts;
                    Say(s.Hearts == 1 ? "兄弟们救我！" : "哎哟——", 2f);
                }
            }

            public void Say(string text, float dur)
            {
                _bubbleText.text = text;
                _bubble.size = new Vector2(text.Length * 0.28f + 0.5f, 0.55f);
                float half = _bubble.size.x * 0.5f, x0 = _root.localPosition.x;
                float x = Mathf.Clamp(x0 + _inward * 1.2f, -3.94f + half, 3.94f - half) - x0;
                var bt = _bubble.transform;
                bt.localPosition = new Vector2(x, bt.localPosition.y);
                _bubbleText.transform.localPosition = new Vector2(x, bt.localPosition.y);
                _bubble.enabled = true;
                _bubbleT = dur;
            }

            public void Sync()
            {
                var s = _v.M.Sides[_side];
                float dt = Time.deltaTime;
                int n = s.MaxHearts;
                float gap = n <= 4 ? 0.4f : 0.22f;
                float size = n <= 4 ? 0.36f : 0.22f;
                while (_hearts.Count < n)
                    _hearts.Add(Gfx.Sprite(_root, Art.Heart, Pal.Red, Vector2.zero, Vector2.one * size, 40));
                float rowHalf = (n - 1) * gap * 0.5f + size * 0.5f, x0 = _root.localPosition.x;
                float rowX = Mathf.Clamp(x0, -3.96f + rowHalf, 3.96f - rowHalf) - x0;
                for (int i = 0; i < _hearts.Count; i++)
                {
                    var heart = _hearts[i];
                    heart.enabled = i < n;
                    heart.transform.localPosition = new Vector2(rowX + (i - (n - 1) * 0.5f) * gap, _heartY);
                    float fill = Mathf.Clamp01((s.Hp - i * Tuning.HeartHp) / (float)Tuning.HeartHp);
                    heart.color = fill <= 0 ? Pal.Ink.A(0.28f) : Color.Lerp(Pal.Hex("E8A0A0"), Pal.Red, fill);
                    heart.transform.localScale = Vector3.one * size * (0.85f + 0.15f * fill) * (1f + _hurt * 0.25f);
                }
                if (s.Hearts > _lastHearts) _lastHearts = s.Hearts;
                _hurt = Mathf.Max(0, _hurt - dt * 3f);
                _bg.color = Color.Lerp(Pal.Tile, Pal.Hex("F2B8A8"), _hurt);
                _glyph.transform.localPosition = new Vector2(Mathf.Sin(Time.time * 60f) * 0.06f * _hurt, -0.1f);

                _shield.enabled = s.ShieldT > 0;
                if (_shield.enabled)
                {
                    _shield.transform.localRotation = Quaternion.Euler(0, 0, -Time.time * 240f);
                    _shield.color = Pal.Red.A(Mathf.Min(0.75f, s.ShieldT * 0.4f));
                }

                bool danger = false;
                var goal = s.Board.Goal.Center;
                foreach (var e in s.Enemies)
                    if (!e.Dead && V2.Dist(e.Pos, goal) < 1.8f) { danger = true; break; }
                _danger.gameObject.SetActive(danger);
                if (danger) _danger.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(Time.time * 12f));

                if (_bubbleT > 0)
                {
                    _bubbleT -= dt;
                    if (_bubbleT <= 0) { _bubble.enabled = false; _bubbleText.text = ""; }
                }
            }
        }
    }
}
