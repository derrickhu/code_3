using System;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;

namespace Rescue
{
    /// <summary>战场的世界空间表现与输入。只读 Match 状态渲染，玩家操作走 Side 的公开接口。</summary>
    public sealed class BattleView : MonoBehaviour
    {
        public const float Left = -4f;
        public const float MyBottom = -5.9f;
        public const float OppBottom = 1.0f;
        public const float CampY = -7.05f;
        public const float ButtonY = -8.3f;
        public const float TopY = 8.05f;

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
        Vector3 _camBase;
        int _frame;

        readonly SpriteRenderer[][,] _cells = { new SpriteRenderer[Board.Cols, Board.Rows], new SpriteRenderer[Board.Cols, Board.Rows] };
        readonly TextMesh[][,] _willow = { new TextMesh[Board.Cols, Board.Rows], new TextMesh[Board.Cols, Board.Rows] };
        readonly SpriteRenderer[,] _hl = new SpriteRenderer[Board.Cols, Board.Rows];
        readonly TextMesh[,] _hlMark = new TextMesh[Board.Cols, Board.Rows];
        readonly Dictionary<int, PieceView> _pieces = new Dictionary<int, PieceView>();
        readonly Dictionary<int, EnemyView> _enemies = new Dictionary<int, EnemyView>();
        readonly Dictionary<int, FrameView> _frames = new Dictionary<int, FrameView>();
        readonly GoalView[] _goals = new GoalView[2];
        readonly List<Fx> _fx = new List<Fx>();
        readonly Stack<Fx> _fxPool = new Stack<Fx>();
        readonly SpriteRenderer[] _campSlots = new SpriteRenderer[Tuning.CampSlots];

        SpriteRenderer _recruitBtn, _pauseBtn, _mengBtn, _shuBtn;
        TextMesh _recruitCost, _recruitLabel, _bunsText, _waveText, _midText, _mengText, _shuText, _rivalText, _titleText;
        Transform _bunIcon;
        static readonly Vector3 _bunIconScale = new Vector3(0.42f, 0.34f, 1f);
        TextMesh _tipText;
        SpriteRenderer _tipBg;
        float _tipT;

        // 拖拽状态
        PieceView _drag;
        int _dragCamp = -1;
        CellPos _dragCell;
        Vector2 _downPos;
        bool _dragMoved;

        // ─────────────────────────── 布局 ───────────────────────────

        public static Vector2 World(int side, V2 p) =>
            side == 0 ? new Vector2(Left + p.x, MyBottom + p.y) : new Vector2(Left + (Board.Cols - p.x), OppBottom + (Board.Rows - p.y));

        public static Vector2 CellWorld(int side, int c, int r) => World(side, new CellPos(c, r).Center);
        public static Vector2 CampWorld(int i) => new Vector2(-2.3f + i * 1.12f, CampY);
        public Vector2 BunsWorld => _bunIcon.position;

        public void Build(Match m, Camera cam)
        {
            M = m;
            _cam = cam;
            _camBase = cam.transform.position;
            _root = new GameObject("World").transform;
            _root.SetParent(transform, false);
            _fxRoot = new GameObject("Fx").transform;
            _fxRoot.SetParent(transform, false);

            var paper = Gfx.Sprite(_root, Art.Paper, Pal.Paper, new Vector2(0, 0), new Vector2(30, 30), -10, "paper");
            paper.drawMode = SpriteDrawMode.Tiled;
            paper.size = new Vector2(30, 30);
            paper.transform.localScale = Vector3.one;
            Gfx.Text(_root, "梁山", Fonts.Brush, 5.5f, Pal.Ink.A(0.035f), new Vector2(0, 4.1f), -9);
            Gfx.Text(_root, "水泊", Fonts.Brush, 5.5f, Pal.Ink.A(0.035f), new Vector2(0, -2.9f), -9);

            for (int s = 0; s < 2; s++) BuildBoard(s);
            BuildCamp();
            BuildHud();
            BuildTip();
            for (int s = 0; s < 2; s++) _goals[s] = new GoalView(this, s);
            SyncAll(true);
        }

        void BuildBoard(int side)
        {
            var b = M.Sides[side].Board;
            float bottom = side == 0 ? MyBottom : OppBottom;
            Gfx.Sprite(_root, Art.Rounded, Pal.Ink.A(0.85f), new Vector2(0, bottom + Board.Rows * 0.5f), new Vector2(Board.Cols + 0.3f, Board.Rows + 0.3f), 1, "board_bg");
            Gfx.Sprite(_root, Art.Rounded, Pal.PaperDark, new Vector2(0, bottom + Board.Rows * 0.5f), new Vector2(Board.Cols + 0.12f, Board.Rows + 0.12f), 2, "board_in");

            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    var pos = CellWorld(side, c, r);
                    var cell = b.Cells[c, r];
                    _cells[side][c, r] = Gfx.Sprite(_root, Art.Rounded, CellColor(cell, side), pos, new Vector2(0.95f, 0.95f), 10, "cell");
                    if (cell == Cell.Path)
                    {
                        Gfx.Sprite(_root, Art.SoftDot, Pal.PathEdge.A(0.35f), pos + new Vector2(0.18f, -0.15f), new Vector2(0.18f, 0.12f), 11);
                        Gfx.Sprite(_root, Art.SoftDot, Pal.PathEdge.A(0.3f), pos + new Vector2(-0.2f, 0.17f), new Vector2(0.14f, 0.1f), 11);
                    }
                    else if (cell == Cell.Water)
                    {
                        var w1 = Gfx.Sprite(_root, Art.Stroke, Color.white.A(0.45f), pos + new Vector2(-0.12f, 0.12f), new Vector2(0.45f, 0.1f), 11);
                        var w2 = Gfx.Sprite(_root, Art.Stroke, Color.white.A(0.35f), pos + new Vector2(0.14f, -0.16f), new Vector2(0.38f, 0.09f), 11);
                    }
                    else if (cell == Cell.Willow)
                    {
                        _willow[side][c, r] = Gfx.Text(_root, "柳", Fonts.Brush, 0.7f, Pal.Willow, pos, 12);
                    }
                    if (side == 0)
                    {
                        _hl[c, r] = Gfx.Sprite(_root, Art.Rounded, Color.clear, pos, new Vector2(0.95f, 0.95f), 14, "hl");
                        _hlMark[c, r] = Gfx.Text(_root, "", Fonts.Kai, 0.26f, Pal.RedDeep, pos + new Vector2(0, -0.33f), 15);
                    }
                }

