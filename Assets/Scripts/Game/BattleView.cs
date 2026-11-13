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
        /// <summary>9:16 时自己棋盘的底边。更高的屏幕整块下移，见 LowerShift。</summary>
        public const float MyBottom0 = -4.25f;
        /// <summary>比 9:16 多出来的高度。下半场（自己的棋盘、山寨、聚义）整体下移这么多，底边留白和 9:16 一样。</summary>
        public static float LowerShift { get; private set; }
        public static float MyBottom => MyBottom0 - LowerShift;
        public const float OppBottom = 1.13f;
        public static float CampY => MyBottom - 0.47f;
        public static float ButtonY => CampY - 1.22f;
        /// <summary>被动道具一行，排在聚义下面。主动道具和聚义同一行，靠左。</summary>
        public static float ItemY => ButtonY - 1.06f;
        public const float TopY = 6.39f;
        static float WallY => (MyBottom + Board.Rows + OppBottom) * 0.5f;
        const float HudHalf = 0.40f;
        const float ItemGap = 1.0f;
        float _activeScale = 0.96f;
        float _activeGap = 1.15f;
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
        Transform _root, _fxRoot, _low, _frameRoot;
        float _layoutShift;
        SpriteRenderer _midInk, _midPaper;
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
        readonly Stack<SpriteRenderer> _spriteFxPool = new Stack<SpriteRenderer>();
        const int MaxPooledSprites = 180;
        int _minorDmgSkip;
        bool LowFxLoad => M != null && (M.Wave >= 12 || _fx.Count >= 85);
        bool HeavyFxLoad => M != null && (M.Wave >= 16 || _fx.Count >= 125);
        readonly SpriteRenderer[] _campSlots = new SpriteRenderer[Tuning.CampSlots];

        SpriteRenderer _recruitBtn, _recruitFill, _pauseBtn, _mengBtn, _shuBtn, _baoziBtn, _fuBtn, _trapBtn, _penBtn, _rescueBtn;
        Transform _recruitRim, _recruitBun;
        bool _recruitDown, _pauseDown, _shovelDown, _rescueDown;
        int _passiveDown = -1;
        Vector3 _pauseRest = Vector3.one, _shovelRest = Vector3.one;
        readonly CooldownRing[] _itemCd = new CooldownRing[6];
        readonly TextMesh[] _itemCdText = new TextMesh[6];
        readonly bool[] _itemCooling = new bool[6];
        readonly float[] _itemFlash = new bool[6].Length > 0 ? new float[6] : null;
        readonly List<SpriteRenderer> _passiveBtns = new List<SpriteRenderer>();
        readonly List<ShopItem> _passiveItems = new List<ShopItem>();
        TextMesh _recruitCost, _recruitLabel, _bunsText, _waveText, _midText, _mengText, _shuText, _baoziText, _fuText, _trapText, _penText, _titleText;
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
        int _cardFrame;
        string _cardKey;
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
        /// 真机上标题让开微信胶囊。比 9:16 更高时，多出来的高度补在两军之间。
        /// 底下先让开指示条，再留一截给聚义下面那排被动，名字才不会被切掉。
        /// </summary>
        public static void FitCamera(Camera cam)
        {
            var rect = cam.rect;
            float vw = Mathf.Max(1f, rect.width * Screen.width), vh = Mathf.Max(1f, rect.height * Screen.height);
            float bar = 0f, menuTop = 0f, menuBottom = 0f, bottomPx = 0f;
            if (rect.width >= 0.999f && rect.height >= 0.999f)
                Ui.ScreenInsets(out bar, out menuTop, out menuBottom, out _, out bottomPx);
            float upp = WidthNeed / vw;
            float topWorld = menuBottom > 0f
                ? TopY + (menuTop + menuBottom) * 0.5f * upp
                : TopY + HudHalf + 0.02f + bar * upp;
            cam.orthographicSize = vh * upp * 0.5f;
            CamBase = new Vector3(0f, topWorld - cam.orthographicSize, -10f);
            cam.transform.position = CamBase;

            float viewH = cam.orthographicSize * 2f;
            float designH = WidthNeed * (Ui.DesignH / Ui.DesignW);
            float extra = Mathf.Max(0f, viewH - designH);
            // 模拟器经常不报底部安全区。比 9:16 更高就按指示条兜一截，报得过大则封顶。
            bool tall = vh > vw * (Ui.DesignH / Ui.DesignW) + 8f;
            float reported = Mathf.Clamp(bottomPx, 0f, vh * 0.08f);
            if (tall && reported < vh * 0.04f) reported = vh * 0.04f;
            // 被动图标在聚义正下方，名字条还往下探。这一截从两军之间匀出来。
            float passive = tall ? vh * 0.065f : 0f;
            float inset = Mathf.Min(reported + passive, vh * 0.14f) * upp;
            LowerShift = Mathf.Max(0f, extra - inset);
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
            _low = new GameObject("Low").transform;
            _low.SetParent(_root, false);
            _layoutShift = LowerShift;
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
            BuildActives();
            BuildBubble();
            for (int s = 0; s < 2; s++) _goals[s] = new GoalView(this, s);
            BuildAmbient();
            SyncAll(true);
        }

        /// <summary>屏幕变高后，把已经摆好的下半场平移到新位置。父节点停在原点，本地坐标仍是世界坐标。</summary>
        public void CatchLayout()
        {
            if (_low == null) return;
            float dy = -(LowerShift - _layoutShift);
            if (Mathf.Abs(dy) < 0.001f) return;
            _layoutShift = LowerShift;
            for (int i = 0; i < _low.childCount; i++)
            {
                var c = _low.GetChild(i);
                var p = c.localPosition;
                p.y += dy;
                c.localPosition = p;
            }
            foreach (var v in _pieces.Values)
                if (v.Side == 0) v.Target.y += dy;
            if (_card != null && _card.gameObject.activeSelf)
            {
                var p = _card.localPosition;
                p.y += dy;
                _card.localPosition = p;
            }
            if (_bubble != null && _bubble.gameObject.activeSelf)
            {
                var p = _bubble.localPosition;
                p.y += dy;
                _bubble.localPosition = p;
            }
            BuildFrame();
            PlaceMid();
        }

        /// <summary>原作是一整张棋盘：上半对手、下半自己，中间一道城墙。更高的屏幕把多出来的空补成草地。</summary>
        void BuildFrame()
        {
            if (_frameRoot != null) Destroy(_frameRoot.gameObject);
            _frameRoot = new GameObject("frame").transform;
            _frameRoot.SetParent(_root, false);

            float gapBottom = MyBottom + Board.Rows;
            float gapTop = OppBottom;
            float gapH = Mathf.Max(0.05f, gapTop - gapBottom);
            float mid = (gapBottom + gapTop) * 0.5f;
            if (gapH > 0.5f)
            {
                int row = 0;
                for (float y = gapBottom + 0.5f; y < gapTop + 0.51f; y += 1f, row++)
                    for (int c = -2; c < Board.Cols + 2; c++)
                        Gfx.Sprite(_frameRoot, Art.Ground("tex_grass", c, row), Color.white, new Vector2(Left + c + 0.5f, y), Vector2.one * 1.02f, 7, "gap");
            }

            float stoneH = Mathf.Min(0.38f, gapH);
            var wall = Gfx.Sprite(_frameRoot, Art.Img("tex_stone"), Color.white, new Vector2(0, mid), Vector2.one, 9, "wall");
            wall.drawMode = SpriteDrawMode.Tiled;
            wall.transform.localScale = Vector3.one;
            wall.size = new Vector2(Board.Cols, stoneH);
            Gfx.Sprite(_frameRoot, Art.White, Pal.Ink.A(0.75f), new Vector2(0, mid + stoneH * 0.5f), new Vector2(Board.Cols, 0.05f), 10);
            Gfx.Sprite(_frameRoot, Art.White, Pal.Ink.A(0.75f), new Vector2(0, mid - stoneH * 0.5f), new Vector2(Board.Cols, 0.05f), 10);
            for (float x = Left + 0.25f; x < -Left; x += 0.5f)
            {
                Gfx.Sprite(_frameRoot, Art.White, Pal.InkSoft.A(0.85f), new Vector2(x, mid + stoneH * 0.5f + 0.06f), new Vector2(0.26f, 0.1f), 10);
                Gfx.Sprite(_frameRoot, Art.White, Pal.InkSoft.A(0.85f), new Vector2(x, mid - stoneH * 0.5f - 0.06f), new Vector2(0.26f, 0.1f), 10);
            }
            DecorateWall(mid, stoneH);
        }

        void PlaceMid()
        {
            if (_midText == null) return;
            float y = WallY;
            if (_midInk != null) _midInk.transform.localPosition = new Vector2(0f, y);
            if (_midPaper != null) _midPaper.transform.localPosition = new Vector2(0f, y);
            _midText.transform.localPosition = new Vector2(0f, y);
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
                        _hl[c, r] = Gfx.Sprite(_low, Art.Rounded, Color.clear, pos, new Vector2(0.95f, 0.95f), 14, "hl");
                        _hlMark[c, r] = Gfx.Text(_low, "", Fonts.Kai, 0.26f, Pal.RedDeep, pos + new Vector2(0, -0.33f), 15);
                    }
                }

            var sp = CellWorld(side, b.Spawn.c, b.Spawn.r);
            BuildSpawn(side == 0 ? _low : _root, sp);
        }

        void BuildCell(int side, int c, int r)
        {
            var b = M.Sides[side].Board;
            var kind = b.Cells[c, r];
            _cellKind[side][c, r] = kind;
            if (_cellVis[side][c, r] != null) Destroy(_cellVis[side][c, r].gameObject);
            var t = new GameObject("cell").transform;
            t.SetParent(side == 0 ? _low : _root, false);
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
                    // 开好的地铺成石板院子，相邻几格连成一片，不再一格一块白方砖。
                    Gfx.Sprite(t, Art.Ground("tex_stone", c, r), Pal.Hex("F4E9D2"), Vector2.zero, full, 8);
                    PaveEdges(t, side, b, c, r);
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

        /// <summary>石板院子只在挨着草地、土路的那一边描一道边，院子里面不画格线。</summary>
        void PaveEdges(Transform t, int side, Board b, int c, int r)
        {
            var self = CellWorld(side, c, r);
            foreach (var d in new[] { new CellPos(1, 0), new CellPos(-1, 0), new CellPos(0, 1), new CellPos(0, -1) })
            {
                int nc = c + d.c, nr = r + d.r;
                if (Board.InBounds(nc, nr) && b.Cells[nc, nr] == Cell.Tile) continue;
                var delta = (CellWorld(side, nc, nr) - self) * 0.5f;
                var edge = Gfx.Sprite(t, Art.Stroke, Pal.PathEdge.A(0.55f), delta * 0.96f, new Vector2(1.0f, 0.06f), 9);
                edge.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Abs(delta.x) > 0.01f ? 90 : 0);
            }
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
            Gfx.Sprite(_low, Art.Rounded, Pal.Ink.A(0.85f), new Vector2(-0.52f, CampY), new Vector2(6.88f, 1.22f), 1);
            Gfx.Sprite(_low, Art.Rounded, Pal.PaperDark, new Vector2(-0.52f, CampY), new Vector2(6.76f, 1.1f), 2);
            Gfx.Text(_low, "山寨", Fonts.Brush, 0.4f, Pal.RedDeep, new Vector2(-3.43f, CampY), 5);
            for (int i = 0; i < _campSlots.Length; i++)
            {
                _campSlots[i] = Gfx.Sprite(_low, Art.Rounded, Pal.Paper.A(0.9f), CampWorld(i), new Vector2(0.98f, 0.98f), 4, "slot");
                _campPlus[i] = Gfx.Text(_low, "", Fonts.Kai, 0.48f, Pal.InkSoft, CampWorld(i), 6);
            }

            _rescueRoot = new GameObject("rescue").transform;
            _rescueRoot.SetParent(_low, false);
            _rescueRoot.localPosition = new Vector2(ShovelOfferX, CampY);
            Gfx.Sprite(_rescueRoot, Art.Rounded, Pal.Shadow, new Vector2(0.03f, -0.07f), new Vector2(0.84f, 1.08f), 49);
            _rescueBtn = Gfx.Sprite(_rescueRoot, Art.Rounded, Pal.GoldBright, Vector2.zero, new Vector2(0.84f, 1.08f), 50);
            Gfx.Sprite(_rescueRoot, Art.Frame, Pal.Gold, Vector2.zero, new Vector2(0.84f, 1.08f), 51);
            Gfx.Text(_rescueRoot, "救", Fonts.Brush, 0.5f, Pal.RedDeep, new Vector2(0, 0.12f), 52);
            if (FreeRescue)
            {
                Gfx.Sprite(_rescueRoot, Art.Rounded, Pal.Willow, new Vector2(0, -0.34f), new Vector2(0.78f, 0.28f), 52);
                Gfx.Text(_rescueRoot, "免费", Fonts.Kai, 0.18f, Pal.Paper, new Vector2(0, -0.34f), 53);
            }
            else
            {
                Gfx.Sprite(_rescueRoot, Art.Rounded, Pal.Red, new Vector2(0, -0.34f), new Vector2(0.78f, 0.28f), 52);
                Gfx.Sprite(_rescueRoot, Art.Img("ui_video"), Color.white, new Vector2(-0.2f, -0.34f), Vector2.one * 0.26f, 53);
                Gfx.Text(_rescueRoot, "广告", Fonts.Kai, 0.17f, Pal.Paper, new Vector2(0.12f, -0.34f), 53);
            }
            _rescueRoot.gameObject.SetActive(false);

            _shovelRoot = new GameObject("shovelOffer").transform;
            _shovelRoot.SetParent(_low, false);
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
            Gfx.Sprite(_low, Art.Rounded, Pal.Shadow, new Vector2(0.2f, ButtonY - 0.09f), new Vector2(RecruitW, RecruitH), 48);
            _recruitRim = Gfx.Sprite(_low, Art.Rounded, Pal.Ink, new Vector2(0.2f, ButtonY), new Vector2(RecruitW + 0.16f, RecruitH + 0.16f), 49, "recruitRim").transform;
            _recruitBtn = Gfx.Sprite(_low, Art.Rounded, Pal.Hex("3A342C"), new Vector2(0.2f, ButtonY), new Vector2(RecruitW, RecruitH), 50, "recruit");
            _recruitFill = Gfx.Sprite(_low, Art.Progress, Pal.Hex("C4894A"), new Vector2(0.2f, ButtonY), new Vector2(RecruitW, RecruitH), 51, "recruitFill");
            _recruitFill.enabled = false;
            _recruitLabel = Gfx.Text(_low, "聚  义", Fonts.Brush, 0.5f, Pal.Paper, new Vector2(0.2f, ButtonY + 0.14f), 52);
            _recruitCost = Gfx.Text(_low, "", Fonts.Kai, 0.26f, Pal.Paper, new Vector2(0.32f, ButtonY - 0.27f), 52);
            _recruitBun = Gfx.Sprite(_low, Art.Img("bun"), Color.white, new Vector2(-0.06f, ButtonY - 0.27f), Vector2.one * 0.26f, 52).transform;

            _mengBtn = ItemButton(0, ItemSlot(0), "it_menghan", "蒙汗药", Pal.Hex("C9A6EE"), out _mengText);
            _baoziBtn = ItemButton(1, ItemSlot(1), "bun", "包子", Pal.Hex("A4D8F2"), out _baoziText);
            _shuBtn = ItemButton(2, ItemSlot(2), "it_tianshu", "天书", Pal.Hex("F6C46B"), out _shuText);
            _fuBtn = ItemButton(3, ItemSlot(3), "it_fu", "攻速符", Pal.Hex("C9A6EE"), out _fuText);
            _trapBtn = ItemButton(4, ItemSlot(3), "it_trap", "陷阱", Pal.Hex("C5E08A"), out _trapText);
            _penBtn = ItemButton(5, ItemSlot(3), "wp_fan", "代笔", Pal.Hex("C9A6EE"), out _penText);
            BuildPassives();

            _pauseBtn = Gfx.Sprite(_root, Art.Img("ui_pause"), Color.white, new Vector2(-3.5f, TopY), new Vector2(0.8f, 0.8f), 50, "pause");

            Gfx.Sprite(_root, Art.Rounded, Pal.Ink.A(0.85f), new Vector2(-2.1f, TopY), new Vector2(1.7f, 0.64f), 49);
            _bunIcon = Gfx.Sprite(_root, Art.Img("bun"), Color.white, new Vector2(-2.75f, TopY), Vector2.one * 0.54f, 51, "bun").transform;
            _bunsText = Gfx.Text(_root, "0", Fonts.Kai, 0.4f, Pal.Paper, new Vector2(-1.9f, TopY), 52);

            // 右上角留给微信胶囊，对手名并进波次那一行。
            _titleText = Gfx.Text(_root, M.Chapter.Name, Fonts.Brush, 0.48f, Pal.Ink, new Vector2(0.15f, TopY + 0.10f), 52);
            _waveText = Gfx.Text(_root, "", Fonts.Kai, 0.27f, Pal.InkSoft, new Vector2(0.15f, TopY - 0.18f), 52);

            _midInk = Gfx.Sprite(_root, Art.Rounded, Pal.Ink.A(0.85f), new Vector2(0, WallY), new Vector2(3.3f, 0.34f), 11);
            _midPaper = Gfx.Sprite(_root, Art.Rounded, Pal.Paper, new Vector2(0, WallY), new Vector2(3.2f, 0.28f), 12);
            _midText = Gfx.Text(_root, "", Fonts.Kai, 0.26f, Pal.Ink, new Vector2(0, WallY), 13);
        }

        /// <summary>圆形道具钮：稀有度色圆底 + 图标 + 右上角红色数量角标 + 底部名字条，外圈金环。</summary>
        SpriteRenderer ItemButton(int index, Vector2 pos, string icon, string name, Color disc, out TextMesh text)
        {
            var btn = Gfx.Sprite(_low, Art.Circle, disc, pos, Vector2.one, 50, "item");
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
            CatchLayout();
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
            else
            {
                if (_press != null) CancelPress();
                ReleaseHeld();
            }
            SyncAll(false);
            TickBossHex(Paused ? 0f : Time.deltaTime);
            if (!Paused) TickLater(Time.deltaTime);
            UpdateFx(Time.deltaTime);
            TickMap(Time.deltaTime);
            TickScouts(Paused ? 0f : Time.deltaTime);
            UpdateTip(Time.deltaTime);
            UpdateCard(Time.deltaTime);
            UpdateBubble(Time.deltaTime);
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
            bool dragging = _drag != null && _dragMoved;
            bool over = dragging && OverRecruit(DragAim(PointerWorld()));
            bool can = p.CanPay && !dragging;
            float ratio = p.RecruitCost <= 0 ? 0f : Mathf.Clamp01(p.Buns / (float)p.RecruitCost);
            if (ratio < _recruitShown - 0.02f) _recruitShown = ratio;
            else _recruitShown = Mathf.MoveTowards(_recruitShown, ratio, Time.deltaTime * 2.4f);
            bool full = _recruitShown >= 0.999f && ratio >= 0.999f;
            if (full && !_recruitWasFull) _recruitFlash = 1f;
            _recruitWasFull = full;
            _recruitFlash = Mathf.Max(0f, _recruitFlash - Time.deltaTime * 2.2f);
            float pulse = can ? 1f + Mathf.Sin(Time.time * 6f) * 0.025f : over ? 1.08f : 1f;
            float sink = _recruitDown ? -0.06f : 0f;
            float squash = _recruitDown ? 0.94f : 1f;
            _recruitBtn.transform.localPosition = new Vector2(0.2f, ButtonY + sink);
            _recruitRim.localPosition = new Vector2(0.2f, ButtonY + sink);
            _recruitLabel.transform.localPosition = new Vector2(0.2f, ButtonY + 0.14f + sink);
            _recruitCost.transform.localPosition = new Vector2(0.32f, ButtonY - 0.27f + sink);
            _recruitBun.localPosition = new Vector2(-0.06f, ButtonY - 0.27f + sink);
            _recruitBtn.size = new Vector2(RecruitW, RecruitH) * pulse * squash;
            var face = over ? Pal.Ink : Pal.Hex("3A342C");
            _recruitBtn.color = _recruitDown ? face * 0.82f : face;
            _recruitFill.enabled = !over && _recruitShown > 0.02f;
            if (_recruitFill.enabled)
            {
                var sprite = full ? Art.Rounded : Art.Progress;
                if (_recruitFill.sprite != sprite) _recruitFill.sprite = sprite;
                float w = (full ? RecruitW : Mathf.Max(0.36f, RecruitW * _recruitShown)) * pulse * squash;
                float h = RecruitH * pulse * squash;
                float left = 0.2f - RecruitW * pulse * squash * 0.5f;
                _recruitFill.size = new Vector2(w, h);
                _recruitFill.transform.localPosition = new Vector2(left + w * 0.5f, ButtonY + sink);
                var body = Pal.Hex("8A5649");
                var ready = Pal.Hex("BD6247");
                var hot = Pal.Hex("E88862");
                float breathe = can ? 0.5f + 0.5f * Mathf.Sin(Time.time * 4.5f) : 0f;
                _recruitFill.color = full ? Color.Lerp(ready, hot, Mathf.Max(_recruitFlash, breathe)) : body;
            }
            _recruitLabel.text = over ? "遣散" : "聚  义";
            _recruitCost.text = over ? "+" + Side.DismissRefund(_drag.Piece) : p.RecruitCost.ToString();

            SyncItem(0, _mengBtn, _mengText, p.Items.MengHan, p.MengCd, Tuning.CdMeng);
            SyncItem(1, _baoziBtn, _baoziText, p.Items.Baozi, p.BaoziCd, Tuning.CdBaozi);
            SyncItem(2, _shuBtn, _shuText, p.Items.TianShu, p.ShuCd, Tuning.CdShu);
            SyncItem(3, _fuBtn, _fuText, p.Items.Fu, p.FuCd, Tuning.CdFu);
            SyncItem(4, _trapBtn, _trapText, p.Items.Trap, p.TrapCd, Tuning.CdTrap);
            SyncItem(5, _penBtn, _penText, p.Items.Pen, p.PenCd, Tuning.CdPen);
            PlaceItems();
            PlacePassives();

            bool rescue = p.AtTheGate && !p.RescueUsed;
            if (!Paused) TickShovelOffer(Time.deltaTime, rescue);
            if (rescue != _rescueRoot.gameObject.activeSelf)
            {
                _rescueRoot.gameObject.SetActive(rescue);
                if (rescue) Tween.Pop(_rescueRoot, 0.3f);
            }
            if (rescue) _rescueRoot.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 8f) * 0.06f) * (_rescueDown ? 0.9f : 1f);

            _waveText.text = (M.Chapter.MaxWaves > 0
                ? (M.Wave > 0 ? $"第 {M.Wave}/{M.Chapter.MaxWaves} 波" : $"共 {M.Chapter.MaxWaves} 波")
                : (M.Wave > 0 ? $"第 {M.Wave} 波" : "备战")) + " · 宿敌" + M.Chapter.RivalName + " [军情]";
            float next = M.NextWaveIn;
            _midText.text = p.ShieldT > 0 ? $"宋江护体 {p.ShieldT:0} 秒"
                : M.FinalWaveSpawned ? "肃清来敌即获大捷"
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
                bool pressed = _press != null && _press.Btn == btn && !_pressDrag;
                btn.transform.localScale = Vector3.one * _activeScale * breathe * pop * (pressed ? 0.9f : 1f);
                if (_actives != null) btn.color = pressed ? _actives[i].Disc * 0.82f : _actives[i].Disc;
            }
            float remain = max <= 0f ? 0f : Mathf.Clamp01(cd / max);
            if (cooling) _itemCd[i].ShowVeil(remain);
            else _itemCd[i].Hide();
            var label = _itemCdText[i];
            if (label.gameObject.activeSelf != cooling) label.gameObject.SetActive(cooling);
            if (cooling) label.text = Mathf.CeilToInt(cd).ToString();
        }

        static Vector2 ItemSlot(int i) => new Vector2(-1.5f + i * ItemGap, ItemY);

        /// <summary>主动道具和聚义同一行，靠左下角。一颗时放在聚义左边，多颗往聚义那边挤。</summary>
        void PlaceItems()
        {
            var btns = new[] { _mengBtn, _baoziBtn, _shuBtn, _fuBtn, _trapBtn, _penBtn };
            int shown = 0;
            foreach (var b in btns) if (b.gameObject.activeSelf) shown++;
            shown = Mathf.Min(shown, 2);
            const float left = -3.72f;
            const float right = -1.48f;
            _activeScale = shown <= 1 ? 0.96f : shown == 2 ? 0.86f : shown == 3 ? 0.74f : 0.64f;
            _activeGap = 1.15f;
            float x0 = -2.75f;
            if (shown > 1)
            {
                float room = right - left - _activeScale;
                _activeGap = Mathf.Min(1.05f, room / (shown - 1));
                x0 = right - _activeScale * 0.5f - (shown - 1) * _activeGap;
            }
            int s = 0;
            for (int i = 0; i < btns.Length; i++)
            {
                if (!btns[i].gameObject.activeSelf) continue;
                if (s >= 2)
                {
                    btns[i].gameObject.SetActive(false);
                    continue;
                }
                float sink = _press != null && _press.Btn == btns[i] && !_pressDrag ? -0.05f : 0f;
                btns[i].transform.localPosition = new Vector2(x0 + s * _activeGap, ButtonY + sink);
                s++;
            }
        }

        void BuildPassives()
        {
            foreach (var it in ShopItem.All)
            {
                if (it.Active) continue;
                _passiveItems.Add(it);
                _passiveBtns.Add(PassiveButton(it));
            }
        }

        SpriteRenderer PassiveButton(ShopItem it)
        {
            var btn = Gfx.Sprite(_low, Art.Circle, it.DiscColor, new Vector2(0.2f, ItemY), Vector2.one, 50, "passive");
            btn.gameObject.SetActive(false);
            var t = btn.transform;
            t.localScale = Vector3.one * 0.64f;
            Gfx.Sprite(t, Art.Circle, Pal.Shadow, new Vector2(0.03f, -0.07f), Vector2.one * 1.04f, 47);
            Gfx.Sprite(t, Art.Circle, Pal.Ink, Vector2.zero, Vector2.one * 1.04f, 48);
            Gfx.Sprite(t, Art.Ring, Pal.Gold.A(0.9f), Vector2.zero, Vector2.one * 1.1f, 49);
            Gfx.Sprite(t, Art.Img(it.Icon), Color.white, new Vector2(0, 0.06f), Vector2.one * 0.72f, 54);
            Gfx.Sprite(t, Art.Rounded, Pal.Ink.A(0.82f), new Vector2(0, -0.42f), new Vector2(1.2f, 0.28f), 56);
            Gfx.Text(t, it.Name, Fonts.Kai, 0.16f, Pal.Paper, new Vector2(0, -0.42f), 57);
            return btn;
        }

        static bool PassiveOwned(Loadout l, ShopItem it)
        {
            switch (it.Id)
            {
                case 0: return l.Gang;
                case 1: return l.Flag;
                case 2: return l.Marsh;
                case 3: return l.Stele;
                case 7: return l.Xuming;
                case 8: return l.Shovels > 0;
                case 10: return l.Far;
                case 12: return l.Stove;
                case 13: return l.Wine;
                default: return false;
            }
        }

        /// <summary>被动挂在聚义正下方，点开只看说明，不会被消耗。</summary>
        void PlacePassives()
        {
            var owned = M.Player.Items;
            int n = 0;
            for (int i = 0; i < _passiveBtns.Count; i++)
            {
                bool on = PassiveOwned(owned, _passiveItems[i]);
                if (_passiveBtns[i].gameObject.activeSelf != on) _passiveBtns[i].gameObject.SetActive(on);
                if (on) n++;
            }
            if (n == 0) return;
            float gap = n <= 1 ? 0f : Mathf.Min(0.78f, 2.5f / (n - 1));
            float x0 = 0.2f - (n - 1) * gap * 0.5f;
            float leftEdge = x0 - 0.34f;
            if (leftEdge < -1.05f) x0 += -1.05f - leftEdge;
            int s = 0;
            for (int i = 0; i < _passiveBtns.Count; i++)
            {
                if (!_passiveBtns[i].gameObject.activeSelf) continue;
                bool down = _passiveDown == i;
                _passiveBtns[i].transform.localPosition = new Vector2(x0 + s * gap, ItemY + (down ? -0.04f : 0f));
                _passiveBtns[i].transform.localScale = Vector3.one * (down ? 0.58f : 0.64f);
                s++;
            }
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
                    v.SetStun(piece.StunT > 0);
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
                else if (f.WantsArt && Art.HeroFrame(h.Def.Id, 0) != null)
                {
                    f.Destroy();
                    f = new FrameView(this, h, s, true);
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
                v.SetStun(piece.StunT > 0);
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
                v = new PieceView(side == 0 ? _low : _root, p, side);
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
                        v = new EnemyView(s == 0 ? _low : _root, e, s);
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
                case Ev.GearUp:
                {
                    var pos = World(ev.Side, ev.P);
                    var w = HeroDef.Get((HeroId)ev.B);
                    var c = Ui.GradeColor(w.WGrade);
                    Ring(pos, 1.8f, c, 0.45f, 45);
                    FloatText(pos + new Vector2(0, 0.95f), ev.Text, mine ? c : Pal.RedDeep, mine ? 0.32f : 0.26f, 1.8f);
                    if (mine) FloatText(pos + new Vector2(0, 0.62f), GearGain(ev.Side, ev.A, w), Pal.Paper, 0.22f, 1.8f);
                    if (_frames.TryGetValue(ev.A, out var fv)) fv.Glow();
                    if (mine) Sfx.Play(Snd.Merge, 0.8f, 1.5f);
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
                    if (mine) DamageNumber(World(ev.Side, ev.P), ev.Val, ev.Flag, ev.B == 1);
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
                    if (ev.Flag == 1) { BossCastFx(ev); break; }
                    var pos = World(ev.Side, ev.P);
                    var ud = HeroDef.Get((HeroId)ev.B);
                    float r = Mathf.Min(ev.Val, 6f);
                    if (_frames.TryGetValue(ev.A, out var caster)) caster.StrikeAt(pos, SwingOf(ud));
                    bool custom = HeroUltFx(ev);
                    if (!custom && r > 0.1f)
                    {
                        Ring(pos, r * 2f, Pal.Ink, 0.5f, 44);
                        InkSwirl(pos, Mathf.Clamp(r * 2f, 1.6f, 7f), Pal.Ink.A(0.8f), 0.8f);
                    }
                    string word = ev.Text ?? "";
                    int colon = word.IndexOf('：');
                    if (colon > 0) word = word.Substring(0, colon);
                    if (mine)
                    {
                        if (!string.IsNullOrEmpty(word)) UltCutIn(ud, word);
                        Sfx.Play(Snd.Ult, 0.45f);
                        Sfx.Play(UltSound(ud), 0.85f, ud.Look == Look.Poison ? 0.8f : 1f);
                        _shake = Mathf.Max(_shake, 0.9f);
                    }
                    else if (!string.IsNullOrEmpty(word)) FloatText(HeroWorld(ev) + new Vector2(0, -0.7f), word + "！", Pal.RedDeep, 0.34f, 1.1f);
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
                    if (mine && ev.Flag == 2 && _pieces.TryGetValue(ev.A, out var stove)) stove.Punch(0.14f);
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
                case Ev.NemesisDuel:
                {
                    var from = World(ev.Side, ev.P);
                    var to = World(ev.Side, ev.Q);
                    Ring(from, 2.8f, Pal.GoldBright, 0.55f, 46);
                    Streak(from, to, 0.2f, 0.12f, Pal.Red, Pal.GoldBright, 0.35f, 47);
                    FloatText(from + Vector2.up * 0.72f, "宿命克星！", Pal.Red, mine ? 0.38f : 0.28f, 1.2f);
                    if (_frames.TryGetValue(ev.A, out var dfv)) dfv.Glow();
                    if (mine) _shake = Mathf.Max(_shake, 0.65f);
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
                case Ev.BossHex:
                    BossHexFx(ev);
                    break;
                case Ev.BossRage:
                    BossRageFx(ev);
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
                case "萧让代笔": icon = "wp_fan"; btn = _penBtn; break;
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
            SpriteRenderer sr = null;
            while (_spriteFxPool.Count > 0 && sr == null)
                sr = _spriteFxPool.Pop();
            if (sr != null)
            {
                var go = sr.gameObject;
                go.SetActive(true);
                var tr = go.transform;
                tr.localPosition = pos;
                tr.localRotation = Quaternion.identity;
                sr.sprite = s;
                sr.color = c;
                sr.sortingOrder = order;
                if (s != null && s.border != Vector4.zero)
                {
                    sr.drawMode = SpriteDrawMode.Sliced;
                    tr.localScale = Vector3.one;
                    sr.size = size;
                }
                else
                {
                    sr.drawMode = SpriteDrawMode.Simple;
                    tr.localScale = new Vector3(size.x, size.y, 1f);
                }
            }
            else
            {
                sr = Gfx.Sprite(_fxRoot, s, c, pos, size, order, "fx");
            }
            var fx = new Fx { Life = life, Step = step, C = c, Sr = sr, Go = sr.gameObject };
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
                f.Step?.Invoke(f, k);
                if (k >= 1f)
                {
                    if (f.Sr != null && f.Tm == null && f.Go.transform.childCount == 0 && _spriteFxPool.Count < MaxPooledSprites)
                    {
                        f.Go.SetActive(false);
                        _spriteFxPool.Push(f.Sr);
                    }
                    else Destroy(f.Go);
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

        /// <summary>击杀时飞溅的墨点（高波次自动限流防掉帧）。</summary>
        void InkDrops(Vector2 pos, int n)
        {
            if (HeavyFxLoad) return;
            if (LowFxLoad) n = Mathf.Max(1, n / 2);
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

        /// <summary>伤害数字。好汉打的用金字、大一号、描暗红边；高波次对小兵普通微伤自动节流，暴击/克星/好汉金字照常全出。</summary>
        void DamageNumber(Vector2 pos, float val, int flag, bool hero = false)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(val));
            if (!hero && flag == 0 && n < 18 && LowFxLoad)
            {
                _minorDmgSkip++;
                if (HeavyFxLoad ? (_minorDmgSkip % 3 != 0) : (_minorDmgSkip % 2 != 0)) return;
            }
            bool big = flag > 0 || n >= 18 || hero;
            float h = flag == 2 || n >= 40 ? 0.52f : flag > 0 ? 0.38f : hero ? 0.34f : big ? 0.38f : 0.26f;
            if (hero) h *= 1.1f;
            string s = flag == 2 ? "克" + n : flag == 3 ? "半" + n : flag == 1 ? n + "!" : n.ToString();
            Color c = flag == 2 ? Pal.Red : flag == 1 ? Pal.Gold : flag == 3 ? Pal.Hex("8A5A44") : hero ? Pal.GoldBright : Pal.Ink;
            Color edge = hero ? Pal.RedDeep : Pal.Paper;
            var at = pos + new Vector2(UnityEngine.Random.Range(-0.2f, 0.2f), hero ? 0.5f : 0.4f);
            var fx = new Fx { Life = big ? 0.8f : 0.5f, C = c, A = at };
            fx.Tm = Gfx.Text(_fxRoot, s, Fonts.Kai, h, c, at, 49, TextAnchor.MiddleCenter, "dmg");
            fx.Go = fx.Tm.gameObject;
            var rim = new TextMesh[4];
            for (int i = 0; i < 4; i++)
            {
                var o = new Vector2(i % 2 == 0 ? -1 : 1, i < 2 ? -1 : 1) * (hero ? 0.028f : 0.022f);
                rim[i] = Gfx.Text(fx.Go.transform, s, Fonts.Kai, h, edge, o, 48);
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
                foreach (var r in rim) r.color = edge.A(0.9f * a);
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

        void HoldScale(Transform t, ref bool held, ref Vector3 rest)
        {
            if (t == null || held) return;
            held = true;
            rest = t.localScale;
            t.localScale = rest * 0.9f;
        }

        void ReleaseHeld()
        {
            _recruitDown = _rescueDown = false;
            _passiveDown = -1;
            if (_pauseDown)
            {
                _pauseDown = false;
                if (_pauseBtn) _pauseBtn.transform.localScale = _pauseRest;
            }
            if (_shovelDown)
            {
                _shovelDown = false;
                if (_shovelRoot) _shovelRoot.localScale = _shovelRest;
            }
        }

        void HandleInput()
        {
            var p = M.Player;
            var w = PointerWorld();

            if (!Input.GetMouseButton(0)) ReleaseHeld();

            if (Input.GetMouseButtonDown(0))
            {
                _idleT = 0;
                if (Ui.PointerOverUi()) return;
                // 说明还开着时，这一下只负责关掉，不顺手再点到聚义或下一颗道具。
                if (_card != null && _card.gameObject.activeSelf && _frame != _cardFrame) { HideCard(); return; }
                string bubbleWas = _bubble.gameObject.activeSelf ? _bubbleKey : null;
                HideBubble();
                var active = HitActive(w);
                if (active != null) { BeginPress(active, w); return; }
                _downPos = w;
                _dragMoved = false;
                if (In(w, _recruitBtn.transform.localPosition, new Vector2(2.8f, 1.1f)))
                {
                    _recruitDown = true;
                    if (p.RefreshCamp()) Sfx.Play(Snd.Tap);
                    else
                    {
                        Sfx.Play(Snd.Deny);
                        Tip(_recruitBtn.transform.localPosition + Vector3.up * 1.1f, "炊饼不够，多杀几个官兵");
                    }
                    return;
                }
                if (In(w, _pauseBtn.transform.localPosition, new Vector2(0.9f, 0.9f)))
                {
                    HoldScale(_pauseBtn.transform, ref _pauseDown, ref _pauseRest);
                    Sfx.Play(Snd.Tap);
                    OnPauseClicked?.Invoke();
                    return;
                }
                if (In(w, new Vector2(0.15f, TopY), new Vector2(3.4f, 0.78f)) || In(w, new Vector2(0f, WallY), new Vector2(3.3f, 0.42f)))
                {
                    Sfx.Play(Snd.Tap, 0.7f);
                    ShowChapterIntel();
                    return;
                }
                for (int i = 0; i < _passiveBtns.Count; i++)
                {
                    if (!HitItem(_passiveBtns[i], w, new Vector2(0.78f, 0.78f))) continue;
                    _passiveDown = i;
                    if (bubbleWas != "passive:" + _passiveItems[i].Id) ShowPassiveBubble(_passiveItems[i]);
                    return;
                }
                if (_shovelRoot.gameObject.activeSelf && In(w, _shovelRoot.localPosition, new Vector2(0.95f, 1.1f)))
                {
                    HoldScale(_shovelRoot, ref _shovelDown, ref _shovelRest);
                    Sfx.Play(Snd.Tap);
                    ShovelAd();
                    return;
                }
                if (_rescueRoot.gameObject.activeSelf && In(w, _rescueRoot.localPosition, new Vector2(0.95f, 1.2f)))
                {
                    _rescueDown = true;
                    Sfx.Play(Snd.Tap);
                    RescueAd();
                    return;
                }

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

            if (_press != null) { UpdatePress(w); return; }

            if (_drag != null)
            {
                if ((w - _downPos).magnitude > 0.18f) _dragMoved = true;
                if (_dragMoved) { _drag.Target = DragAim(w); HideCard(); }
                ShowHighlights(_drag.Piece, w);

                if (Input.GetMouseButtonUp(0)) EndDrag(w);
            }
        }

        static bool HitItem(SpriteRenderer btn, Vector2 w, Vector2 size) =>
            btn.gameObject.activeSelf && In(w, btn.transform.localPosition, size);

        // ─────────────────────────── 主动道具：拖上战场放出 ───────────────────────────

        sealed class ActiveItem
        {
            public SpriteRenderer Btn;
            public Func<bool> Use;
            public Func<float> Cd;
            public float Max;
            public Func<int> Count;
            public string Name, Desc, Icon;
            public Color Disc;
            /// <summary>要落在某一格上才生效（天书）；不设就是整片战场。</summary>
            public Func<int, int, bool> CanAt, UseAt;
        }

        const float LongPress = 0.32f;
        ActiveItem[] _actives;
        ActiveItem _press;
        float _pressT;
        Vector2 _pressPos;
        bool _pressDrag;
        Transform _ghost;
        SpriteRenderer _ghostDisc, _ghostIcon, _zone, _zoneRim;
        TextMesh _zoneText;
        SpriteRenderer _zonePill;
        readonly SpriteRenderer[,] _aimRim = new SpriteRenderer[Board.Cols, Board.Rows], _aimTag = new SpriteRenderer[Board.Cols, Board.Rows];
        readonly TextMesh[,] _aimText = new TextMesh[Board.Cols, Board.Rows];

        void BuildActives()
        {
            var p = M.Player;
            _actives = new[]
            {
                new ActiveItem { Btn = _mengBtn, Use = p.UseMengHan, Cd = () => p.MengCd, Max = Tuning.CdMeng, Count = () => p.Items.MengHan, Name = "蒙汗药", Desc = "官兵停住3秒，不走也不打", Icon = "it_menghan", Disc = Pal.Hex("C9A6EE") },
                new ActiveItem { Btn = _baoziBtn, Use = p.UseBaozi, Cd = () => p.BaoziCd, Max = Tuning.CdBaozi, Count = () => p.Items.Baozi, Name = "包子", Desc = "多半给宋江加一颗心，不成就掉一颗", Icon = "bun", Disc = Pal.Hex("A4D8F2") },
                new ActiveItem { Btn = _shuBtn, UseAt = p.UseTianShu, CanAt = p.ShuTarget, Cd = () => p.ShuCd, Max = Tuning.CdShu, Count = () => p.Items.TianShu, Name = "天书", Desc = "拖到兵或灶上升一级，满级的不行", Icon = "it_tianshu", Disc = Pal.Hex("F6C46B") },
                new ActiveItem { Btn = _fuBtn, Use = p.UseFu, Cd = () => p.FuCd, Max = Tuning.CdFu, Count = () => p.Items.Fu, Name = "攻速符", Desc = "出手快四成，管十二秒，满级也算", Icon = "it_fu", Disc = Pal.Hex("C9A6EE") },
                new ActiveItem { Btn = _trapBtn, Use = p.UseTrap, Cd = () => p.TrapCd, Max = Tuning.CdTrap, Count = () => p.Items.Trap, Name = "陷阱", Desc = "官兵被推回去，还得愣一会儿", Icon = "it_trap", Disc = Pal.Hex("C5E08A") },
                new ActiveItem { Btn = _penBtn, UseAt = p.UsePen, CanAt = p.PenTarget, Cd = () => p.PenCd, Max = Tuning.CdPen, Count = () => p.Items.Pen, Name = "萧让代笔", Desc = "拖到单字上补缺的字，拖到兵上把兵变成字", Icon = "wp_fan", Disc = Pal.Hex("C9A6EE") },
            };

            float fieldY = MyBottom + Board.Rows * 0.5f;
            _zone = Gfx.Sprite(_low, Art.White, Color.clear, new Vector2(0, fieldY), new Vector2(Board.Cols, Board.Rows), 15, "itemZone");
            _zoneRim = Gfx.Sprite(_low, Art.Frame, Color.clear, new Vector2(0, fieldY), new Vector2(Board.Cols - 0.1f, Board.Rows - 0.1f), 16, "itemZoneRim");
            _zoneText = Gfx.Text(_root, "", Fonts.Brush, 0.46f, Pal.Paper, new Vector2(0, fieldY), 48);
            _zone.enabled = _zoneRim.enabled = false;
            _zonePill = Gfx.Sprite(_root, Art.Rounded, Pal.Ink.A(0.72f), new Vector2(0, fieldY), new Vector2(3.2f, 0.66f), 47, "zonePill");
            _zonePill.enabled = false;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    var at = CellWorld(0, c, r);
                    _aimRim[c, r] = Gfx.Sprite(_root, Art.Frame, Color.clear, at, new Vector2(1.02f, 1.02f), 44, "aim");
                    _aimTag[c, r] = Gfx.Sprite(_root, Art.Circle, Pal.Gold, at + new Vector2(-0.3f, 0.3f), Vector2.one * 0.36f, 45, "aimTag");
                    _aimText[c, r] = Gfx.Text(_root, "升", Fonts.Brush, 0.26f, Pal.Paper, at + new Vector2(-0.3f, 0.29f), 46);
                    _aimRim[c, r].enabled = _aimTag[c, r].enabled = false;
                    _aimText[c, r].gameObject.SetActive(false);
                }

            _ghost = new GameObject("itemGhost").transform;
            _ghost.SetParent(_root, false);
            Gfx.Sprite(_ghost, Art.Circle, Pal.Shadow, new Vector2(0.04f, -0.1f), Vector2.one * 1.04f, 90);
            Gfx.Sprite(_ghost, Art.Circle, Pal.Ink, Vector2.zero, Vector2.one * 1.04f, 91);
            _ghostDisc = Gfx.Sprite(_ghost, Art.Circle, Color.white, Vector2.zero, Vector2.one, 92);
            _ghostIcon = Gfx.Sprite(_ghost, Art.White, Color.white, new Vector2(0, 0.02f), Vector2.one * 0.74f, 93);
            _ghost.gameObject.SetActive(false);
        }

        ActiveItem HitActive(Vector2 w)
        {
            float hitW = Mathf.Min(_activeScale + 0.2f, _activeGap * 0.92f);
            var size = new Vector2(hitW, 1.15f);
            foreach (var a in _actives)
                if (HitItem(a.Btn, w, size)) return a;
            return null;
        }

        /// <summary>松手的地方在山寨上面就算放到了战场上。</summary>
        static bool OverField(Vector2 w) => w.y > CampY + 0.55f;

        void BeginPress(ActiveItem a, Vector2 w)
        {
            _press = a;
            _pressT = 0;
            _pressPos = w;
            _pressDrag = false;
            Sfx.Play(Snd.Tap, 0.45f, 1.2f);
        }

        void UpdatePress(Vector2 w)
        {
            var a = _press;
            _pressT += Time.deltaTime;
            if (!_pressDrag && (w - _pressPos).magnitude > 0.25f)
            {
                _pressDrag = true;
                HideBubble();
            }
            if (!_pressDrag && _pressT >= LongPress && _bubbleKey != "hold:" + a.Name)
            {
                ShowActiveBubble(a, false);
                _bubbleKey = "hold:" + a.Name;
            }

            if (_pressDrag) DrawDrag(a, w);

            if (!Input.GetMouseButtonUp(0)) return;
            CancelPress();
            if (_pressDrag)
            {
                if (OverField(w)) CastActive(a, w);
                return;
            }
            if (_pressT >= LongPress) { HideBubble(); return; }
            ShowActiveBubble(a, true);
        }

        /// <summary>道具跟着手指走；进了战场整片己方战场亮起来，表示松手就作用到这里。</summary>
        void DrawDrag(ActiveItem a, Vector2 w)
        {
            if (_ghostIcon.sprite != Art.Img(a.Icon)) _ghostIcon.sprite = Art.Img(a.Icon);
            _ghostDisc.color = a.Disc;
            _ghost.gameObject.SetActive(true);
            bool field = OverField(w);
            bool cooling = a.Cd() > 0.05f;
            _ghost.localPosition = w + new Vector2(0, 0.55f);
            _ghost.localScale = Vector3.one * (field ? 0.92f : 0.78f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 7f);
            if (a.CanAt != null)
            {
                DrawTargets(a, w, field, cooling, pulse);
                return;
            }
            _zone.enabled = _zoneRim.enabled = field;
            _zoneText.text = field ? (cooling ? $"还要 {Mathf.CeilToInt(a.Cd())} 秒" : "松手放出" + a.Name) : "";
            var tint = cooling ? Pal.InkSoft : a.Disc;
            _zone.color = tint.A(0.16f + 0.08f * pulse);
            _zoneRim.color = (cooling ? Pal.Ink : Pal.GoldBright).A(0.55f + 0.35f * pulse);
        }

        /// <summary>点名的道具（天书/萧让代笔）：能落的格子描金标「升/配/化」，指到的那格更亮；字写在指到的格子上方。</summary>
        void DrawTargets(ActiveItem a, Vector2 w, bool field, bool cooling, float pulse)
        {
            _zone.enabled = false;
            _zoneRim.enabled = false;
            bool isPen = a.Name == "萧让代笔";
            bool hover = TryMyCell(w + new Vector2(0, 0.55f), out var hc, out var hr);
            bool hit = false;
            int any = 0;
            string hoverPenLine = "";
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    bool ok = !cooling && a.CanAt(c, r);
                    if (ok) any++;
                    bool on = ok && hover && c == hc && r == hr;
                    hit |= on;
                    _aimRim[c, r].enabled = _aimTag[c, r].enabled = ok;
                    _aimText[c, r].gameObject.SetActive(ok);
                    if (!ok) continue;
                    if (isPen)
                    {
                        bool hasPrev = M.Player.PreviewPen(c, r, out char ch, out var hd, out bool isUnit);
                        _aimText[c, r].text = isUnit ? "化" : "配";
                        if (on && hasPrev)
                            hoverPenLine = isUnit
                                ? $"松手化为「{ch}」（凑{hd.Name}）"
                                : $"松手补「{ch}」凑{hd.Name}";
                    }
                    else _aimText[c, r].text = "升";
                    _aimRim[c, r].color = on ? Pal.GoldBright : Pal.GoldBright.A(0.55f + 0.4f * pulse);
                    _aimRim[c, r].transform.localScale = Vector3.one * (on ? 1.08f : 1f);
                    _aimTag[c, r].color = on ? Pal.RedDeep : Pal.Gold;
                }
            _zonePill.enabled = field;
            if (!field) { _zoneText.text = ""; return; }
            if (cooling) _zoneText.text = $"还要 {Mathf.CeilToInt(a.Cd())} 秒";
            else if (isPen)
                _zoneText.text = hit && hoverPenLine.Length > 0 ? hoverPenLine
                    : any == 0 ? "先摆孤字或兵上场" : "拖到孤字补配对，或拖到兵上化金字";
            else
                _zoneText.text = hit ? "松手升一级"
                    : any == 0 ? "场上没有能升级的" : "拖到发光的兵或灶上";
            _zonePill.size = new Vector2(Mathf.Clamp(_zoneText.text.Length * 0.40f + 0.65f, 3.2f, 7.2f), 0.66f);
            var at = hit ? CellWorld(0, hc, hr) + new Vector2(0, 1.25f) : new Vector2(0, MyBottom + Board.Rows * 0.5f);
            float halfW = _zonePill.size.x * 0.5f + 0.1f;
            at.x = Mathf.Clamp(at.x, Left + halfW, -Left - halfW);
            _zoneText.transform.localPosition = at;
            _zonePill.transform.localPosition = at;
        }

        /// <summary>验收截图用：摆出拖着第 i 颗主动道具悬在战场上的样子。</summary>
        public void PreviewItemDrag(int i, Vector2 w) => DrawDrag(_actives[i], w);

        /// <summary>验收截图用：摆两个兵和一张灶，返回拖天书悬在第一个兵上的手指位置。</summary>
        public bool ShotShuTarget(out Vector2 w)
        {
            HideBubble();
            var p = M.Player;
            p.Items.TianShu = Math.Max(1, p.Items.TianShu);
            p.ShuCd = 0f;
            int placed = 0;
            w = default;
            for (int r = Board.Rows - 1; r >= 0 && placed < 2; r--)
                for (int c = 0; c < Board.Cols && placed < 2; c++)
                {
                    if (!p.Board.Placeable(c, r) || p.Board.Pieces[c, r] != null) continue;
                    p.Board.Pieces[c, r] = Piece.Unit(placed == 0 ? UnitKind.Dao : UnitKind.Gong, placed + 1);
                    if (placed == 0) w = CellWorld(0, c, r) - new Vector2(0, 0.55f);
                    placed++;
                }
            if (!p.FindStove(out _, out _))
                for (int r = 0; r < Board.Rows; r++)
                    for (int c = Board.Cols - 1; c >= 0; c--)
                        if (p.Board.Placeable(c, r) && p.Board.Pieces[c, r] == null && !p.FindStove(out _, out _))
                            p.Board.Pieces[c, r] = Piece.Stove();
            p.RecomputeHeroes();
            return placed > 0;
        }

        /// <summary>验收截图用：点开场上那张灶的说明。</summary>
        public void ShotStoveInfo()
        {
            CancelPress();
            var p = M.Player;
            if (p.FindStove(out int c, out int r) && _pieces.TryGetValue(p.Board.Pieces[c, r].Id, out var v)) ShowInfo(v);
        }

        /// <summary>验收截图用：长按第 i 颗主动道具弹出的说明。</summary>
        public void PreviewItemBubble(int i)
        {
            CancelPress();
            ShowActiveBubble(_actives[i], false);
        }

        void CancelPress()
        {
            _press = null;
            _ghost.gameObject.SetActive(false);
            _zone.enabled = _zoneRim.enabled = false;
            _zoneText.text = "";
            _zoneText.transform.localPosition = new Vector2(0, MyBottom + Board.Rows * 0.5f);
            _zonePill.enabled = false;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    _aimRim[c, r].enabled = _aimTag[c, r].enabled = false;
                    _aimText[c, r].gameObject.SetActive(false);
                }
            if (_bubbleKey != null && _bubbleKey.StartsWith("hold:")) HideBubble();
        }

        void CastActive(ActiveItem a, Vector2 w)
        {
            var at = new Vector2(Mathf.Clamp(w.x, -2.6f, 2.6f), w.y + 0.9f);
            if (a.Cd() > 0.05f)
            {
                Sfx.Play(Snd.Deny);
                Tip(at, $"{a.Name}还要 {Mathf.CeilToInt(a.Cd())} 秒");
                return;
            }
            if (a.UseAt != null)
            {
                if (!TryMyCell(w + new Vector2(0, 0.55f), out var c, out var r) || !a.UseAt(c, r))
                {
                    Sfx.Play(Snd.Deny);
                    string failTip = a.Name == "萧让代笔"
                        ? (AnyTarget(a) ? "拖到孤字或小兵身上才落笔，这支没用掉" : "场上还没有孤字或小兵")
                        : (AnyTarget(a) ? "拖到兵或灶上才算数，这本没用掉" : "场上没有能升级的");
                    Tip(at, failTip);
                    return;
                }
                Ring(CellWorld(0, c, r), 0.9f, a.Disc, 0.4f, 46);
                return;
            }
            if (!a.Use())
            {
                Sfx.Play(Snd.Deny);
                Tip(at, "现在用不了");
                return;
            }
            Ring(w, 2.2f, a.Disc, 0.45f, 46);
        }

        static bool AnyTarget(ActiveItem a)
        {
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    if (a.CanAt(c, r)) return true;
            return false;
        }

        // ─────────────────────────── 说明气泡 ───────────────────────────

        Transform _bubble;
        SpriteRenderer _bubbleBg, _bubbleRim, _bubbleTail, _bubbleIcon;
        TextMesh _bubbleTitle, _bubbleBody;
        float _bubbleT;
        string _bubbleKey;
        const float BubbleW = 4.2f;

        void BuildBubble()
        {
            _bubble = new GameObject("bubble").transform;
            _bubble.SetParent(_root, false);
            _bubbleRim = Gfx.Sprite(_bubble, Art.Rounded, Pal.Ink, Vector2.zero, new Vector2(BubbleW + 0.08f, 1.2f), 84);
            _bubbleBg = Gfx.Sprite(_bubble, Art.Rounded, Pal.Paper, Vector2.zero, new Vector2(BubbleW, 1.12f), 85);
            _bubbleTail = Gfx.Sprite(_bubble, Art.White, Pal.Paper, Vector2.zero, Vector2.one * 0.22f, 86);
            _bubbleTail.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            _bubbleIcon = Gfx.Sprite(_bubble, Art.White, Color.white, Vector2.zero, Vector2.one * 0.6f, 87);
            _bubbleTitle = Gfx.Text(_bubble, "", Fonts.Brush, 0.32f, Pal.RedDeep, Vector2.zero, 87, TextAnchor.MiddleLeft);
            _bubbleBody = Gfx.Text(_bubble, "", Fonts.Kai, 0.22f, Pal.Ink, Vector2.zero, 87, TextAnchor.UpperLeft);
            _bubble.gameObject.SetActive(false);
        }

        void ShowActiveBubble(ActiveItem a, bool tapped)
        {
            float cd = a.Cd();
            string state = cd > 0.05f ? $"还要 {Mathf.CeilToInt(cd)} 秒" : "可以用";
            string tail = tapped ? "按住拖到战场上放出" : $"还剩 {Mathf.Max(0, a.Count())} 个　冷却 {Mathf.CeilToInt(a.Max)} 秒";
            ShowBubble(a.Btn.transform.localPosition.x, a.Name, state, a.Desc + "\n" + tail, a.Icon, tapped ? 2.2f : 999f);
            _bubbleKey = "tap:" + a.Name;
        }

        void ShowPassiveBubble(ShopItem it)
        {
            string key = "passive:" + it.Id;
            Sfx.Play(Snd.Tap, 0.7f);
            ShowBubble(_passiveBtns[_passiveItems.IndexOf(it)].transform.localPosition.x, it.Name, "被动　本局一直生效", it.Desc, it.Icon, 2.6f);
            _bubbleKey = key;
        }

        /// <summary>说明浮在山寨上方，尖角指着按下的那颗钮，不压道具和聚义那一排。</summary>
        void ShowBubble(float anchorX, string title, string state, string body, string icon, float dur)
        {
            HideCard();
            string text = WrapChars(body, 14, 4);
            int lines = 1;
            foreach (var ch in text) if (ch == '\n') lines++;
            float h = 0.74f + lines * 0.3f;
            float half = BubbleW * 0.5f;
            float x = Mathf.Clamp(anchorX, Left + half + 0.06f, -Left - half - 0.06f);
            float bottom = CampY + 0.66f;
            _bubble.localPosition = new Vector2(x, bottom + h * 0.5f);
            _bubbleRim.size = new Vector2(BubbleW + 0.08f, h + 0.08f);
            _bubbleBg.size = new Vector2(BubbleW, h);
            float tx = Mathf.Clamp(anchorX - x, -half + 0.3f, half - 0.3f);
            _bubbleTail.transform.localPosition = new Vector2(tx, -h * 0.5f);
            float left = -half + 0.2f;
            float top = h * 0.5f - 0.1f;
            _bubbleIcon.sprite = Art.Img(icon);
            _bubbleIcon.transform.localPosition = new Vector2(left + 0.3f, top - 0.3f);
            _bubbleTitle.text = title + "  <size=60><color=#6B5D4D>" + state + "</color></size>";
            _bubbleTitle.richText = true;
            _bubbleTitle.transform.localPosition = new Vector2(left + 0.72f, top - 0.24f);
            _bubbleBody.text = text;
            _bubbleBody.transform.localPosition = new Vector2(left + 0.72f, top - 0.5f);
            _bubble.gameObject.SetActive(true);
            _bubbleT = dur;
            Tween.Pop(_bubble, 0.14f);
        }

        void HideBubble()
        {
            if (_bubble != null) _bubble.gameObject.SetActive(false);
            _bubbleKey = null;
        }

        void UpdateBubble(float dt)
        {
            if (_bubble == null || !_bubble.gameObject.activeSelf || _press != null) return;
            _bubbleT -= dt;
            if (_bubbleT <= 0) HideBubble();
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

        public bool FreeRescue => M.Chapter.Index == 0 && !M.Chapter.IsDaily && !M.Chapter.IsEndless;

        public void RescueAd()
        {
            var p = M.Player;
            if (p.RescueUsed || M.Over) return;
            if (FreeRescue)
            {
                Analytics.TutorialStep("free_rescue");
                p.Rescue();
                return;
            }
            ShowAd("公孙胜作法", () => p.Rescue());
        }

        /// <summary>拖动时棋子画在手指上方。落点、高亮、遣散都认这块棋子的中心，不认被棋子挡住的手指。</summary>
        const float DragLift = 0.55f;
        static Vector2 DragAim(Vector2 finger) => finger + new Vector2(0f, DragLift);

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

            var aim = DragAim(w);
            bool onCell = TryMyCell(aim, out var c, out var r);
            int slot = onCell ? -1 : CampAt(aim);
            if (v.Piece.Type == PieceType.Stove && (slot >= 0 || OverRecruit(aim)))
            {
                Sfx.Play(Snd.Deny);
                v.Punch(0.1f);
                Tip(new Vector2(0, CampY + 1.1f), "灶搬不回山寨，只能在场上挪");
                return;
            }
            bool ok = false;
            if (onCell)
            {
                ok = _dragCamp >= 0 ? p.CampToCell(_dragCamp, c, r) : p.CellToCell(_dragCell.c, _dragCell.r, c, r);
                if (ok) { Sfx.Play(Snd.Place, 0.8f); OnPlaced?.Invoke(); }
                else if (v.Piece.Type == PieceType.Shovel) Tip(CellWorld(0, c, r) + Vector2.up * 0.9f, "铲子只能挖草地");
                else if (p.Board.Diggable(c, r)) Tip(CellWorld(0, c, r) + Vector2.up * 0.9f, "这块是草地，先用铲子开荒");
            }
            else if (slot >= 0)
            {
                ok = _dragCamp >= 0 ? p.CampToCamp(_dragCamp, slot) : p.CellToCamp(_dragCell.c, _dragCell.r, slot);
                if (ok) Sfx.Play(Snd.Tap, 0.7f);
            }
            else if (OverRecruit(aim))
            {
                ok = _dragCamp >= 0 ? p.DismissCamp(_dragCamp) : p.DismissCell(_dragCell.c, _dragCell.r);
                if (ok) return;
            }
            if (!ok) { Sfx.Play(Snd.Deny); v.Punch(0.1f); }
        }

        void ShowHighlights(Piece piece, Vector2 w)
        {
            var p = M.Player;
            var b = p.Board;
            bool hasHover = TryMyCell(DragAim(w), out var hc, out var hr);
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
                    else if (b.Open(c, r) && b.Blocked(c, r))
                    {
                        col = Pal.Red.A(0.22f);
                        mark = b.Seal[c, r] > 0 ? "封" : "云";
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
                float range = d.RangeWith(p.Items.Far);
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
            var to = DragAim(w);
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

        bool TryFindBoardPos(Piece target, out int bc, out int br)
        {
            var pcs = M.Player.Board.Pieces;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    if (ReferenceEquals(pcs[c, r], target)) { bc = c; br = r; return true; }
            bc = br = -1;
            return false;
        }

        /// <summary>点击顶栏回目名或「[军情]」，随时明牌查看本回好汉池、头目绝技解法、克星×5与宿敌流派。</summary>
        public void ShowChapterIntel()
        {
            if (_card != null && _card.gameObject.activeSelf && _cardKey == "intel")
            {
                HideCard();
                return;
            }
            SetPicked(null);
            HideRange();
            var ch = M.Chapter;
            var names = new List<string>();
            foreach (var id in ch.Pool)
            {
                var hd = HeroDef.Get(id);
                if (!hd.Hidden) names.Add(hd.Name);
            }
            var nem = HeroDef.Get(ch.Boss.Nemesis).Name;
            var tips = new List<string>();
            foreach (var sk in ch.Boss.Skills)
            {
                string tip = BossDef.CounterTip(sk);
                if (!tips.Contains(tip)) tips.Add(tip);
            }
            string ruleLine = M.Chapter.Rule == BattleRule.None ? "" : $"战场规则：【{M.Chapter.RuleTitle}】{M.Chapter.RuleDesc}";
            string body = Lines(
                $"本回好汉：{string.Join("、", names)}",
                ruleLine,
                $"头目：{ch.Boss.Title}{ch.Boss.Name}",
                $"克星：{nem}（伤×5+宿命加速）",
                $"绝技：{ch.Boss.SkillText}",
                $"破阵良策：{tips[0]}",
                $"宿敌：{ch.RivalName}（{ch.RivalStyleLabel}）");
            _cardKey = "intel";
            PlaceCard("军情密报　" + ch.Name, ch.MaxWaves > 0 ? $"共{ch.MaxWaves}波" : "点空白收起", body, "密", Pal.TileHero, null);
            _cardT = 7f;
        }

        void ShowInfo(PieceView v)
        {
            var piece = v.Piece;
            string title, level, body;
            float range = 0;
            var style = RangeStyle.Unit;
            Vector2 center = v.Root.localPosition;
            Color seal = Pal.Tile;
            bool onBoard = TryFindBoardPos(piece, out int pc, out int pr);
            int orphanNear = onBoard ? M.Player.OrphanBoostersNear(pc, pr) : 0;
            if (piece.Type == PieceType.Unit)
            {
                var d = UnitDef.Get(piece.Kind);
                title = d.Glyph + "兵";
                level = $"Lv.{piece.Level}";
                range = d.RangeWith(M.Player.Items.Far);
                string orphanLine = orphanNear > 0 ? $"受周围 {orphanNear} 枚孤字助威：出手速度提升" : "";
                body = Lines($"类型：{d.TypeLabel}", $"射程：{range:0.#}格", $"攻击力：{d.DamageAt(piece.Level):0.#}", $"攻速：{1f / d.CooldownAt(piece.Level):0.0}次/秒", $"满级{UnitDef.MaxLevel}级", d.Role, orphanLine);
            }
            else if (piece.Type == PieceType.Stove)
            {
                int lv = Mathf.Clamp(piece.Level, 1, Tuning.StoveMaxLevel);
                int cap = Side.StoveCapAt(lv);
                int made = M.Player.StoveMade;
                title = "十字坡灶";
                level = $"Lv.{lv}";
                seal = Pal.Hex("F6D3A8");
                string state = made >= cap ? "这一局的面用完了" : $"这一局已出 {made}/{cap} 个";
                string up = lv >= Tuning.StoveMaxLevel ? "火已经最旺了" : $"天书升 {lv + 1} 级：{Tuning.StoveGap[lv]:0.#} 秒出 {Tuning.StoveYield[lv]} 个，最多 {Side.StoveCapAt(lv + 1)} 个";
                body = Lines($"每 {Tuning.StoveGap[lv - 1]:0.#} 秒出 {Tuning.StoveYield[lv - 1]} 个炊饼", state, up, "能在场上挪，不能收回山寨");
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
                var gearDef = M.Player.GearOf(d);
                var aff = gearDef == null ? WeaponAffix.Bare : Weapons.Affix(gearDef, d);
                int wlv = gearDef == null ? 1 : M.Player.GearRank(d);
                float wmul = M.Player.GearMul(d);
                float bond = M.Player.BondBonus(d.Id);
                float water = h.OnWater ? d.WaterBonus * aff.Water
                    : (d.WaterBonus > 1f && M.Player.HasBondTrait(d.Id, BondTrait.WaveWater) ? 1f + (d.WaterBonus - 1f) * 0.4f : 1f);
                float power = d.DamageAt(h.Level) * water * wmul * aff.Dmg * (1f + bond);
                float cd = d.CooldownAt(h.Level) * (aff.Cd <= 0f ? 1f : aff.Cd) * (M.Player.HasBondTrait(d.Id, BondTrait.SwornGuard) ? 0.85f : 1f);
                string xp = h.Level >= UnitDef.MaxLevel ? "已满级（仍享攻速符加成）" : $"经验值 {Mathf.FloorToInt(h.Xp)}/{HeroInstance.XpNeed(h.Level)}";
                string xpCap = h.Level >= UnitDef.MaxLevel ? "" : $"满级{UnitDef.MaxLevel}级";
                string atk = d.NoAttack ? "不出手" : $"攻击力：{power:0.#}";
                string spd = d.NoAttack ? "" : $"攻速：{1f / cd:0.0}次/秒";
                string ult = d.NoAttack ? "" : $"绝技：{M.Player.UltMax(h):0.#}秒";
                string gearName = "", gearGrade = "", gearFrom = "", gearDmg = "";
                if (gearDef != null)
                {
                    int pct = Mathf.RoundToInt((wmul - 1f) * 100f);
                    gearName = "兵器 " + gearDef.Weapon;
                    gearGrade = $"{Weapons.Grades[Mathf.Clamp(gearDef.WGrade, 0, 3)]} {wlv}阶";
                    gearFrom = gearDef.Id == d.Id ? "原主" : "出自" + gearDef.Name;
                    gearDmg = $"伤害 +{pct}%";
                }
                string bonds = "";
                var traitNames = new List<string>();
                foreach (var bd in M.Player.ActiveBonds)
                    if (System.Array.IndexOf(bd.Members, d.Id) >= 0)
                    {
                        bonds += (bonds.Length > 0 ? "、" : "") + bd.Name;
                        string tn = Bond.TraitName(bd.Trait);
                        if (!traitNames.Contains(tn)) traitNames.Add(tn);
                    }
                if (bonds.Length > 0)
                    bonds = $"羁绊 {bonds}　伤+{Mathf.RoundToInt(bond * 100f)}%【{string.Join("/", traitNames)}】";
                string star = d.Rank > 0 ? $"{d.Star}·{d.Nick}" : "隐藏好汉";
                string affix = gearDef == null ? "" : Weapons.AffixText(gearDef.WKind);
                var move = Weapons.MoveOf(gearDef);
                string moveLine = move == WeaponMove.None ? "" : $"招式「{Weapons.MoveName(move)}」{Weapons.MoveText(move)}";
                body = Lines(star, $"类型：{d.TypeLabel}", atk, spd, ult, xp, xpCap, gearName, gearGrade, gearFrom, gearDmg, affix, moveLine, d.Skill, bonds);
                range = M.Player.ShownRange(d);
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
                string auraLine = onBoard
                    ? "孤字助威（生效中）：周围八格友军攻速 +6%"
                    : "孤字助威：摆上场可令周围八格友军攻速 +6%";
                body = Lines(auraLine, "可组成：" + string.Join("、", names), "按名字顺序左→右或上→下紧挨成将", "可用「萧让代笔」补齐缺字");
            }

            _cardKey = "piece";
            PlaceCard(title, level, body, piece.Glyph.ToString(), seal, piece.Type == PieceType.Stove ? Art.Img("it_stove") : null);

            SetPicked(v);
            if (range > 0f && piece.Type != PieceType.Shovel) ShowRange(center, range, style, false);
            else HideRange();
            v.Punch(0.15f);
        }

        /// <summary>纸签宽。正文从印鉴右侧排到右内边，汉字按一格、数字按六成宽。</summary>
        const float CardW = 7.56f;
        const float CardBody0 = 0.22f;
        const float CardSealS = 0.68f;

        static float CardTextLeft => -CardW * 0.5f + 0.18f + CardSealS + 0.16f;
        static float CardTextRight => CardW * 0.5f - 0.20f;

        /// <summary>纸签顶到屏幕底边还剩多高。字再多也得收在这条里面。</summary>
        float CardRoom()
        {
            if (_cam == null) return 2.05f;
            float bottom = _cam.transform.position.y - _cam.orthographicSize;
            float top = CampY - 0.66f;
            return Mathf.Max(1.45f, top - bottom - 0.06f);
        }

        /// <summary>纸签钉在山寨下沿。原点在纸签顶边中点，只往下长。短句往右接，接不下再换行；仍超高就把字号收一档，保证最后一行落在框内。</summary>
        void PlaceCard(string title, string level, string body, string glyph, Color seal, Sprite icon)
        {
            float textL = CardTextLeft;
            float room = CardRoom();
            float size = CardBody0;
            string shown = body ?? "";
            float h = 1.28f;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                float em = (CardTextRight - textL) / size;
                shown = PackEm(body, em);
                int lines = string.IsNullOrEmpty(shown) ? 0 : 1;
                foreach (var ch in shown) if (ch == '\n') lines++;
                float pitch = size * 1.22f;
                h = 0.62f + Mathf.Max(0, lines - 1) * pitch + (lines > 0 ? size : 0f) + 0.16f;
                if (h < 1.28f) h = 1.28f;
                if (h <= room + 0.01f || size <= 0.16f) break;
                size = Mathf.Max(0.16f, size * room / h * 0.96f);
            }
            if (h > room) h = room;

            _card.gameObject.SetActive(true);
            _card.localPosition = new Vector2(0f, CampY - 0.66f);
            _cardSize = new Vector2(CardW, h);
            float cy = -h * 0.5f;
            _cardShadow.transform.localPosition = new Vector2(0.04f, cy - 0.05f);
            _cardShadow.size = _cardSize;
            _cardBg.transform.localPosition = new Vector2(0f, cy);
            _cardBg.size = _cardSize;
            _cardPaper.transform.localPosition = new Vector2(0f, cy);
            _cardPaper.size = _cardSize - new Vector2(0.1f, 0.1f);

            float sealX = -CardW * 0.5f + 0.18f + CardSealS * 0.5f;
            float sealY = -0.18f - CardSealS * 0.5f;
            _cardSeal.transform.localPosition = new Vector2(sealX, sealY);
            _cardSeal.size = new Vector2(CardSealS, CardSealS);
            _cardSeal.color = seal;
            _cardGlyph.text = icon == null ? glyph : "";
            _cardGlyph.transform.localPosition = new Vector2(sealX, sealY - 0.02f);
            _cardIcon.enabled = icon != null;
            if (icon != null)
            {
                _cardIcon.sprite = icon;
                _cardIcon.transform.localPosition = new Vector2(sealX, sealY);
                _cardIcon.transform.localScale = Vector3.one * (CardSealS * 0.86f);
            }

            _cardTitle.text = title;
            _cardTitle.transform.localPosition = new Vector2(textL, -0.32f);
            _cardLevel.text = level;
            _cardLevel.transform.localPosition = new Vector2(CardW * 0.5f - 0.2f, -0.32f);
            float ruleW = CardW * 0.5f - 0.2f - textL;
            _cardRule.transform.localPosition = new Vector2(textL + ruleW * 0.5f, -0.54f);
            _cardRule.transform.localScale = new Vector3(Mathf.Max(0.4f, ruleW), 0.04f, 1f);
            Gfx.SetHeight(_cardBody, size);
            _cardBody.text = shown;
            _cardBody.transform.localPosition = new Vector2(textL, -0.60f);
            _cardT = 4f;
            _cardFrame = _frame;
            Tween.Pop(_card, 0.18f);
        }

        /// <summary>领到兵器时那一行说明：伤害加多少、兵器自带的那一条。</summary>
        string GearGain(int side, int heroInstance, HeroDef weapon)
        {
            HeroDef wearer = null;
            foreach (var h in M.Sides[side].Heroes) if (h.Id == heroInstance) { wearer = h.Def; break; }
            int pct = wearer == null ? 0 : Mathf.RoundToInt((M.Sides[side].GearMul(wearer) - 1f) * 100f);
            string affix = Weapons.AffixText(weapon.WKind);
            int colon = affix.IndexOf('：');
            if (colon >= 0) affix = affix.Substring(colon + 1);
            return pct > 0 ? $"伤害 +{pct}%　{affix}" : affix;
        }

        static string GearBadge(HeroDef weapon)
        {
            var name = weapon.Weapon;
            if (string.IsNullOrEmpty(name) || name.Length <= 4) return name ?? "";
            return name.Substring(name.Length - 2);
        }

        /// <summary>几段说明先用换行隔开，真正排进纸签时再按宽度接成行。</summary>
        static string Lines(params string[] parts)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(part);
            }
            return sb.ToString();
        }

        /// <summary>汉字算 1，英文数字算 0.62。一行还有空就接下一句，接不下整句换行；一句比行还长才从中间断开。</summary>
        static string PackEm(string body, float width)
        {
            if (string.IsNullOrEmpty(body)) return "";
            width = Mathf.Max(6f, width);
            var sb = new System.Text.StringBuilder();
            float col = 0f;
            foreach (var raw in body.Split('\n'))
            {
                if (raw.Length == 0) continue;
                var segs = new List<string>();
                int i = 0;
                while (i < raw.Length)
                {
                    float used = 0f;
                    int j = i;
                    while (j < raw.Length)
                    {
                        float w = raw[j] < 128 ? 0.62f : 1f;
                        if (used > 0.01f && used + w > width + 0.05f) break;
                        used += w;
                        j++;
                    }
                    if (j == i) j = i + 1;
                    segs.Add(raw.Substring(i, j - i));
                    i = j;
                }
                foreach (var seg in segs)
                {
                    float need = 0f;
                    foreach (var ch in seg) need += ch < 128 ? 0.62f : 1f;
                    if (col > 0.01f && col + 1f + need > width + 0.05f)
                    {
                        sb.Append('\n');
                        col = 0f;
                    }
                    if (col > 0.01f)
                    {
                        sb.Append('　');
                        col += 1f;
                    }
                    sb.Append(seg);
                    col += need;
                }
            }
            return sb.ToString();
        }

        /// <summary>窄气泡按字数折行，超行数的丢掉。</summary>
        static string WrapChars(string body, int width, int maxLines)
        {
            if (string.IsNullOrEmpty(body)) return "";
            var sb = new System.Text.StringBuilder();
            int col = 0, lines = 0;
            foreach (var ch in body)
            {
                if (ch == '\n' || col >= width)
                {
                    if (lines + 1 >= maxLines) break;
                    if (sb.Length > 0 && sb[sb.Length - 1] != '\n') sb.Append('\n');
                    lines++;
                    col = 0;
                    if (ch == '\n') continue;
                }
                sb.Append(ch);
                col++;
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

        bool CardShowing(string key) => _card != null && _card.gameObject.activeSelf && _cardKey == key;

        void HideCard()
        {
            _card.gameObject.SetActive(false);
            if (!_rangeFromDrag) HideRange();
            SetPicked(null);
            _cardT = 0;
            _cardKey = null;
        }

        void UpdateCard(float dt)
        {
            if (_card == null || !_card.gameObject.activeSelf) return;
            _cardT -= dt;
            if (_cardT <= 0 || (Input.GetMouseButtonDown(0) && _frame != _cardFrame)) HideCard();
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

        public Vector2 RecruitButtonPos => _recruitBtn.transform.position;
        public Vector2 RescueWorld => _rescueRoot.position;
        public bool Dragging => _drag != null;

        /// <summary>世界坐标换成战斗浮层上的锚点，和贴士同一套换算。</summary>
        public bool OverlayOf(RectTransform overlay, Vector2 world, out Vector2 local)
        {
            var screen = (Vector2)_cam.WorldToScreenPoint(world);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out local);
        }

        /// <summary>军师贴士的上沿：HUD 下面、对手半区顶上，换成屏幕像素。</summary>
        public Vector2 HintScreenTop => _cam.WorldToScreenPoint(new Vector3(0f, TopY - HudHalf - 0.1f, 0f));

        public bool TryFindEmptyCell(Vector2 near, out Vector2 pos)
        {
            var b = M.Player.Board;
            float best = float.MaxValue;
            bool found = false;
            pos = default;
            for (int r = 0; r < Board.Rows; r++)
                for (int c = 0; c < Board.Cols; c++)
                {
                    if (!b.Placeable(c, r) || b.Pieces[c, r] != null) continue;
                    var p = CellWorld(0, c, r);
                    float d = (p - near).sqrMagnitude;
                    if (d >= best) continue;
                    best = d;
                    pos = p;
                    found = true;
                }
            return found;
        }

        public bool TryFindEmptyPair(out Vector2 leftPos, out Vector2 rightPos)
        {
            var b = M.Player.Board;
            // 优先挑靠上路边、横向相邻的两块空石板（如 (2,3)-(3,3)），直观演示左→右连读成名。
            for (int r = Board.Rows - 1; r >= 0; r--)
                for (int c = 0; c < Board.Cols - 1; c++)
                {
                    if (!b.Placeable(c, r) || !b.Placeable(c + 1, r)) continue;
                    if (b.Pieces[c, r] != null || b.Pieces[c + 1, r] != null) continue;
                    int pickC = c == 1 && c + 2 < Board.Cols && b.Placeable(c + 2, r) && b.Pieces[c + 2, r] == null ? c + 1 : c;
                    leftPos = CellWorld(0, pickC, r);
                    rightPos = CellWorld(0, pickC + 1, r);
                    return true;
                }
            for (int c = 0; c < Board.Cols; c++)
                for (int r = Board.Rows - 1; r >= 1; r--)
                {
                    if (!b.Placeable(c, r) || !b.Placeable(c, r - 1)) continue;
                    if (b.Pieces[c, r] != null || b.Pieces[c, r - 1] != null) continue;
                    leftPos = CellWorld(0, c, r);
                    rightPos = CellWorld(0, c, r - 1);
                    return true;
                }
            leftPos = rightPos = default;
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
            /// <summary>一级兵从头到脚的高度，脚踩在底座上。</summary>
            const float UnitFigH = 0.9f, FootY = -0.4f;
            readonly SpriteRenderer _tile, _shadow, _edge, _icon;
            readonly TextMesh _glyph, _level;
            readonly Transform _glyphT;
            /// <summary>有人物图时：兵是小人，好汉字写在举牌小兵的牌面上，脚下一圈按等级上色的底座。</summary>
            readonly SpriteRenderer _body, _lvBg, _lvRim, _rim, _aura;
            readonly Transform _bodyT;
            readonly bool _figure;
            /// <summary>兵种有分档动作帧：站、蓄力、出手三张，换档时整组换。</summary>
            readonly bool _frameSet;
            readonly Sprite[] _frames = new Sprite[3];
            readonly float _bodyH, _bodyY;
            float _face = 1f;
            const float PlacardY = 0.186f, PlacardGlyph = 0.44f;
            Color _glyphColor = Pal.Ink;
            float _animT, _animDur = AnimDur, _animDist;
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
                _frameSet = p.Type == PieceType.Unit && Art.UnitFrame(p.Kind, p.Level, 0) != null;
                var body = _frameSet ? Art.UnitFrame(p.Kind, p.Level, 0) : p.Type == PieceType.HeroChar ? Art.Opt("pc_placard") : null;
                _figure = body != null;
                if (_figure)
                {
                    bool placard = p.Type == PieceType.HeroChar;
                    if (_frameSet)
                    {
                        _bodyH = UnitFigH / Art.UnitFigure(p.Kind) * (p.Kind == UnitKind.Qi ? 1.12f : 1f);
                        _bodyY = FootY + (Art.FigureFoot - 0.5f) * _bodyH;
                    }
                    else
                    {
                        _bodyH = 1f;
                        _bodyY = 0.03f;
                    }
                    _shadow = Gfx.Sprite(Root, Art.SoftDot, Pal.Shadow, new Vector2(0, -0.4f), new Vector2(0.84f, 0.3f), 20);
                    if (_frameSet)
                    {
                        _aura = Gfx.Sprite(Root, Art.SoftDot, Color.clear, new Vector2(0, -0.12f), new Vector2(1.1f, 1.1f), 20);
                        _rim = Gfx.Sprite(Root, Art.Circle, Pal.Ink.A(0.75f), new Vector2(0, -0.39f), new Vector2(0.8f, 0.27f), 21);
                        // 和底座同一层，往后挪一点让底座盖在它上面。
                        _aura.transform.localPosition = new Vector3(0, -0.12f, 0.02f);
                        _rim.transform.localPosition = new Vector3(0, -0.39f, 0.01f);
                    }
                    _tile = Gfx.Sprite(Root, Art.Circle, Pal.Tile, new Vector2(0, -0.38f), new Vector2(0.72f, 0.22f), 21);
                    _edge = Gfx.Sprite(Root, Art.Frame, Color.clear, Vector2.zero, new Vector2(0.92f, 0.92f), 21);
                    _body = Gfx.Sprite(Root, body, Color.white, new Vector2(0, _bodyY), Vector2.one * _bodyH, 22);
                    _bodyT = _body.transform;
                    _glyph = Gfx.Text(_bodyT, placard ? p.Glyph.ToString() : "", Fonts.Brush, PlacardGlyph, Pal.RedDeep, new Vector2(0, PlacardY), 23);
                    _glyphT = _glyph.transform;
                    var badge = _frameSet ? new Vector2(0.32f, -0.3f) : new Vector2(0.33f, -0.33f);
                    float bs = _frameSet ? 0.34f : 0.27f;
                    _lvRim = Gfx.Sprite(Root, Art.Circle, Pal.Ink, badge, Vector2.one * (bs + 0.05f), 24);
                    _lvBg = Gfx.Sprite(Root, Art.Circle, Pal.Ink, badge, Vector2.one * bs, 25);
                    _level = Gfx.Text(Root, "", _frameSet ? Fonts.Brush : Fonts.Kai, _frameSet ? 0.3f : 0.22f, Pal.Paper, badge + new Vector2(0, -0.005f), 26);
                    if (!_frameSet) _lvRim.enabled = false;
                }
                else
                {
                    _shadow = Gfx.Sprite(Root, Art.Rounded, Pal.Shadow, new Vector2(0.04f, -0.07f), new Vector2(0.92f, 0.92f), 20);
                    _tile = Gfx.Sprite(Root, Art.Rounded, Pal.Tile, Vector2.zero, new Vector2(0.92f, 0.92f), 21);
                    _edge = Gfx.Sprite(Root, Art.Frame, Pal.Ink.A(0.22f), Vector2.zero, new Vector2(0.92f, 0.92f), 21);
                    bool iconOnly = p.Type == PieceType.Shovel || p.Type == PieceType.Stove;
                    _glyph = Gfx.Text(Root, iconOnly ? "" : p.Glyph.ToString(), Fonts.Brush, 0.74f, Pal.Ink, new Vector2(0, -0.03f), 22);
                    _glyphT = _glyph.transform;
                    _level = Gfx.Text(Root, "", Fonts.Kai, 0.25f, Pal.InkSoft, new Vector2(0.31f, 0.31f), 23);
                }
                if (p.Type == PieceType.Shovel)
                    _icon = Gfx.Sprite(Root, Art.Img("shovel"), Color.white, Vector2.zero, Vector2.one * 0.72f, 22);
                if (p.Type == PieceType.Stove)
                    _icon = Gfx.Sprite(Root, Art.Img("it_stove"), Color.white, new Vector2(0, -0.02f), Vector2.one * 0.8f, 22);
                Refresh();
            }

            /// <summary>
            /// 各兵种一次出手的时长和落点（占时长的比例）。刀举刀再劈，枪后撤再刺，弓拉满再放，骑扬蹄再冲。
            /// 攻速快时整段压短，不会上一刀没收回来下一刀又起。
            /// </summary>
            public static float StrikeDur(UnitKind k, int level)
            {
                float base_ = k switch
                {
                    UnitKind.Dao => 0.42f,
                    UnitKind.Qiang => 0.4f,
                    UnitKind.Gong => 0.46f,
                    _ => 0.5f,
                };
                return Mathf.Min(base_, UnitDef.Get(k).CooldownAt(level) * 0.88f);
            }

            static float StrikeAt(UnitKind k) => k switch
            {
                UnitKind.Dao => 0.34f,
                UnitKind.Qiang => 0.36f,
                UnitKind.Gong => 0.46f,
                _ => 0.52f,
            };

            /// <summary>出手那一下离起手多久。特效、伤害数字在这时候出。</summary>
            public static float HitDelay(UnitKind k, int level) => StrikeDur(k, level) * StrikeAt(k);

            /// <summary>
            /// 动作帧和身位：k 从 0 到 1。刀劈带前冲和前倾；枪先缩后猛刺，刺出去最远；
            /// 弓几乎不动，放箭时往后一震；骑先扬起前蹄，再整个冲到敌人跟前撞一下退回。
            /// </summary>
            void Pose(float k, out int frame, out Vector2 off, out float rot, out float sx, out float sy)
            {
                var d = _animDir;
                float hit = StrikeAt(_animKind);
                sx = sy = 1f;
                rot = 0f;
                off = Vector2.zero;
                frame = 0;
                if (k < hit)
                {
                    float e = Tween.OutCubic(k / hit);
                    frame = 1;
                    switch (_animKind)
                    {
                        case UnitKind.Dao: off = -d * 0.05f * e; rot = 7f * e; sy = 1f + 0.05f * e; break;
                        case UnitKind.Qiang: off = -d * 0.11f * e; sx = 1f + 0.05f * e; sy = 1f - 0.04f * e; break;
                        case UnitKind.Gong: off = -d * 0.03f * e; break;
                        default: off = Vector2.up * 0.07f * e - d * 0.04f * e; rot = 12f * e; break;
                    }
                    return;
                }
                float t = (k - hit) / (1f - hit);
                frame = t < 0.62f ? 2 : 0;
                switch (_animKind)
                {
                    case UnitKind.Dao:
                    {
                        float p = t < 0.25f ? Tween.OutCubic(t / 0.25f) : 1f - Tween.OutCubic((t - 0.25f) / 0.75f);
                        off = d * 0.2f * p;
                        rot = -11f * p;
                        sx = 1f + 0.06f * p;
                        break;
                    }
                    case UnitKind.Qiang:
                    {
                        float p = t < 0.2f ? Tween.OutCubic(t / 0.2f) : 1f - Tween.OutCubic((t - 0.2f) / 0.8f);
                        off = d * 0.34f * p;
                        sx = 1f + 0.1f * p;
                        sy = 1f - 0.05f * p;
                        break;
                    }
                    case UnitKind.Gong:
                    {
                        float p = 1f - Tween.OutCubic(t);
                        off = -d * 0.06f * p;
                        sx = 1f - 0.04f * p;
                        break;
                    }
                    default:
                    {
                        float reach = Mathf.Clamp(_animDist - 0.4f, 0.25f, 1.05f);
                        float p = t < 0.3f ? Tween.OutCubic(t / 0.3f) : 1f - Tween.OutCubic((t - 0.3f) / 0.7f);
                        off = d * reach * p + Vector2.up * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * 0.05f * p;
                        rot = -6f * p;
                        frame = t < 0.75f ? 2 : 0;
                        break;
                    }
                }
            }

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
                if (Piece.Type == PieceType.Stove)
                {
                    var warm = Piece.Level >= Tuning.StoveMaxLevel ? Pal.Hex("E8823A") : Pal.Hex("F2B06A");
                    _tile.color = Color.Lerp(Pal.Tile, warm, 0.25f + 0.12f * (Piece.Level - 1));
                    _edge.color = Pal.Hex("C2552B").A(0.45f + 0.2f * (Piece.Level - 1));
                    _level.text = Piece.Level.ToString();
                    _level.color = Pal.RedDeep;
                    return;
                }
                if (_figure)
                {
                    RefreshFigure();
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

            void RefreshFigure()
            {
                bool hero = Piece.Type == PieceType.HeroChar;
                // 凑成了好汉、也画了这位好汉：金框里站好汉本人，这几块字牌收起来。
                bool hidden = hero && Hero != null && Art.HeroFrame(Hero.Def.Id, 0) != null;
                _body.enabled = _tile.enabled = _shadow.enabled = _lvBg.enabled = !hidden;
                _glyph.gameObject.SetActive(!hidden);
                _level.text = hidden ? "" : Piece.Level.ToString();
                if (_frameSet)
                {
                    for (int i = 0; i < 3; i++) _frames[i] = Art.UnitFrame(Piece.Kind, Piece.Level, i) ?? _frames[0];
                    _body.sprite = _frames[0];
                    int lvl = Mathf.Clamp(Piece.Level, 1, UnitDef.MaxLevel);
                    var metal = Pal.Metal(lvl);
                    _tile.color = metal;
                    _rim.color = Color.Lerp(metal, Pal.Ink, 0.55f);
                    _lvBg.color = lvl >= UnitDef.MaxLevel ? Pal.Hex("C0392B") : lvl == 1 ? Pal.InkSoft : Color.Lerp(metal, Pal.Ink, 0.25f);
                    _lvRim.color = lvl >= 4 ? Pal.GoldBright : Pal.Paper;
                    _level.color = lvl >= UnitDef.MaxLevel ? Pal.Hex("FFE36A") : Pal.Paper;
                    _glyphColor = Pal.RedDeep;
                    return;
                }
                var lv = hero ? Pal.Gold : Pal.ForLevel(Piece.Level);
                _tile.color = Color.Lerp(Pal.Tile, lv, hero || Piece.Level > 1 ? 0.7f : 0.35f);
                _lvBg.color = lv;
                _level.color = Pal.Paper;
                _glyphColor = Pal.RedDeep;
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

            SpriteRenderer _chainA, _chainB, _stunBg;
            TextMesh _stunMark;
            bool _stunned;

            /// <summary>被头目定身：两道铁链交叉锁住，角上一枚红「定」字，字发灰。</summary>
            public void SetStun(bool on)
            {
                if (on == _stunned) return;
                _stunned = on;
                if (on && _chainA == null)
                {
                    _chainA = Gfx.Sprite(Root, Art.Stroke, Pal.Ink.A(0.9f), Vector2.zero, new Vector2(1.05f, 0.14f), 25);
                    _chainA.transform.localRotation = Quaternion.Euler(0, 0, 40f);
                    _chainB = Gfx.Sprite(Root, Art.Stroke, Pal.Ink.A(0.9f), Vector2.zero, new Vector2(1.05f, 0.14f), 25);
                    _chainB.transform.localRotation = Quaternion.Euler(0, 0, -40f);
                    _stunBg = Gfx.Sprite(Root, Art.Rounded, Pal.Red, new Vector2(-0.3f, 0.3f), new Vector2(0.34f, 0.34f), 26);
                    _stunMark = Gfx.Text(Root, "定", Fonts.Brush, 0.26f, Pal.Paper, new Vector2(-0.3f, 0.3f), 27);
                }
                if (_chainA == null) return;
                _chainA.enabled = _chainB.enabled = _stunBg.enabled = on;
                _stunMark.gameObject.SetActive(on);
                if (on) _punch = Mathf.Max(_punch, 0.35f);
            }

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

            public void Attack(UnitKind kind, Vector2 dir, float dist)
            {
                _animT = 1f;
                _animKind = kind;
                _animDir = dir.sqrMagnitude > 0.001f ? dir : Vector2.up;
                _animDist = dist;
                _animDur = _frameSet ? StrikeDur(kind, Piece.Level) : AnimDur;
                _lungeReach = 0f;
                if (Mathf.Abs(_animDir.x) > 0.15f) _face = _animDir.x < 0 ? -1f : 1f;
            }

            float _lungeReach;

            /// <summary>好汉出手：名字里的字朝目标探出去，不换成兵器图。</summary>
            public void Lunge(Vector2 dir)
            {
                _animT = 1f;
                _animKind = UnitKind.Qi;
                _animDir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.up;
                _animDur = AnimDur;
                _lungeReach = 0.34f;
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
                if (_body != null)
                {
                    _body.sortingOrder = o + 2;
                    _glyph.GetComponent<MeshRenderer>().sortingOrder = o + 3;
                    _lvRim.sortingOrder = o + 4;
                    _lvBg.sortingOrder = o + 5;
                    _level.GetComponent<MeshRenderer>().sortingOrder = o + 6;
                    if (_rim != null) _rim.sortingOrder = o + 1;
                    if (_aura != null) _aura.sortingOrder = o;
                }
                else _level.GetComponent<MeshRenderer>().sortingOrder = o + 3;
                if (_chainA != null)
                {
                    _chainA.sortingOrder = _chainB.sortingOrder = o + 5;
                    _stunBg.sortingOrder = o + 6;
                    _stunMark.GetComponent<MeshRenderer>().sortingOrder = o + 7;
                }
                Root.localScale = Vector3.one * (on ? 1.12f : 1f);
                _shadow.transform.localPosition = _figure ? new Vector2(on ? 0.08f : 0f, on ? -0.46f : -0.4f)
                    : on ? new Vector2(0.1f, -0.18f) : new Vector2(0.04f, -0.07f);
            }

            public void Tick(float dt, bool dragging)
            {
                int key = Piece.Level * 10 + (Hero == null ? 0 : Art.HeroFrame(Hero.Def.Id, 0) != null ? 2 : 1);
                if (_lastLevel != key)
                {
                    _lastLevel = key;
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
                float sx = 1, sy = 1, rot = 0;
                int frame = 0;
                if (_animT > 0 && _frameSet)
                {
                    if (dragging) _animT = 0f;
                    else
                    {
                        _animT = Mathf.Max(0, _animT - dt / _animDur);
                        Pose(1f - _animT, out frame, out _lungeOff, out rot, out sx, out sy);
                    }
                }
                else if (_animT > 0)
                {
                    float lk = 1f - _animT;
                    if (!dragging)
                    {
                        float reach = _lungeReach > 0f ? _lungeReach : 0.14f;
                        _lungeOff = _animDir * reach * Mathf.Sin(Mathf.Min(1f, lk * 1.6f) * Mathf.PI);
                    }
                    _animT = Mathf.Max(0, _animT - dt / _animDur);
                    if (_animT <= 0f) _lungeReach = 0f;
                    float q = Mathf.Sin(lk * Mathf.PI);
                    sx += q * 0.18f;
                    sy -= q * 0.12f;
                }
                Root.localPosition = cur + _lungeOff;

                _punch = Mathf.Max(0, _punch - dt * 1.5f);
                _popK = Mathf.Min(1f, _popK + dt / 0.3f);
                float pop = _popK < 1f ? Tween.OutBack(_popK) : 1f;
                Root.localScale = Vector3.one * ((_dragging ? 1.12f : 1f) * (1f + Mathf.Sin(_punch * 20f) * _punch * 0.5f) * pop);

                if (Piece.Type == PieceType.HeroChar && Hero == null && !_dragging)
                {
                    float breathe = Mathf.Sin(Time.time * 2f + Piece.Id) * 0.04f;
                    sx += breathe;
                    sy += breathe;
                }
                if (_figure)
                {
                    TickFigure(sx, sy, rot, frame);
                    return;
                }
                _glyphT.localPosition = new Vector2(0, -0.02f);
                _glyphT.localScale = new Vector3(sx, sy, 1);
                _glyph.color = _glyphColor;
                if (_stunned)
                {
                    _glyph.color = Color.Lerp(_glyphColor, Pal.InkSoft, 0.65f);
                    _glyphT.localPosition = new Vector2(Mathf.Sin(Time.time * 47f) * 0.025f, -0.02f);
                    float a = 0.75f + 0.25f * Mathf.Sin(Time.time * 6f);
                    _chainA.color = _chainB.color = Pal.Ink.A(0.9f * a);
                }
                if (_pickedPiece)
                {
                    _edge.color = Pal.GoldBright;
                    _edge.size = new Vector2(1.02f, 1.02f);
                }
            }

            /// <summary>
            /// 小人原地轻轻起伏。有动作帧的兵出手时换蓄力、出手两张，身子绕脚底转，前冲后撤跟着兵种走；
            /// 牌面的字跟着人走，翻面时字不反。
            /// </summary>
            void TickFigure(float sx, float sy, float rot, int frame)
            {
                bool busy = _animT > 0f;
                float idle = _dragging || busy ? 0f : Mathf.Sin(Time.time * 3f + Piece.Id * 1.7f) * 0.025f;
                if (_frameSet)
                {
                    _body.sprite = _frames[frame] ?? _frames[0];
                    _body.sortingOrder = (_dragging ? 60 : _baseOrder) + 2 + (busy && _animKind == UnitKind.Qi ? 30 : 0);
                    // 绕脚底转：先把脚底挪到原点，转完再挪回去。
                    var foot = new Vector2(0f, FootY);
                    var q = Quaternion.Euler(0, 0, rot * _face);
                    var center = new Vector2(0f, (_bodyY - FootY) * (sy + idle));
                    _bodyT.localRotation = q;
                    _bodyT.localPosition = foot + (Vector2)(q * center);
                    _bodyT.localScale = new Vector3(_bodyH * sx * _face, _bodyH * (sy + idle), 1f);
                    TickAura();
                }
                else
                {
                    _bodyT.localScale = new Vector3(_bodyH * sx * _face, _bodyH * (sy + idle), 1f);
                    _bodyT.localPosition = new Vector2(0f, _bodyY + idle * 0.4f);
                }
                _glyphT.localScale = new Vector3(_face, 1f, 1f);
                var tint = Color.white;
                if (_stunned)
                {
                    tint = Color.Lerp(Color.white, Pal.Hex("8A9BB0"), 0.6f);
                    _bodyT.localPosition += new Vector3(Mathf.Sin(Time.time * 47f) * 0.025f, 0f, 0f);
                    float a = 0.75f + 0.25f * Mathf.Sin(Time.time * 6f);
                    _chainA.color = _chainB.color = Pal.Ink.A(0.9f * a);
                }
                _body.color = tint;
                _glyph.color = _stunned ? Color.Lerp(_glyphColor, Pal.InkSoft, 0.65f) : _glyphColor;
                if (_pickedPiece)
                {
                    _edge.color = Pal.GoldBright;
                    _edge.size = new Vector2(1.02f, 1.02f);
                }
                else _edge.color = Color.clear;
            }

            /// <summary>四级底座泛一圈金光，五级金光一明一暗地呼吸，隔着半个棋盘也认得出。</summary>
            void TickAura()
            {
                int lvl = Piece.Level;
                if (lvl < 4 || _dragging)
                {
                    _aura.color = Color.clear;
                    return;
                }
                float pulse = lvl >= UnitDef.MaxLevel ? 0.5f + 0.5f * Mathf.Sin(Time.time * 3.2f + Piece.Id) : 0.5f;
                float a = lvl >= UnitDef.MaxLevel ? 0.42f + 0.22f * pulse : 0.26f;
                _aura.color = Pal.Hex(lvl >= UnitDef.MaxLevel ? "FFB43A" : "F0C23C").A(a);
                float s = lvl >= UnitDef.MaxLevel ? 1.15f + 0.1f * pulse : 1f;
                _aura.transform.localScale = new Vector3(s, s * 0.9f, 1f);
            }

            public void FadeOutAndDestroy()
            {
                var root = Root;
                Tween.Run(root, 0.18f, k => { if (root) root.localScale = Vector3.one * (1 - k); }, () => { if (root) UnityEngine.Object.Destroy(root.gameObject); });
            }
        }

        sealed class FrameView
        {
            readonly SpriteRenderer _frame, _glow, _xpBg, _xpFg;
            readonly TextMesh _nick, _seal;
            SpriteRenderer _armBg, _armRim, _armIcon;
            SpriteMask _clip;
            /// <summary>漆底上的姓氏大字和人背后的一团金光，出手时金光跟着亮。</summary>
            SpriteRenderer _halo, _lvRim, _lvBg;
            TextMesh _mark, _lv;
            bool _ultFull;
            HeroDef _gear;
            readonly BattleView _v;
            readonly SpriteRenderer _sealBg;
            readonly Transform _root;
            readonly Color _tint, _tintBright;
            public int Seen;
            float _pulse, _glowT;
            float _barW;
            /// <summary>大招蓄力沿金框走：从框底正中往两边爬，左右在框顶会合就满。</summary>
            LineRenderer _trace;
            SpriteRenderer _tipL, _tipR, _glint;
            readonly List<Vector3> _path = new List<Vector3>();
            readonly List<float> _cum = new List<float>();
            readonly List<Vector3> _pts = new List<Vector3>();
            Vector2 _pathSize, _size;
            float _lastK, _burstT;
            Gradient _grad;
            /// <summary>画了这位好汉时：框里站好汉本人，框底一块红名牌。</summary>
            readonly SpriteRenderer _back, _body, _plate;
            readonly Sprite[] _art;
            Vector2 _bodyAt, _lungeDir;
            float _bodyH, _lungeT = -1f, _face = 1f;
            HeroSwing _swing;
            bool _crit;
            /// <summary>图包还没下完，先只摆框和名牌，下完了整个重建。</summary>
            public readonly bool WantsArt;

            /// <summary>三帧出手：蓄力 → 出手 → 收势。出手那一刻在 StrikeK，特效等到那时才放。</summary>
            const float StrikeDur = 0.46f, StrikeK = 0.4f;
            public const float HitDelay = StrikeDur * StrikeK;
            /// <summary>好汉比框高出这么多，头和兵器探出框顶，一眼比小兵大一号。</summary>
            const float Pop = 0.3f;
            /// <summary>框上的排序：漆底 19、大字 20、金光 21、框 22、蓄力线 23、线头 24、人 25、名牌 26、字 27、兵器章 28。</summary>
            const int BodyOrder = 25;

            public FrameView(BattleView v, HeroInstance h, int side, bool quiet = false)
            {
                _root = new GameObject("hero" + h.Id).transform;
                _root.SetParent(side == 0 ? v._low : v._root, false);
                bool disha = h.Def.Disha;
                _tint = disha ? Pal.Purple : Pal.Gold;
                _tintBright = disha ? Pal.Hex("A57BD1") : Pal.GoldBright;
                _glow = Gfx.Sprite(_root, Art.Frame, _tintBright.A(0f), Vector2.zero, Vector2.one, 17);
                _frame = Gfx.Sprite(_root, Art.Frame, _tintBright, Vector2.zero, Vector2.one, 22);
                _nick = Gfx.Text(_root, h.Def.Nick, Fonts.Kai, 0.2f, disha ? Pal.Purple : Pal.RedDeep, Vector2.zero, 25);
                _sealBg = Gfx.Sprite(_root, Art.Rounded, disha ? Pal.Purple : Pal.Red, Vector2.zero, new Vector2(0.26f, 0.26f), 26);
                _seal = Gfx.Text(_root, disha ? "煞" : "罡", Fonts.Brush, 0.2f, Pal.Paper, Vector2.zero, 27);
                // 顶上一条是绝技蓄力，小兵没有，攒满就放大招。
                _xpBg = Gfx.Sprite(_root, Art.White, Pal.Ink.A(0.6f), Vector2.zero, new Vector2(1, 0.09f), 26);
                _xpFg = Gfx.Sprite(_root, Art.White, Pal.Red, Vector2.zero, new Vector2(1, 0.06f), 27);
                var art = Art.HeroFrame(h.Def.Id, 0);
                WantsArt = art == null && ArtPack.Has(Art.HeroPack(h.Def.Id));
                if (WantsArt) ArtPack.Ensure(Art.HeroPack(h.Def.Id));
                if (art != null)
                {
                    _art = new[] { art, Art.HeroFrame(h.Def.Id, 1) ?? art, Art.HeroFrame(h.Def.Id, 2) ?? art };
                    // 深色漆底压住，米色地上的小兵和好汉一眼分开。
                    _back = Gfx.Sprite(_root, Art.Rounded, Pal.Hex(disha ? "33204A" : "4A1A14").A(0.94f), Vector2.zero, Vector2.one, 19);
                    // 人会探出框顶，顶上横条会压脸，蓄力改成沿金框走一圈，线垫在人身后。
                    _xpBg.enabled = _xpFg.enabled = false;
                    var tr = new GameObject("trace");
                    tr.transform.SetParent(_root, false);
                    _trace = tr.AddComponent<LineRenderer>();
                    _trace.useWorldSpace = false;
                    _trace.sharedMaterial = _frame.sharedMaterial;
                    _trace.sortingOrder = 23;
                    _trace.widthMultiplier = 0.075f;
                    _trace.numCapVertices = 3;
                    _trace.numCornerVertices = 2;
                    _trace.positionCount = 0;
                    _tipL = Gfx.Sprite(_root, Art.SoftDot, Color.white.A(0f), Vector2.zero, Vector2.one * 0.26f, 24);
                    _tipR = Gfx.Sprite(_root, Art.SoftDot, Color.white.A(0f), Vector2.zero, Vector2.one * 0.26f, 24);
                    _glint = Gfx.Sprite(_root, Art.SoftDot, Color.white.A(0f), Vector2.zero, Vector2.one * 0.42f, 24);
                    _mark = Gfx.Text(_root, h.Def.Name.Substring(0, 1), Fonts.Brush, 1f, _tintBright.A(0.2f), Vector2.zero, 20);
                    _halo = Gfx.Sprite(_root, Art.SoftDot, _tintBright.A(0.4f), Vector2.zero, Vector2.one, 21);
                    _body = Gfx.Sprite(_root, art, Color.white, Vector2.zero, Vector2.one, BodyOrder);
                    _body.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    // 人从框顶探出一截，两侧和脚下仍按框裁，挥出去的兵器不压到隔壁格子。
                    var clip = new GameObject("clip");
                    clip.transform.SetParent(_root, false);
                    _clip = clip.AddComponent<SpriteMask>();
                    _clip.sprite = Art.White;
                    _clip.isCustomRangeActive = true;
                    _clip.frontSortingOrder = BodyOrder;
                    _clip.backSortingOrder = BodyOrder - 1;
                    _plate = Gfx.Sprite(_root, Art.Rounded, disha ? Pal.Purple : Pal.RedDeep, Vector2.zero, new Vector2(0.86f, 0.3f), 26);
                    _nick.text = h.Def.Name;
                    _nick.font = Fonts.Brush;
                    _nick.GetComponent<MeshRenderer>().sharedMaterial = Fonts.Brush.material;
                    Gfx.SetHeight(_nick, 0.24f);
                    _nick.color = Pal.Paper;
                    _nick.GetComponent<MeshRenderer>().sortingOrder = 27;
                    _lvRim = Gfx.Sprite(_root, Art.Circle, _tintBright, Vector2.zero, Vector2.one * 0.34f, 27);
                    _lvBg = Gfx.Sprite(_root, Art.Circle, Pal.Ink, Vector2.zero, Vector2.one * 0.29f, 28);
                    _lv = Gfx.Text(_root, "", Fonts.Brush, 0.24f, Pal.Paper, Vector2.zero, 29);
                }
                _v = v;
                _side = side;
                SetGear(v.M.Sides[side].GearOf(h.Def));
                Place(h);
                if (quiet) return;
                Tween.Pop(_root, 0.4f);
                _glowT = 1.5f;
            }

            readonly int _side;

            /// <summary>
            /// 兵器架是局里动态发的，原主凑出来会把兵器要回去，角标跟着换。
            /// 只留一枚品级色的兵器章，不写兵器名：加了什么在领到时飘字说，点开好汉看详情。
            /// </summary>
            void SetGear(HeroDef gear)
            {
                _gear = gear;
                if (_armBg) UnityEngine.Object.Destroy(_armBg.gameObject);
                if (_armRim) UnityEngine.Object.Destroy(_armRim.gameObject);
                if (_armIcon) UnityEngine.Object.Destroy(_armIcon.gameObject);
                _armBg = _armRim = _armIcon = null;
                if (gear == null) return;
                _armRim = Gfx.Sprite(_root, Art.Circle, Pal.Ink, Vector2.zero, Vector2.one * 0.42f, 28);
                _armBg = Gfx.Sprite(_root, Art.Circle, Ui.GradeColor(gear.WGrade), Vector2.zero, Vector2.one * 0.36f, 29);
                _armIcon = Gfx.Sprite(_root, Art.Img(Weapons.Icon(gear.WKind)), Color.white, Vector2.zero, Vector2.one * 0.3f, 30);
            }

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
                _size = size;
                _glow.size = size + Vector2.one * (0.3f + 0.45f * _burstT);
                if (_trace != null && size != _pathSize) BuildPath(size);
                _nick.transform.localPosition = new Vector2(0, -size.y * 0.5f + 0.02f);
                if (_body != null)
                {
                    _back.size = size - new Vector2(0.06f, 0.06f);
                    // 帧是方画布，人占 HeroFigure 高、脚踩在画布 FigureFoot 处。人比框高出 Pop，头探出框顶。
                    const float bottom = 0.1f;
                    float fig = Mathf.Min(size.y - 0.14f + Pop, size.x * 1.15f, 2.1f);
                    float clipH = size.y - 0.1f + Pop;
                    _clip.transform.localPosition = new Vector2(0f, Pop * 0.5f);
                    _clip.transform.localScale = new Vector3(size.x - 0.1f, clipH, 1f);
                    _bodyH = fig / Art.HeroFigure;
                    float feet = -size.y * 0.5f + bottom;
                    _bodyAt = new Vector2(0f, feet + (Art.FigureFoot - 0.5f) * _bodyH);
                    float plateY = -size.y * 0.5f + 0.04f;
                    _plate.transform.localPosition = new Vector2(0f, plateY);
                    _nick.transform.localPosition = new Vector2(0f, plateY);
                    float markH = Mathf.Min(size.y, size.x) * 0.92f;
                    Gfx.SetHeight(_mark, markH);
                    _mark.transform.localPosition = new Vector2(0f, 0.02f);
                    _halo.transform.localPosition = new Vector2(0f, feet + fig * 0.45f);
                    _halo.transform.localScale = new Vector3(Mathf.Min(size.x, 1.6f), fig * 1.05f, 1f);
                    // 横着的好汉等级章挨着名牌右端；竖着的框太窄，挪到名牌上面的右角。
                    float lvX = Mathf.Min(0.63f, size.x * 0.5f - 0.17f);
                    var lv = new Vector2(lvX, lvX < 0.6f ? plateY + 0.33f : plateY);
                    _lvRim.transform.localPosition = lv;
                    _lvBg.transform.localPosition = lv;
                    _lv.transform.localPosition = lv + new Vector2(0f, -0.005f);
                }
                _sealBg.transform.localPosition = new Vector2(size.x * 0.5f - 0.1f, size.y * 0.5f - 0.1f);
                _seal.transform.localPosition = _sealBg.transform.localPosition;
                _barW = size.x - 0.5f;
                _xpBg.transform.localPosition = new Vector2(0f, size.y * 0.5f - 0.03f);
                _xpBg.transform.localScale = new Vector3(_barW + 0.04f, 0.1f, 1);
                if (_armBg != null)
                {
                    var p = new Vector2(-size.x * 0.5f + 0.12f, size.y * 0.5f - 0.12f);
                    _armRim.transform.localPosition = p;
                    _armBg.transform.localPosition = p;
                    _armIcon.transform.localPosition = p;
                }
            }

            public void Update(HeroInstance h)
            {
                var gear = _v.M.Sides[_side].GearOf(h.Def);
                if (gear != _gear) SetGear(gear);
                Place(h);
                float dt = Time.deltaTime;
                _pulse = Mathf.Max(0, _pulse - dt * 3.2f);
                _glowT = Mathf.Max(0, _glowT - dt);
                // 绝技蓄力：红色往金色走，攒满时整框发亮。
                float full = Mathf.Max(0.01f, _v.M.Sides[_side].UltMax(h));
                float k = Mathf.Clamp01(1f - h.UltCd / full);
                bool ready = k >= 0.999f;
                if (ready && !_ultFull)
                {
                    _glowT = Mathf.Max(_glowT, 0.8f);
                    if (_trace != null) Tween.Pop(_root, 0.22f);
                }
                if (k < _lastK - 0.3f) _burstT = 1f;
                _lastK = k;
                _ultFull = ready;
                _burstT = Mathf.Max(0f, _burstT - dt * 2.8f);
                float breathe = 0.5f + 0.5f * Mathf.Sin(Time.time * (ready ? 7f : 3f));
                float a = Mathf.Max(_pulse * 0.95f, Mathf.Min(1, _glowT) * 0.8f, _burstT, (ready ? 0.5f : 0.18f) + (ready ? 0.2f : 0.08f) * breathe);
                _glow.color = (_burstT > 0f ? Color.Lerp(_tintBright, Color.white, _burstT * 0.6f) : _tintBright).A(a);
                if (_trace != null) Trace(k, ready, breathe);
                else _frame.color = Color.Lerp(_tint, _tintBright, breathe);
                _xpFg.color = ready ? Color.Lerp(Pal.GoldBright, Color.white, breathe * 0.6f) : Color.Lerp(Pal.Red, Pal.GoldBright, k * k);
                _xpFg.transform.localScale = new Vector3(_barW * k, 0.06f, 1);
                _xpFg.transform.localPosition = _xpBg.transform.localPosition - new Vector3(_barW * (1 - k) * 0.5f, 0, 0);
                if (_lv != null) _lv.text = h.Level.ToString();
                if (_halo != null) _halo.color = _tintBright.A(0.32f + 0.12f * breathe + _pulse * 0.5f);
                if (_body != null) Pose(h, dt);
            }

            /// <summary>金框带子中线的右半圈：框底正中 → 右下角 → 右边 → 右上角 → 框顶正中。圆角和 Art.Frame 一致。</summary>
            void BuildPath(Vector2 size)
            {
                _pathSize = size;
                _path.Clear();
                _cum.Clear();
                const float inset = 3.5f / 64f, outerR = 18f / 64f;
                float hx = size.x * 0.5f - inset, hy = size.y * 0.5f - inset;
                float r = Mathf.Min(outerR - inset, hx, hy);
                void Add(float x, float y)
                {
                    var p = new Vector3(x, y, 0f);
                    _cum.Add(_path.Count == 0 ? 0f : _cum[_cum.Count - 1] + Vector3.Distance(_path[_path.Count - 1], p));
                    _path.Add(p);
                }
                void Arc(float cx, float cy, float from, float to)
                {
                    for (int i = 0; i <= 8; i++)
                    {
                        float t = Mathf.Lerp(from, to, i / 8f) * Mathf.Deg2Rad;
                        Add(cx + Mathf.Cos(t) * r, cy + Mathf.Sin(t) * r);
                    }
                }
                Add(0f, -hy);
                Arc(hx - r, -hy + r, -90f, 0f);
                Arc(hx - r, hy - r, 0f, 90f);
                Add(0f, hy);
            }

            Vector3 PathAt(float s)
            {
                int i = 1;
                while (i < _path.Count - 1 && _cum[i] < s) i++;
                float seg = _cum[i] - _cum[i - 1];
                return Vector3.Lerp(_path[i - 1], _path[i], seg > 1e-5f ? Mathf.Clamp01((s - _cum[i - 1]) / seg) : 1f);
            }

            static Vector3 Mirror(Vector3 p) => new Vector3(-p.x, p.y, 0f);

            /// <summary>
            /// 蓄力：金框先压成暗铜色的槽，亮金线从框底正中往左右两边爬，线头一颗光点。
            /// 满了整圈金框亮起来，一道白光绕框跑；放出大招那下框猛地一闪、外光往外一扩。
            /// </summary>
            void Trace(float k, bool ready, float breathe)
            {
                float len = _cum[_cum.Count - 1];
                float s = len * k;
                _pts.Clear();
                if (k > 0.005f)
                {
                    for (int i = _path.Count - 1; i >= 1; i--) if (_cum[i] < s) _pts.Add(Mirror(_path[i]));
                    _pts.Insert(0, Mirror(PathAt(s)));
                    _pts.Add(_path[0]);
                    for (int i = 1; i < _path.Count; i++) if (_cum[i] < s) _pts.Add(_path[i]);
                    _pts.Add(PathAt(s));
                }
                _trace.positionCount = _pts.Count;
                for (int i = 0; i < _pts.Count; i++) _trace.SetPosition(i, _pts[i]);

                var hot = Color.Lerp(_tintBright, Color.white, 0.7f);
                var lit = ready ? Color.Lerp(_tintBright, Color.white, 0.15f + 0.3f * breathe) : _tintBright;
                var root = ready ? lit : Color.Lerp(_tint, _tintBright, 0.4f);
                float edge = Mathf.Clamp(0.06f / Mathf.Max(0.05f, k), 0.02f, 0.3f);
                var g = _grad ??= new Gradient();
                g.SetKeys(
                    new[] { new GradientColorKey(ready ? lit : hot, 0f), new GradientColorKey(lit, edge), new GradientColorKey(root, 0.5f), new GradientColorKey(lit, 1f - edge), new GradientColorKey(ready ? lit : hot, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                _trace.colorGradient = g;
                _trace.widthMultiplier = ready ? 0.085f : 0.075f;

                var groove = Color.Lerp(_tint, Pal.Ink, 0.6f);
                var band = ready ? Color.Lerp(_tintBright, Color.white, 0.2f * breathe) : groove;
                _frame.color = Color.Lerp(band, Color.white, _burstT);

                bool tips = !ready && k > 0.005f;
                float flick = 0.85f + 0.3f * Mathf.Sin(Time.time * 17f);
                _tipL.color = _tipR.color = hot.A(tips ? 0.9f : 0f);
                _tipL.transform.localScale = _tipR.transform.localScale = Vector3.one * 0.26f * flick;
                if (tips)
                {
                    var p = PathAt(s);
                    _tipR.transform.localPosition = p;
                    _tipL.transform.localPosition = Mirror(p);
                }

                _glint.color = Color.white.A(ready ? 0.85f : 0f);
                if (ready)
                {
                    float lap = Mathf.Repeat(Time.time * 0.9f, 1f) * len * 2f;
                    _glint.transform.localPosition = lap < len ? PathAt(lap) : Mirror(PathAt(len * 2f - lap));
                }
            }

            void Pose(HeroInstance h, float dt)
            {
                float idle = Mathf.Sin(Time.time * 2.6f + h.Id) * 0.012f;
                int frame = 0;
                Vector2 off = Vector2.zero;
                float sx = 1f, sy = 1f + idle;
                if (_lungeT >= 0f)
                {
                    _lungeT += dt / StrikeDur;
                    if (_lungeT >= 1f) _lungeT = -1f;
                }
                if (_lungeT >= 0f)
                {
                    float t = _lungeT, d = _crit ? 1.4f : 1f;
                    var n = new Vector2(_lungeDir.x, _lungeDir.y * 0.5f).normalized;
                    if (t < StrikeK)
                    {
                        float k = Tween.OutCubic(t / StrikeK);
                        frame = 1;
                        switch (_swing)
                        {
                            case HeroSwing.Loose: off = -n * 0.03f * k; sx = 1f - 0.02f * k; break;
                            case HeroSwing.Hurl: off = -n * 0.06f * k; break;
                            case HeroSwing.Fist: off = -n * 0.04f * k; sy -= 0.06f * k; sx += 0.04f * k; break;
                            default: off = -n * 0.05f * k + Vector2.up * 0.03f * k; sy += 0.03f * k; break;
                        }
                    }
                    else
                    {
                        float u = (t - StrikeK) / (1f - StrikeK);
                        float p = u < 0.25f ? Tween.OutCubic(u / 0.25f) : 1f - Tween.OutCubic((u - 0.25f) / 0.75f);
                        frame = u < 0.62f ? 2 : 0;
                        switch (_swing)
                        {
                            case HeroSwing.Loose: off = -n * 0.07f * (1f - u) * d; break;
                            case HeroSwing.Hurl: off = n * 0.08f * p * d; break;
                            case HeroSwing.Thrust: off = n * 0.2f * p * d; sx += 0.05f * p; sy -= 0.03f * p; break;
                            case HeroSwing.Fist: off = n * 0.16f * p * d; sx += 0.06f * p; break;
                            case HeroSwing.Whip: off = n * 0.06f * p * d; break;
                            default: off = n * 0.13f * p * d - Vector2.up * 0.03f * p; sx += 0.05f * p; sy -= 0.05f * p; break;
                        }
                    }
                }
                _body.sprite = _art[frame];
                // 缩放绕画布中心，脚要钉在原地。
                _body.transform.localPosition = _bodyAt + off + new Vector2(0f, (sy - 1f) * _bodyH * (Art.FigureFoot - 0.5f));
                _body.transform.localScale = new Vector3(_bodyH * sx * _face, _bodyH * sy, 1f);
            }

            public void Lunge(Vector2 dir, HeroSwing swing = HeroSwing.Arc, bool crit = false)
            {
                _lungeT = 0f;
                _swing = swing;
                _crit = crit;
                _lungeDir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.up;
                if (Mathf.Abs(_lungeDir.x) > 0.15f) _face = _lungeDir.x < 0 ? -1f : 1f;
            }

            public bool HasArt => _art != null;

            public void StrikeAt(Vector2 at, HeroSwing swing)
            {
                var d = at - (Vector2)_root.localPosition;
                Lunge(d.sqrMagnitude > 0.04f ? d : new Vector2(_face, 0f), swing, true);
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
                Body = Art.EnemyBody(e.Def.Kind, e.Id, e.Boss?.Name);
                float h = Body != null ? BodyHeight(e.Def.Kind)
                    : boss ? 0.55f : elite ? 0.62f : e.Def.Kind == EnemyKind.Jia ? 0.5f : 0.42f;
                GlyphHeight = h;
                _big = boss || elite;
                uint hash = (uint)e.Id * 2654435761u;
                _jitter = new Vector2(((hash >> 8) & 0xff) / 255f - 0.5f, ((hash >> 16) & 0xff) / 255f - 0.5f) * (_big ? 0f : 0.28f);
                _baseColor = boss ? Pal.RedDeep : elite ? Pal.Hex("3B2050") : Pal.Ink;
                _shadow = Gfx.Sprite(Root, Art.SoftDot, Pal.Shadow, new Vector2(0, -h * 0.45f), new Vector2(h * 1.1f, h * 0.35f), 29);
                if (Body != null)
                {
                    if (boss)
                        _aura = Gfx.Sprite(Root, Art.Blob, Pal.Red.A(0.35f), new Vector2(0, -h * 0.3f), Vector2.one * 1.6f, 30);
                    _walk = Art.EnemyWalk(e.Def.Kind, e.Id, e.Boss?.Name);
                    _front = Art.EnemyFront(e.Def.Kind, e.Id, e.Boss?.Name);
                    _back = Art.EnemyBack(e.Def.Kind, e.Id, e.Boss?.Name);
                    if (_walk != null)
                    {
                        _canvas = h / Art.EnemyFigure;
                        _body = Gfx.Sprite(Root, _walk[0], Color.white, new Vector2(0, WalkBodyY(1f)), Vector2.one * _canvas, 32);
                        _walkFrame = (int)(hash >> 4) & 3;
                    }
                    else _body = Gfx.Sprite(Root, Body, Color.white, Vector2.zero, Vector2.one * h, 32);
                }
                else
                {
                    if (boss || elite)
                        _aura = Gfx.Sprite(Root, Art.Blob, (boss ? Pal.Red : Pal.Hex("3B2050")).A(0.35f), Vector2.zero, Vector2.one * (boss ? 1.6f : 0.95f), 30);
                    if (e.Def.Kind == EnemyKind.Jia)
                        _aura = Gfx.Sprite(Root, Art.Circle, Pal.InkSoft.A(0.25f), Vector2.zero, Vector2.one * 0.6f, 30);
                }
                _glyph = Gfx.Text(Root, e.Glyph, Fonts.Brush, h, _baseColor, Vector2.zero, 32);
                if (Body != null) _glyph.gameObject.SetActive(false);
                _cc = Gfx.Text(Root, "", Fonts.Kai, 0.28f, Pal.Hex("5B7FA6"), new Vector2(0.42f, 0.12f), 35);
                _barW = boss ? 1.3f : elite ? 0.8f : 0.45f;
                float y = Body != null ? h * 0.5f + 0.06f : boss ? 0.55f : 0.4f;
                _hpBg = Gfx.Sprite(Root, Art.White, Pal.Ink.A(0.55f), new Vector2(0, y), new Vector2(_barW + 0.04f, 0.09f), 33);
                _hpFg = Gfx.Sprite(Root, Art.White, Pal.Hp, new Vector2(0, y), new Vector2(_barW, 0.06f), 34);
                Root.gameObject.SetActive(false);
            }

            bool _out;

            public void Flash() => _flash = 1f;

            float _swell;
            /// <summary>头目吞兵、放绝技时鼓一下。</summary>
            public void Swell(float amount = 0.4f) => _swell = Mathf.Max(_swell, amount);

            public string GlyphText => _glyph.text;
            public float GlyphHeight { get; }
            public Color GlyphColor => _baseColor;
            /// <summary>画好的官兵小人；null 时照旧写字。图都是朝左走的。</summary>
            public readonly Sprite Body;
            readonly SpriteRenderer _body;
            readonly Sprite[] _walk, _front, _back;
            readonly float _canvas;
            int _walkFrame;
            float _travel;
            /// <summary>0 侧面左右走，1 往上走看后背，-1 往下走看正脸。</summary>
            int _vert;
            public float Face { get; private set; } = 1f;

            /// <summary>走路帧是方画布、脚在画布 0.86 处；按竖向缩放算画布中心，脚始终踩在 -h/2。</summary>
            float WalkBodyY(float sy) => -GlyphHeight * 0.5f + (Art.FigureFoot - 0.5f) * _canvas * sy;
            Vector2 _lastPos;
            bool _hasLast;

            static float BodyHeight(EnemyKind k) => k switch
            {
                EnemyKind.Chai => 0.6f,
                EnemyKind.Jia => 0.74f,
                EnemyKind.Bu => 0.92f,
                EnemyKind.Boss => 1.2f,
                _ => 0.66f,
            };

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
                float bob = _walk != null ? 0f : Mathf.Abs(Mathf.Sin(Time.time * 9f + e.Id)) * 0.04f;
                Root.localPosition = pos + _jitter + _kick + new Vector2(0, bob);
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
                bool rage = e.Boss != null && e.Enraged;
                if (rage) c = Color.Lerp(c, Pal.Red, 0.5f + 0.3f * Mathf.Sin(Time.time * 10f));
                _glyph.color = Color.Lerp(c, Pal.Red, _flash * 0.8f);
                float squash = _flash * 0.22f;
                _swell = Mathf.Max(0f, _swell - dt * 1.6f);
                float grow = (haste ? 1.14f : 1f) * (rage ? 1.15f : 1f) * (1f + Mathf.Sin(_swell * 9f) * _swell * 0.6f + _swell * 0.3f);
                _glyph.transform.localScale = new Vector3(1f + squash, 1f - squash * 0.6f, 1f) * grow;
                if (_aura != null && e.Boss != null)
                {
                    float pulse = rage ? 1f + 0.18f * Mathf.Sin(Time.time * 9f) : 1f;
                    _aura.transform.localScale = Vector3.one * (rage ? 2.3f : 1.6f) * pulse * (1f + _swell * 0.5f);
                    _aura.color = Pal.Red.A(rage ? 0.55f : 0.35f);
                }
                _glyph.transform.localRotation = Quaternion.Euler(0, 0, -_kick.x * 40f);
                if (_body != null) SyncBody(e, pos, squash, grow, haste, rage);
            }

            /// <summary>小人按走向翻面、走路左右晃；挨打变红，定身发蓝，被催阵发红。</summary>
            void SyncBody(Enemy e, Vector2 pos, float squash, float grow, bool haste, bool rage)
            {
                float step = 0f;
                if (_hasLast)
                {
                    var d = pos - _lastPos;
                    // 竖着走比横着走更明显时换正背：往上脸朝路的前方（宋江在上方就朝他），往下才是正脸。
                    if (Mathf.Abs(d.y) > Mathf.Abs(d.x) && Mathf.Abs(d.y) > 0.002f)
                        _vert = d.y > 0f ? 1 : -1;
                    else if (Mathf.Abs(d.x) > 0.002f)
                    {
                        _vert = 0;
                        Face = d.x < 0f ? 1f : -1f;
                    }
                    step = d.magnitude;
                }
                _lastPos = pos;
                _hasLast = true;
                var tint = e.StunT > 0 ? Pal.Hex("9DB4D6") : haste ? Pal.Hex("FFB0A0") : Color.white;
                if (rage) tint = Color.Lerp(tint, Pal.Hex("FF8070"), 0.5f + 0.3f * Mathf.Sin(Time.time * 10f));
                _body.color = Color.Lerp(tint, Pal.Hex("FF4030"), _flash * 0.75f);
                float h = GlyphHeight;
                var t = _body.transform;
                float sy = (1f - squash * 0.6f) * grow;
                if (_walk != null)
                {
                    // 按走过的路程翻帧，走得快腿就迈得快；定身、站住时停在当前步。
                    if (e.StunT <= 0 && step < 0.5f)
                    {
                        _travel += step;
                        float stride = h * 0.2f;
                        while (_travel >= stride) { _travel -= stride; _walkFrame = (_walkFrame + 1) & 3; }
                    }
                    var frames = _vert > 0 && _back != null ? _back : _vert < 0 && _front != null ? _front : _walk;
                    float face = frames == _walk ? Face : 1f;
                    _body.sprite = frames[_walkFrame % frames.Length];
                    t.localScale = new Vector3((1f + squash) * face * grow, sy, 1f) * _canvas;
                    t.localPosition = new Vector2(0, WalkBodyY(sy));
                    t.localRotation = Quaternion.Euler(0, 0, -_kick.x * 40f);
                    return;
                }
                float walk = e.StunT > 0 ? 0f : Mathf.Sin(Time.time * 10f + e.Id) * 5f;
                t.localScale = new Vector3((1f + squash) * Face, 1f - squash * 0.6f, 1f) * h * grow;
                t.localRotation = Quaternion.Euler(0, 0, walk - _kick.x * 40f);
            }
        }

        sealed class GoalView
        {
            readonly BattleView _v;
            readonly int _side;
            readonly Transform _root;
            readonly List<SpriteRenderer> _hearts = new List<SpriteRenderer>();
            readonly List<SpriteRenderer> _heartRims = new List<SpriteRenderer>();
            readonly TextMesh _heartCount;
            readonly TextMesh _danger, _glyph;
            readonly float _glyphY;
            readonly SpriteRenderer _bubble, _shield, _feet;
            readonly TextMesh _bubbleText;
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
                _root.SetParent(side == 0 ? v._low : v._root, false);
                _root.localPosition = pos;
                // 宋江直接站在路上：脚下一团墨影，头顶飘着心，不垫白底。
                var song = Art.Opt("pc_song");
                _feet = Gfx.Sprite(_root, Art.SoftDot, Pal.Ink.A(0.34f), new Vector2(0, song != null ? -0.44f : -0.36f), new Vector2(0.86f, 0.26f), 24);
                if (song != null)
                {
                    _glyphY = 0.02f;
                    _glyph = Gfx.Text(_root, "", Fonts.Brush, 0.58f, Pal.RedDeep, new Vector2(0, _glyphY), 27);
                    _song = Gfx.Sprite(_root, song, Color.white, new Vector2(0, _glyphY), Vector2.one * SongH, 27);
                }
                else
                {
                    // 帽子只压住宝盖头上沿，字还读得出是「宋」。
                    _glyphY = -0.15f;
                    _glyph = Gfx.Text(_root, "宋", Fonts.Brush, 0.58f, Pal.RedDeep, new Vector2(0, _glyphY), 27);
                    Gfx.Sprite(_root, Art.Img("hat"), Color.white, new Vector2(0.03f, 0.26f), Vector2.one * 0.46f, 28);
                }
                _shield = Gfx.Sprite(_root, Art.Swirl, Pal.Red.A(0.75f), Vector2.zero, Vector2.one * 1.9f, 29);
                _shield.enabled = false;
                // 心飘在帽子上方；对手大本营贴着屏幕顶时改挂脚下，不钻进标题栏。
                _heartY = pos.y + 0.7f > TopY - 0.5f ? (song != null ? -0.64f : -0.56f) : (song != null ? 0.68f : 0.6f);
                _heartCount = Gfx.Text(_root, "", Fonts.Brush, 0.26f, Pal.Ink, new Vector2(0.1f, _heartY), 41);
                _lastHearts = s.Hearts;
                _inward = pos.x > 0f ? -1f : 1f;
                _danger = Gfx.Text(_root, "危", Fonts.Brush, 0.6f, Pal.Red, new Vector2(_inward, 0), 30);
                _danger.gameObject.SetActive(false);
                float by = side == 0 ? 1.45f : -1.45f;
                _bubble = Gfx.Sprite(_root, Art.Rounded, Color.white, new Vector2(_inward * 1.2f, by), new Vector2(2.2f, 0.55f), 70);
                _bubbleText = Gfx.Text(_root, "", Fonts.Kai, 0.26f, Pal.Ink, new Vector2(_inward * 1.2f, by), 71);
                _bubble.enabled = false;
            }

            const int MaxRow = 5;
            const float HeartSize = 0.24f, HeartGap = 0.25f;
            const float SongH = 0.98f;
            readonly SpriteRenderer _song;

            /// <summary>
            /// 心直接飘在宋江头顶，每颗描一圈墨边，在土路和草地上都看得清；掉了的心只剩墨边。
            /// 续命丹之后心太多，改成一颗心加「剩余/上限」。整排超出棋盘时往里挪。
            /// </summary>
            void SyncHearts(Side s)
            {
                int max = s.MaxHearts, left = Mathf.Max(0, s.Hearts);
                bool compact = max > MaxRow;
                int icons = compact ? 1 : max;
                while (_hearts.Count < icons)
                {
                    _heartRims.Add(Gfx.Sprite(_root, Art.Heart, Pal.Ink, Vector2.zero, Vector2.one * (HeartSize + 0.07f), 39));
                    _hearts.Add(Gfx.Sprite(_root, Art.Heart, Pal.Red, Vector2.zero, Vector2.one * HeartSize, 40));
                }

                float rowW = compact ? 0.86f : icons * HeartGap;
                float x0 = _root.localPosition.x, half = rowW * 0.5f;
                float rowX = Mathf.Clamp(x0, -3.96f + half, 3.96f - half) - x0;
                float pop = 1f + _hurt * 0.22f;
                float bob = Mathf.Sin(Time.time * 2.4f) * 0.025f;

                _heartCount.text = compact ? $"{left}/{max}" : "";
                _heartCount.transform.localPosition = new Vector2(rowX + 0.14f, _heartY + bob);

                for (int i = 0; i < _hearts.Count; i++)
                {
                    var heart = _hearts[i];
                    var rim = _heartRims[i];
                    heart.enabled = rim.enabled = i < icons;
                    if (i >= icons) continue;
                    float x = compact ? rowX - 0.24f : rowX + (i - (icons - 1) * 0.5f) * HeartGap;
                    var at = new Vector2(x, _heartY + bob);
                    bool full = compact ? left > 0 : i < left;
                    heart.transform.localPosition = rim.transform.localPosition = at;
                    heart.enabled = full;
                    heart.color = Color.Lerp(Pal.Hex("E5383B"), Color.white, _hurt * 0.5f);
                    heart.transform.localScale = Vector3.one * HeartSize * pop;
                    rim.transform.localScale = Vector3.one * (HeartSize + 0.07f) * (full ? pop : 0.9f);
                    rim.color = full ? Pal.Ink.A(0.9f) : Pal.Ink.A(0.35f);
                }
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
                SyncHearts(s);
                if (s.Hearts > _lastHearts) _lastHearts = s.Hearts;
                _hurt = Mathf.Max(0, _hurt - dt * 3f);
                _glyph.color = Color.Lerp(Pal.RedDeep, Pal.Red, _hurt);
                _feet.color = Color.Lerp(Pal.Ink.A(0.34f), Pal.Red.A(0.5f), _hurt);
                _glyph.transform.localPosition = new Vector2(Mathf.Sin(Time.time * 60f) * 0.06f * _hurt, _glyphY);
                if (_song != null)
                {
                    _song.color = Color.Lerp(Color.white, Pal.Hex("FF7060"), _hurt);
                    float idle = Mathf.Sin(Time.time * 2.2f) * 0.02f;
                    _song.transform.localPosition = new Vector2(Mathf.Sin(Time.time * 60f) * 0.06f * _hurt, _glyphY + idle * 0.4f);
                    _song.transform.localScale = new Vector3(SongH, SongH * (1f + idle), 1f);
                }

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