            var sp = CellWorld(side, b.Spawn.c, b.Spawn.r);
            Gfx.Sprite(_root, Art.Blob, Pal.Ink.A(0.8f), sp, new Vector2(1.0f, 1.0f), 12, "spawn");
            Gfx.Text(_root, "官", Fonts.Brush, 0.45f, Pal.Paper, sp, 13);
        }

        static Color CellColor(Cell c, int side)
        {
            switch (c)
            {
                case Cell.Path: return Pal.Path;
                case Cell.Water: return Pal.Water;
                case Cell.Rock: return Pal.InkSoft;
                default: return side == 0 ? Pal.Grass : Pal.GrassRival;
            }
        }

        void BuildCamp()
        {
            Gfx.Sprite(_root, Art.Rounded, Pal.Ink.A(0.85f), new Vector2(-0.5f, CampY), new Vector2(7.4f, 1.22f), 1);
            Gfx.Sprite(_root, Art.Rounded, Pal.PaperDark, new Vector2(-0.5f, CampY), new Vector2(7.28f, 1.1f), 2);
            Gfx.Text(_root, "山寨", Fonts.Brush, 0.42f, Pal.RedDeep, new Vector2(-3.55f, CampY), 5);
            for (int i = 0; i < _campSlots.Length; i++)
                _campSlots[i] = Gfx.Sprite(_root, Art.Rounded, Pal.Paper.A(0.9f), CampWorld(i), new Vector2(0.98f, 0.98f), 4, "slot");
        }

        void BuildHud()
        {
            _recruitBtn = Gfx.Sprite(_root, Art.Rounded, Pal.Red, new Vector2(0.2f, ButtonY), new Vector2(2.7f, 1.0f), 50, "recruit");
            Gfx.Sprite(_root, Art.Rounded, Pal.Shadow, new Vector2(0.2f, ButtonY - 0.09f), new Vector2(2.7f, 1.0f), 49);
            _recruitLabel = Gfx.Text(_root, "聚  义", Fonts.Brush, 0.5f, Pal.Paper, new Vector2(0.2f, ButtonY + 0.14f), 52);
            _recruitCost = Gfx.Text(_root, "", Fonts.Kai, 0.26f, Pal.Paper, new Vector2(0.3f, ButtonY - 0.27f), 52);
            Gfx.Sprite(_root, Art.Circle, Pal.GoldBright, new Vector2(-0.08f, ButtonY - 0.27f), new Vector2(0.2f, 0.16f), 52);

            _mengBtn = Gfx.Sprite(_root, Art.Rounded, Pal.InkSoft, new Vector2(-3.05f, ButtonY), new Vector2(1.6f, 0.8f), 50, "meng");
            _mengText = Gfx.Text(_root, "", Fonts.Kai, 0.26f, Pal.Paper, new Vector2(-3.05f, ButtonY), 52);
            _shuBtn = Gfx.Sprite(_root, Art.Rounded, Pal.InkSoft, new Vector2(3.35f, ButtonY), new Vector2(1.6f, 0.8f), 50, "shu");
            _shuText = Gfx.Text(_root, "", Fonts.Kai, 0.26f, Pal.Paper, new Vector2(3.35f, ButtonY), 52);

            _pauseBtn = Gfx.Sprite(_root, Art.Circle, Pal.Ink.A(0.85f), new Vector2(-3.55f, TopY), new Vector2(0.75f, 0.75f), 50, "pause");
            Gfx.Text(_root, "停", Fonts.Brush, 0.38f, Pal.Paper, new Vector2(-3.55f, TopY), 52);

            Gfx.Sprite(_root, Art.Rounded, Pal.Ink.A(0.85f), new Vector2(-1.95f, TopY), new Vector2(1.9f, 0.62f), 49);
            _bunIcon = Gfx.Sprite(_root, Art.Circle, Pal.GoldBright, new Vector2(-2.6f, TopY), new Vector2(0.42f, 0.34f), 51, "bun").transform;
            Gfx.Text(_root, "饼", Fonts.Brush, 0.24f, Pal.RedDeep, new Vector2(-2.6f, TopY), 52);
            _bunsText = Gfx.Text(_root, "0", Fonts.Kai, 0.36f, Pal.Paper, new Vector2(-1.75f, TopY), 52);

            _titleText = Gfx.Text(_root, M.Chapter.Name, Fonts.Brush, 0.46f, Pal.Ink, new Vector2(0.75f, TopY + 0.2f), 52);
            _waveText = Gfx.Text(_root, "", Fonts.Kai, 0.28f, Pal.InkSoft, new Vector2(0.75f, TopY - 0.27f), 52);
            _rivalText = Gfx.Text(_root, "对手 · " + M.Chapter.RivalName, Fonts.Kai, 0.28f, Pal.InkSoft, new Vector2(3.95f, TopY), 52, TextAnchor.MiddleRight);

            Gfx.Sprite(_root, Art.Stroke, Pal.Ink.A(0.5f), new Vector2(0, 0.55f), new Vector2(8.2f, 0.12f), 3);
            _midText = Gfx.Text(_root, "", Fonts.Kai, 0.3f, Pal.Ink, new Vector2(0, 0.55f), 6);
            var midBg = Gfx.Sprite(_root, Art.Rounded, Pal.Paper, new Vector2(0, 0.55f), new Vector2(3.6f, 0.46f), 5);
        }

        void BuildTip()
        {
            _tipBg = Gfx.Sprite(_root, Art.Rounded, Pal.Ink.A(0.9f), Vector2.zero, new Vector2(3, 1), 80, "tipbg");
            _tipText = Gfx.Text(_root, "", Fonts.Kai, 0.27f, Pal.Paper, Vector2.zero, 81);
            _tipBg.enabled = false;
            _tipText.text = "";
        }

        // ─────────────────────────── 主循环 ───────────────────────────

        void Update()
        {
            if (M == null) return;
            _frame++;
            if (!Paused && !M.Over)
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
            UpdateFx(Time.deltaTime);
            UpdateTip(Time.deltaTime);

            _shake = Mathf.Max(0, _shake - Time.deltaTime * 2.5f);
            _cam.transform.position = _camBase + (Vector3)(UnityEngine.Random.insideUnitCircle * _shake * 0.18f);
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
            _recruitCost.text = p.RecruitCost.ToString();
            bool can = p.CanRecruit;
            _recruitBtn.color = can ? Pal.Red : p.EmptyCampSlots == 0 ? Pal.InkSoft : Color.Lerp(Pal.Red, Pal.InkSoft, 0.6f);
            float pulse = can ? 1f + Mathf.Sin(Time.time * 6f) * 0.025f : 1f;
            _recruitBtn.size = new Vector2(2.7f, 1.0f) * pulse;
            _recruitLabel.text = p.EmptyCampSlots == 0 ? "寨满" : "聚  义";
            if (_drag != null && _dragMoved)
            {
                bool over = OverRecruit(PointerWorld());
                _recruitBtn.color = over ? Pal.Ink : Pal.InkSoft;
                _recruitBtn.size = new Vector2(2.7f, 1.0f) * (over ? 1.08f : 1f);
                _recruitLabel.text = "遣散";
                _recruitCost.text = "+" + Side.DismissRefund(_drag.Piece);
            }

            _mengBtn.gameObject.SetActive(p.Items.MengHan > 0);
            _mengText.gameObject.SetActive(p.Items.MengHan > 0);
            _mengText.text = "蒙汗药×" + p.Items.MengHan;
            _shuBtn.gameObject.SetActive(p.Items.TianShu > 0);
            _shuText.gameObject.SetActive(p.Items.TianShu > 0);
            _shuText.text = "天书×" + p.Items.TianShu;

            _waveText.text = M.Wave > 0 ? $"第 {M.Wave} 波" : "备战";
            float next = M.NextWaveIn;
            _midText.text = M.Wave == 0 ? $"官兵 {next:0} 秒后到" : next > 0.1f ? $"下一波 {next:0} 秒" : $"第 {M.Wave} 波来袭";

            for (int s = 0; s < 2; s++) SyncSide(s, snap);
            SyncCamp(snap);
            SyncEnemies();
            for (int s = 0; s < 2; s++) _goals[s].Sync();
            CleanupStale();
        }

        void SyncSide(int s, bool snap)
        {
            var side = M.Sides[s];
            var b = side.Board;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    if (_willow[s][c, r] != null && b.Cells[c, r] != Cell.Willow)
                    {
                        Destroy(_willow[s][c, r].gameObject);
                        _willow[s][c, r] = null;
                        _cells[s][c, r].color = CellColor(b.Cells[c, r], s);
                    }
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
                if (fresh) { v.Root.localPosition = v.Target; v.PopIn(); }
                if (snap) v.Root.localPosition = v.Target;
            }
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
                    if (mine) Sfx.Play(Snd.Recruit);
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
                case Ev.Attack:
                    UnitAttackFx(ev);
                    break;
                case Ev.HeroAttack:
                    HeroAttackFx(ev);
                    break;
                case Ev.Hit:
                    if (_enemies.TryGetValue(ev.A, out var hv)) hv.Flash();
                    if (mine && ev.Flag > 0)
                        FloatText(World(ev.Side, ev.P) + new Vector2(UnityEngine.Random.Range(-0.2f, 0.2f), 0.4f),
                            (ev.Flag == 2 ? "克制 " : "") + Mathf.RoundToInt(ev.Val), ev.Flag == 2 ? Pal.Red : Pal.Gold, ev.Flag == 2 ? 0.4f : 0.32f, 0.7f);
                    if (mine) Sfx.Play(Snd.Hit, 0.25f);
                    break;
                case Ev.Kill:
                {
                    var pos = World(ev.Side, ev.P);
                    Splat(pos, ev.Flag == 2 ? 2.4f : ev.Flag == 1 ? 1.4f : 0.8f, Pal.Ink.A(0.5f), 0.8f);
                    if (mine)
                    {
                        Sfx.Play(Snd.Kill, 0.35f);
                        BunFly(pos, (int)ev.Val);
                        if (ev.Flag == 2) { _shake = 1f; Sfx.Play(Snd.Ult); }
                    }
                    break;
                }
                case Ev.Leak:
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
                    if (r > 0.1f) Ring(pos, r * 2f, boss ? Pal.Red : Pal.Ink, 0.5f, 44);
                    string word = ev.Text ?? "";
                    int colon = word.IndexOf('：');
                    if (colon > 0 && !boss) word = word.Substring(0, colon) + "！";
                    if (!string.IsNullOrEmpty(word)) FloatText(pos + new Vector2(0, 0.7f), word, boss ? Pal.Red : Pal.RedDeep, mine ? 0.42f : 0.3f, 1.2f);
                    if (mine && !boss) { Sfx.Play(Snd.Ult, 0.7f); _shake = Mathf.Max(_shake, 0.5f); }
                    if (_frames.TryGetValue(ev.A, out var fv)) fv.Glow();
                    break;
                }
                case Ev.StaffUsed:
                {
                    var pos = World(ev.Side, ev.P);
                    Leaves(pos);
                    if (mine) Sfx.Play(Snd.Staff);
                    break;
                }
                case Ev.Buns:
                    if (mine) BunFly(World(ev.Side, ev.P), (int)ev.Val);
                    break;
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
                    if (mine) { Sfx.Play(Snd.Ult); _shake = 0.6f; }
                    break;
                case Ev.BossSpawn:
                    Sfx.Play(Snd.Warn);
                    break;
                case Ev.EliteWarn:
                    Sfx.Play(Snd.Warn, 0.6f);
                    break;
            }
        }

        void UnitAttackFx(SimEvent ev)
        {
            var from = World(ev.Side, ev.P);
            var to = World(ev.Side, ev.Q);
            var kind = (UnitKind)ev.B;
            if (_pieces.TryGetValue(ev.A, out var pv)) pv.Attack(kind, (to - from).normalized);
            var col = Pal.ForLevel((int)ev.Val).A(0.85f);
            switch (kind)
            {
                case UnitKind.Dao: SlashAt(to, col, 0.9f); break;
                case UnitKind.Qiang: Thrust(from, to, col, 0.16f); break;
                case UnitKind.Gong: Arrow(from, to, col); break;
                case UnitKind.Qi: Splat(to, 0.5f, col.A(0.4f), 0.3f); break;
            }
        }

        void HeroAttackFx(SimEvent ev)
        {
            var from = World(ev.Side, ev.P);
            var to = World(ev.Side, ev.Q);
            var id = (HeroId)ev.B;
            var col = Pal.GoldBright;
            if (ev.Flag == 2)
            {
                Thrust(from, to, Pal.Red, 0.35f);
                Ring(to, 1.6f, Pal.Red, 0.4f, 46);
                if (ev.Side == 0) Sfx.Play(Snd.Ult, 0.6f, 1.3f);
                return;
            }
            switch (id)
            {
                case HeroId.HuaRong:
                case HeroId.HuSanNiang:
                case HeroId.RuanXiaoQi:
                case HeroId.RuanXiaoEr:
                case HeroId.ZhangShun:
                case HeroId.ZhangHeng:
                    Arrow(from, to, col);
                    break;
                case HeroId.LinChong:
                    Thrust(from, to, col, 0.22f);
                    break;
                default:
                    SlashAt(to, ev.Flag == 1 ? Pal.Red : col, 1.2f);
                    break;
            }
            if (_frames.TryGetValue(ev.A, out var fv)) fv.Pulse();
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

        void SlashAt(Vector2 pos, Color c, float size)
        {
            var fx = NewSpriteFx(Art.Slash, c, pos, new Vector2(size, size), 42, 0.22f, (f, k) =>
            {
                f.Sr.color = f.C.A(f.C.a * (1 - k));
                f.Go.transform.localScale = new Vector3(size * (0.6f + 0.6f * k), size * (0.8f + 0.2f * k), 1);
            });
            fx.Go.transform.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-60f, 60f) + 180f * (UnityEngine.Random.value < 0.5f ? 0 : 1));
        }

        void Thrust(Vector2 from, Vector2 to, Color c, float thick)
        {
            var dir = to - from;
            float len = dir.magnitude + 0.4f;
            var mid = from + dir.normalized * len * 0.5f;
            var fx = NewSpriteFx(Art.Stroke, c, mid, new Vector2(len, thick), 42, 0.2f, (f, k) =>
            {
                f.Sr.color = f.C.A(f.C.a * (1 - k));
                f.Go.transform.localScale = new Vector3(len * (0.4f + 0.6f * Tween.OutCubic(Mathf.Min(1, k * 3))), thick * (1 - k * 0.5f), 1);
            });
            fx.Go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        void Arrow(Vector2 from, Vector2 to, Color c)
        {
            var dir = to - from;
            var fx = NewSpriteFx(Art.Stroke, c, from, new Vector2(0.45f, 0.09f), 42, 0.14f, (f, k) =>
            {
                f.Go.transform.localPosition = Vector2.Lerp(f.A, f.B, k);
            });
            fx.A = from;
            fx.B = to;
            fx.Go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
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

        void BunFly(Vector2 from, int n)
        {
            var target = (Vector2)_bunIcon.position;
            NewSpriteFx(Art.Circle, Pal.GoldBright, from, new Vector2(0.24f, 0.2f), 47, 0.55f, (f, k) =>
            {
                float e = k * k;
                var p = Vector2.Lerp(f.A, f.B, e) + new Vector2(0, Mathf.Sin(k * Mathf.PI) * 0.8f);
                f.Go.transform.localPosition = p;
                if (k >= 0.99f) Tween.Punch(_bunIcon, 0.15f, 0.25f, _bunIconScale);
            }).A = from;
            _fx[_fx.Count - 1].B = target;
            if (n > 1) FloatText(from + new Vector2(0, 0.35f), "+" + n, Pal.Gold, 0.28f, 0.6f);
        }

        void CoinRain()
        {
            for (int i = 0; i < 36; i++)
            {
                var start = new Vector2(UnityEngine.Random.Range(-4f, 4f), MyBottom + Board.Rows + UnityEngine.Random.Range(0f, 2.5f));
                var end = start + new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), -UnityEngine.Random.Range(5f, 8f));
                var fx = NewSpriteFx(Art.Circle, Pal.GoldBright, start, new Vector2(0.22f, 0.18f), 47, UnityEngine.Random.Range(0.9f, 1.5f), (f, k) =>
                {
                    f.Go.transform.localPosition = Vector2.Lerp(f.A, f.B, k);
                    f.Sr.color = f.C.A(k < 0.8f ? 1 : (1 - k) * 5);
                });
                fx.A = start;
                fx.B = end;
            }
        }

        void Leaves(Vector2 pos)
        {
            for (int i = 0; i < 14; i++)
            {
                var dir = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.6f, 1.6f);
                var fx = NewSpriteFx(Art.SoftDot, Pal.Willow, pos, new Vector2(0.22f, 0.12f), 46, 0.7f, (f, k) =>
                {
                    f.Go.transform.localPosition = f.A + (f.B - f.A) * Tween.OutCubic(k) + new Vector2(0, -k * k * 0.6f);
                    f.Sr.color = f.C.A(1 - k);
                });
                fx.A = pos;
                fx.B = pos + dir;
            }
            Splat(pos, 1.4f, Pal.Willow.A(0.35f), 0.7f);
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

        bool TryMyCell(Vector2 w, out int c, out int r)
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

        void HandleInput()
        {
            var p = M.Player;
            var w = PointerWorld();

            if (Input.GetMouseButtonDown(0))
            {
                if (Ui.PointerOverUi()) return;
                _downPos = w;
                _dragMoved = false;
                if (In(w, _recruitBtn.transform.localPosition, new Vector2(2.8f, 1.1f)))
                {
                    if (p.Recruit()) Tween.Punch(_recruitBtn.transform, 0.15f, 0.12f);
                    else Tip(_recruitBtn.transform.localPosition + Vector3.up * 1.1f, p.EmptyCampSlots == 0 ? "山寨满了，先把字拖上战场" : "炊饼不够，多杀几个官兵");
                    return;
                }
                if (In(w, _pauseBtn.transform.localPosition, new Vector2(0.9f, 0.9f))) { OnPauseClicked?.Invoke(); return; }
                if (_mengBtn.gameObject.activeSelf && In(w, _mengBtn.transform.localPosition, new Vector2(1.6f, 0.8f))) { p.UseMengHan(); return; }
                if (_shuBtn.gameObject.activeSelf && In(w, _shuBtn.transform.localPosition, new Vector2(1.6f, 0.8f))) { p.UseTianShu(); return; }

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
            }

            if (_drag != null)
            {
                if ((w - _downPos).magnitude > 0.18f) _dragMoved = true;
                if (_dragMoved) _drag.Target = w + new Vector2(0, 0.55f);
                ShowHighlights(_drag.Piece, w);

                if (Input.GetMouseButtonUp(0)) EndDrag(w);
            }
        }

        bool OverRecruit(Vector2 w) => In(w, _recruitBtn.transform.localPosition, new Vector2(2.9f, 1.3f));

        void BeginDrag(PieceView v, int campSlot, CellPos cell)
        {
            _drag = v;
            _dragCamp = campSlot;
            _dragCell = cell;
            v.SetDragging(true);
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
                else if (v.Piece.Type == PieceType.Chanzhang) Tip(CellWorld(0, c, r) + Vector2.up * 0.9f, "禅杖只能拔柳树");
            }
            if (!ok) v.Punch(0.1f);
        }

        void ShowHighlights(Piece piece, Vector2 w)
        {
            var p = M.Player;
            var b = p.Board;
            bool hasHover = TryMyCell(w + new Vector2(0, 0.35f), out var hc, out var hr);
            var nameTargets = piece.Type == PieceType.HeroChar ? NameTargets(piece.Char) : null;

            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    var t = b.Pieces[c, r];
                    Color col = Color.clear;
                    string mark = "";
                    if (piece.Type == PieceType.Chanzhang)
                    {
                        if (b.Cells[c, r] == Cell.Willow) col = Pal.Willow.A(0.45f + 0.2f * Mathf.Sin(Time.time * 8));
                    }
                    else if (b.Placeable(c, r))
                    {
                        if (t != null && ReferenceEquals(t, piece)) col = Color.clear;
                        else if (Side.CanMerge(piece, t)) { col = Pal.GoldBright.A(0.55f + 0.25f * Mathf.Sin(Time.time * 8)); mark = "合"; }
                        else if (t == null) col = Color.white.A(0.28f);
                        if (nameTargets != null && nameTargets.TryGetValue(new CellPos(c, r), out var nm))
                        {
                            col = Pal.Red.A(0.35f + 0.2f * Mathf.Sin(Time.time * 8));
                            mark = nm;
                        }
                    }
                    if (hasHover && c == hc && r == hr && col.a > 0) col = col.A(Mathf.Min(1, col.a + 0.35f));
                    _hl[c, r].color = col;
                    _hlMark[c, r].text = mark;
                }
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
        }

        void ShowInfo(PieceView v)
        {
            var piece = v.Piece;
            string s;
            if (piece.Type == PieceType.Unit)
            {
                var d = UnitDef.Get(piece.Kind);
                s = $"{d.Glyph} · {ToCn(piece.Level)}级\n{d.Role}\n两个同级的{d.Glyph}拖到一起可升级";
            }
            else if (piece.Type == PieceType.Chanzhang)
            {
                s = "禅杖（鲁智深遗物）\n拖到柳树上，拔掉它开出新地";
            }
            else if (v.Hero != null)
            {
                var d = v.Hero.Def;
                s = $"{d.Name} · {d.Nick}  {ToCn(v.Hero.Level)}级\n{d.Skill}\n{(string.IsNullOrEmpty(d.Ult) ? "" : "绝技：" + d.Ult)}";
            }
            else
            {
                var names = new List<string>();
                foreach (var h in M.Player.Pool)
                    if (h.Name.IndexOf(piece.Char) >= 0) names.Add(h.Hidden ? "？？？" : h.Name);
                s = $"「{piece.Char}」还在睡大觉\n可组成：{string.Join("、", names)}\n按名字顺序左→右或上→下摆好";
            }
            Tip((Vector2)v.Root.localPosition + new Vector2(0, 1.2f), s);
        }

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
            pos.x = Mathf.Clamp(pos.x, -4.1f + size.x * 0.5f, 4.1f - size.x * 0.5f);
            pos.y = Mathf.Min(pos.y, 7.4f);
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
            return n >= 0 && n <= 10 ? d[n] : n.ToString();
        }

        public Vector2 RecruitButtonPos => _recruitBtn.transform.localPosition;

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

            readonly SpriteRenderer _tile, _shadow;
            readonly TextMesh _glyph, _level;
            readonly Transform _glyphT;
            float _animT;
            UnitKind _animKind;
            Vector2 _animDir;
            float _punch;
            int _lastLevel = -1;
            bool _dragging;
            int _baseOrder;

            public PieceView(Transform parent, Piece p, int side)
            {
                Piece = p;
                Side = side;
                Root = new GameObject("piece" + p.Id).transform;
                Root.SetParent(parent, false);
                _baseOrder = 20;
                _shadow = Gfx.Sprite(Root, Art.Rounded, Pal.Shadow, new Vector2(0.04f, -0.07f), new Vector2(0.86f, 0.86f), 20);
                _tile = Gfx.Sprite(Root, Art.Rounded, Pal.Tile, Vector2.zero, new Vector2(0.86f, 0.86f), 21);
                string g = p.Glyph.ToString();
                _glyph = Gfx.Text(Root, g, Fonts.Brush, 0.66f, Pal.Ink, new Vector2(0, -0.02f), 22);
                _glyphT = _glyph.transform;
                _level = Gfx.Text(Root, "", Fonts.Kai, 0.22f, Pal.InkSoft, new Vector2(0.29f, 0.29f), 23);
                Refresh();
            }

            void Refresh()
            {
                bool hero = Piece.Type == PieceType.HeroChar;
                bool formed = Hero != null;
                if (Piece.Type == PieceType.Chanzhang)
                {
                    _tile.color = Pal.Willow.A(0.25f) + Pal.Tile * 0.75f;
                    _glyph.color = Pal.Willow;
                    _level.text = "";
                    return;
                }
                _level.text = Piece.Level.ToString();
                if (hero)
                {
                    _tile.color = formed ? Pal.TileHero : Pal.Tile;
                    _glyph.color = formed ? Pal.RedDeep : Pal.Gold;
                    _level.color = Pal.Gold;
                }
                else
                {
                    _tile.color = Color.Lerp(Pal.Tile, Pal.ForLevel(Piece.Level), Piece.Level > 1 ? 0.08f : 0f);
                    _glyph.color = Pal.ForLevel(Piece.Level);
                    _level.color = Pal.ForLevel(Piece.Level);
                }
            }

            float _popK = 1f;

            public void PopIn() => _popK = 0f;

            public void Punch(float amount) => _punch = Mathf.Max(_punch, amount);

            public void Attack(UnitKind kind, Vector2 dir)
            {
                _animT = 1f;
                _animKind = kind;
                _animDir = dir;
            }

            public void SetDragging(bool on)
            {
                _dragging = on;
                int o = on ? 60 : _baseOrder;
                _shadow.sortingOrder = o;
                _tile.sortingOrder = o + 1;
                _glyph.GetComponent<MeshRenderer>().sortingOrder = o + 2;
                _level.GetComponent<MeshRenderer>().sortingOrder = o + 3;
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
                Root.localPosition = Vector2.Lerp(Root.localPosition, Target, 1f - Mathf.Exp(-dt * (dragging ? 30f : 16f)));

                _punch = Mathf.Max(0, _punch - dt * 1.5f);
                _popK = Mathf.Min(1f, _popK + dt / 0.3f);
                float pop = _popK < 1f ? Tween.OutBack(_popK) : 1f;
                Root.localScale = Vector3.one * ((_dragging ? 1.12f : 1f) * (1f + Mathf.Sin(_punch * 20f) * _punch * 0.5f) * pop);

                Vector2 off = Vector2.zero;
                float rot = 0, sx = 1, sy = 1;
                if (_animT > 0)
                {
                    _animT = Mathf.Max(0, _animT - dt * 5f);
                    float k = Mathf.Sin(_animT * Mathf.PI);
                    switch (_animKind)
                    {
                        case UnitKind.Dao: rot = -35f * k; sx = 1f + 0.1f * k; break;
                        case UnitKind.Qiang: off = _animDir * 0.2f * k; sx = 1f + 0.25f * k * Mathf.Abs(_animDir.x); sy = 1f + 0.25f * k * Mathf.Abs(_animDir.y); break;
                        case UnitKind.Gong: sx = 1f - 0.22f * k; sy = 1f + 0.08f * k; break;
                        case UnitKind.Qi: off = _animDir * 0.3f * k; rot = -12f * k * Mathf.Sign(_animDir.x); break;
                    }
                }
                if (Piece.Type == PieceType.HeroChar && Hero == null && !_dragging)
                {
                    float breathe = Mathf.Sin(Time.time * 2f + Piece.Id) * 0.04f;
                    sx += breathe;
                    sy += breathe;
                }
                _glyphT.localPosition = new Vector2(0, -0.02f) + off;
                _glyphT.localRotation = Quaternion.Euler(0, 0, rot);
                _glyphT.localScale = new Vector3(sx, sy, 1);
            }

            public void FadeOutAndDestroy()
            {
                var root = Root;
                Tween.Run(root, 0.18f, k => { if (root) root.localScale = Vector3.one * (1 - k); }, () => { if (root) UnityEngine.Object.Destroy(root.gameObject); });
            }
        }

        sealed class FrameView
        {
            readonly SpriteRenderer _frame, _glow;
            readonly TextMesh _nick, _seal;
            readonly SpriteRenderer _sealBg;
            readonly Transform _root;
            public int Seen;
            float _pulse, _glowT;

            public FrameView(BattleView v, HeroInstance h, int side)
            {
                _root = new GameObject("hero" + h.Id).transform;
                _root.SetParent(v._root, false);
                _glow = Gfx.Sprite(_root, Art.Frame, Pal.GoldBright.A(0f), Vector2.zero, Vector2.one, 17);
                _frame = Gfx.Sprite(_root, Art.Frame, Pal.GoldBright, Vector2.zero, Vector2.one, 24);
                _nick = Gfx.Text(_root, h.Def.Nick, Fonts.Kai, 0.2f, Pal.RedDeep, Vector2.zero, 25);
                _sealBg = Gfx.Sprite(_root, Art.Rounded, Pal.Red, Vector2.zero, new Vector2(0.26f, 0.26f), 25);
                _seal = Gfx.Text(_root, "义", Fonts.Brush, 0.2f, Pal.Paper, Vector2.zero, 26);
                Place(h, side);
                Tween.Pop(_root, 0.4f);
                _glowT = 1.5f;
            }

            int _side;

            void Place(HeroInstance h, int side)
            {
                _side = side;
                Vector2 min = new Vector2(99, 99), max = new Vector2(-99, -99);
                foreach (var c in h.Cells)
                {
                    var p = CellWorld(side, c.c, c.r);
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
            }

            public void Update(HeroInstance h)
            {
                Place(h, _side);
                float dt = Time.deltaTime;
                _pulse = Mathf.Max(0, _pulse - dt * 4f);
                _glowT = Mathf.Max(0, _glowT - dt);
                float a = Mathf.Max(_pulse * 0.5f, Mathf.Min(1, _glowT) * 0.8f, 0.15f + 0.1f * Mathf.Sin(Time.time * 3f));
                _glow.color = Pal.GoldBright.A(a);
                _frame.color = Color.Lerp(Pal.Gold, Pal.GoldBright, 0.5f + 0.5f * Mathf.Sin(Time.time * 3f));
            }

            public void Pulse() => _pulse = 1f;
            public void Glow() => _glowT = 1f;

            public void Destroy() => UnityEngine.Object.Destroy(_root.gameObject);
        }

        sealed class EnemyView
        {
            public readonly Transform Root;
            public int Seen;
            readonly TextMesh _glyph;
            readonly SpriteRenderer _hpBg, _hpFg, _aura;
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
                _big = boss || elite;
                uint hash = (uint)e.Id * 2654435761u;
                _jitter = new Vector2(((hash >> 8) & 0xff) / 255f - 0.5f, ((hash >> 16) & 0xff) / 255f - 0.5f) * (_big ? 0f : 0.28f);
                _baseColor = boss ? Pal.RedDeep : elite ? Pal.Hex("3B2050") : Pal.Ink;
                if (boss || elite)
                    _aura = Gfx.Sprite(Root, Art.Blob, (boss ? Pal.Red : Pal.Hex("3B2050")).A(0.35f), Vector2.zero, Vector2.one * (boss ? 1.6f : 0.95f), 30);
                if (e.Def.Kind == EnemyKind.Jia)
                    _aura = Gfx.Sprite(Root, Art.Circle, Pal.InkSoft.A(0.25f), Vector2.zero, Vector2.one * 0.6f, 30);
                _glyph = Gfx.Text(Root, e.Glyph, Fonts.Brush, h, _baseColor, Vector2.zero, 32);
                _barW = boss ? 1.3f : elite ? 0.8f : 0.45f;
                float y = boss ? 0.55f : 0.4f;
                _hpBg = Gfx.Sprite(Root, Art.White, Pal.Ink.A(0.55f), new Vector2(0, y), new Vector2(_barW + 0.04f, 0.09f), 33);
                _hpFg = Gfx.Sprite(Root, Art.White, Pal.Hp, new Vector2(0, y), new Vector2(_barW, 0.06f), 34);
                Root.localScale = Vector3.one * 0.2f;
                Tween.Run(Root, 0.2f, k => { if (Root) Root.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, k); });
            }

            public void Flash() => _flash = 1f;

            public void Sync(Enemy e, Vector2 pos, float dt)
            {
                Root.localPosition = pos + _jitter + new Vector2(0, Mathf.Abs(Mathf.Sin(Time.time * 9f + e.Id)) * 0.04f);
                float k = Mathf.Clamp01(e.Hp / e.MaxHp);
                bool showBar = _big || k < 0.999f;
                _hpBg.enabled = _hpFg.enabled = showBar;
                _hpFg.transform.localScale = new Vector3(_barW * k, 0.06f, 1);
                _hpFg.transform.localPosition = new Vector2(-_barW * (1 - k) * 0.5f, _hpFg.transform.localPosition.y);
                _flash = Mathf.Max(0, _flash - dt * 8f);
                var c = e.StunT > 0 ? Color.Lerp(_baseColor, Pal.Hex("5B7FA6"), 0.7f) : _baseColor;
                _glyph.color = Color.Lerp(c, Pal.Red, _flash * 0.8f);
                _glyph.transform.localScale = Vector3.one * (1f + _flash * 0.15f);
            }
        }

        sealed class GoalView
        {
            readonly BattleView _v;
            readonly int _side;
            readonly Transform _root;
            readonly SpriteRenderer[] _hearts = new SpriteRenderer[3];
            readonly TextMesh _danger, _glyph;
            readonly SpriteRenderer _bubble;
            readonly TextMesh _bubbleText;
            readonly SpriteRenderer _bg;
            float _hurt;
            float _bubbleT;
            int _lastHearts = 3;

            public GoalView(BattleView v, int side)
            {
                _v = v;
                _side = side;
                var b = v.M.Sides[side].Board;
                var pos = CellWorld(side, b.Goal.c, b.Goal.r);
                _root = new GameObject("goal" + side).transform;
                _root.SetParent(v._root, false);
                _root.localPosition = pos;
                _bg = Gfx.Sprite(_root, Art.Rounded, Pal.RedDeep, Vector2.zero, new Vector2(0.95f, 0.95f), 25);
                Gfx.Sprite(_root, Art.Frame, Pal.Ink, Vector2.zero, new Vector2(0.98f, 0.98f), 26);
                _glyph = Gfx.Text(_root, "宋", Fonts.Brush, 0.7f, Pal.Paper, new Vector2(0, 0.02f), 27);
                float hy = side == 0 ? 0.7f : -0.7f;
                for (int i = 0; i < 3; i++)
                    _hearts[i] = Gfx.Sprite(_root, Art.Heart, Pal.Red, new Vector2((i - 1) * 0.3f, hy), new Vector2(0.28f, 0.28f), 28);
                _danger = Gfx.Text(_root, "危", Fonts.Brush, 0.6f, Pal.Red, new Vector2(-1.0f, 0), 29);
                _danger.gameObject.SetActive(false);
                float by = side == 0 ? 1.45f : -1.45f;
                _bubble = Gfx.Sprite(_root, Art.Rounded, Color.white, new Vector2(-1.2f, by), new Vector2(2.2f, 0.55f), 70);
                _bubbleText = Gfx.Text(_root, "", Fonts.Kai, 0.26f, Pal.Ink, new Vector2(-1.2f, by), 71);
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
                _bubble.enabled = true;
                _bubbleT = dur;
            }

            public void Sync()
            {
                var s = _v.M.Sides[_side];
                float dt = Time.deltaTime;
                for (int i = 0; i < 3; i++)
                {
                    float fill = Mathf.Clamp01((s.Hp - i * Tuning.HeartHp) / (float)Tuning.HeartHp);
                    _hearts[i].color = fill <= 0 ? Pal.Ink.A(0.25f) : Color.Lerp(Pal.Hex("E8A0A0"), Pal.Red, fill);
                    _hearts[i].transform.localScale = Vector3.one * (0.18f + 0.1f * fill) * (1f + _hurt * 0.4f);
                }
                _hurt = Mathf.Max(0, _hurt - dt * 3f);
                _bg.color = Color.Lerp(Pal.RedDeep, Pal.Red, _hurt);
                _glyph.transform.localPosition = new Vector2(Mathf.Sin(Time.time * 60f) * 0.06f * _hurt, 0.02f);

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
