using System;
using System.Collections.Generic;

namespace Rescue.Sim
{
    /// <summary>对手：只通过和玩家相同的操作接口下棋。skill 0~1 决定思考间隔和失误率。</summary>
    public sealed class RivalAI
    {
        readonly Side _s;
        readonly float _skill;
        readonly Rng _rng;
        float _think = 1.5f;
        readonly float[][,] _coverage = new float[4][,];

        public RivalAI(Side side, float skill, uint seed)
        {
            _s = side;
            _skill = Math.Max(0, Math.Min(1, skill));
            _rng = new Rng(seed);
            for (int k = 0; k < 4; k++) _coverage[k] = BuildCoverage(UnitDef.Get((UnitKind)k).Range);
        }

        float[,] BuildCoverage(float range)
        {
            var b = _s.Board;
            var map = new float[Board.Cols, Board.Rows];
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    var p = new CellPos(c, r).Center;
                    float s = 0;
                    for (int i = 0; i < b.Waypoints.Length; i++)
                        if (V2.Dist(p, b.Waypoints[i]) <= range) s += 1f + 0.6f * i / b.Waypoints.Length;
                    map[c, r] = s;
                }
            return map;
        }

        public void Tick(float dt)
        {
            if (_s.Dead || _s.M.Over) return;
            _think -= dt;
            if (_think > 0) return;
            float baseT = 1.7f - 1.25f * _skill;
            _think = baseT * (0.75f + 0.5f * _rng.Next01());
            Act();
        }

        void Act()
        {
            if (TryDig()) return;
            if (_rng.Chance(0.15f + 0.85f * _skill) && TryMergeCamp()) return;
            for (int i = 0; i < _s.Camp.Length; i++)
            {
                var p = _s.Camp[i];
                if (p == null) continue;
                if (p.Type == PieceType.Unit && TryPlaceUnit(i, p)) return;
                if (p.Type == PieceType.HeroChar && TryPlaceChar(i, p)) return;
            }
            // 一次聚义填满所有空位且价格递增：高手等山寨快空了再按，新手见空就按
            int empty = _s.EmptyCampSlots;
            int wantEmpty = _skill > 0.5f ? 4 : 2;
            if (_s.CanRecruit && empty >= wantEmpty) { _s.Recruit(); return; }
            if (_rng.Chance(0.3f + 0.7f * _skill) && TryMergeBoard()) return;
            if (_rng.Chance(_skill) && TryUpgradeSwap()) return;
            if (_rng.Chance(_skill) && TryClearOrphan()) return;
            if (_s.CanRecruit && empty > 0 && (_s.Buns >= _s.RecruitCost * 2 || _rng.Chance(0.5f * (1f - _skill)))) { _s.Recruit(); return; }
            if (empty == 0 && _rng.Chance(0.2f + 0.8f * _skill)) TryDismiss();
        }

        /// <summary>山寨堵死时遣散最没用的字（高手挑，新手随手扔）。</summary>
        void TryDismiss()
        {
            int pick = -1;
            float worst = float.MaxValue;
            for (int i = 0; i < _s.Camp.Length; i++)
            {
                var p = _s.Camp[i];
                if (p == null) continue;
                float v = p.Type == PieceType.Shovel ? 100 : p.Type == PieceType.HeroChar ? (BestCharScore(p.Char) >= 10 ? 25 : 5) : p.Level * 10;
                v += _rng.Next01() * (1f - _skill) * 40f;
                if (v < worst) { worst = v; pick = i; }
            }
            if (pick >= 0) _s.DismissCamp(pick);
        }

        /// <summary>有铲子就往离路最近的草地开荒；新手挑得随便。</summary>
        bool TryDig()
        {
            var b = _s.Board;
            for (int i = 0; i < _s.Camp.Length; i++)
            {
                var p = _s.Camp[i];
                if (p == null || p.Type != PieceType.Shovel) continue;
                int bc = -1, br = -1;
                float best = -1;
                for (int c = 0; c < Board.Cols; c++)
                    for (int r = 0; r < Board.Rows; r++)
                    {
                        if (!b.Diggable(c, r)) continue;
                        float s = _coverage[0][c, r] + _coverage[2][c, r] * 0.5f + _rng.Next01() * (1f - _skill) * 15f;
                        if (s > best) { best = s; bc = c; br = r; }
                    }
                if (bc >= 0) return _s.CampToCell(i, bc, br);
                _s.DismissCamp(i);
                return true;
            }
            return false;
        }

        bool TryPlaceUnit(int slot, Piece p)
        {
            var b = _s.Board;
            if (_rng.Chance(0.25f + 0.75f * _skill))
                for (int c = 0; c < Board.Cols; c++)
                    for (int r = 0; r < Board.Rows; r++)
                        if (Side.CanMerge(p, b.Pieces[c, r])) return _s.CampToCell(slot, c, r);

            var cov = _coverage[(int)p.Kind];
            int bc = -1, br = -1;
            float best = -1;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    if (!b.Placeable(c, r) || b.Pieces[c, r] != null) continue;
                    float s = cov[c, r] + _rng.Next01() * (1f - _skill) * 12f;
                    if (s > best) { best = s; bc = c; br = r; }
                }
            return bc >= 0 && _s.CampToCell(slot, bc, br);
        }

        bool TryPlaceChar(int slot, Piece p)
        {
            var b = _s.Board;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    if (Side.CanMerge(p, b.Pieces[c, r]) && _s.HeroAt(c, r) != null) return _s.CampToCell(slot, c, r);

            bool smart = _rng.Chance(0.35f + 0.65f * _skill);
            if (smart)
            {
                NameMatcher.ReadDirs(_s.Mirrored, out var dh, out var dv);
                int bestScore = -1;
                CellPos bestCell = default;
                foreach (var def in _s.Pool)
                {
                    if (def.Name.IndexOf(p.Char) < 0) continue;
                    foreach (var d in new[] { dh, dv })
                        for (int c = 0; c < Board.Cols; c++)
                            for (int r = 0; r < Board.Rows; r++)
                            {
                                int score = ScorePlacement(def.Name, p.Char, c, r, d, out var at);
                                if (score > bestScore) { bestScore = score; bestCell = at; }
                            }
                }
                if (bestScore >= 0) return _s.CampToCell(slot, bestCell.c, bestCell.r);
            }

            if (_s.EmptyCampSlots > 0 || _rng.Chance(_skill)) return false;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    if (b.Placeable(c, r) && b.Pieces[c, r] == null) return _s.CampToCell(slot, c, r);
            return false;
        }

        int ScorePlacement(string name, char ch, int c, int r, CellPos d, out CellPos at)
        {
            at = default;
            var b = _s.Board;
            int have = 0;
            bool slotFound = false;
            for (int i = 0; i < name.Length; i++)
            {
                int cc = c + d.c * i, rr = r + d.r * i;
                if (!b.Placeable(cc, rr)) return -1;
                var q = b.Pieces[cc, rr];
                if (q == null)
                {
                    if (name[i] == ch && !slotFound) { slotFound = true; at = new CellPos(cc, rr); }
                    continue;
                }
                if (q.Type == PieceType.HeroChar && q.Char == name[i] && _s.HeroAt(cc, rr) == null) { have++; continue; }
                return -1;
            }
            if (!slotFound) return -1;
            return have * 10 + (int)_coverage[0][at.c, at.r];
        }

        int BestCharScore(char ch)
        {
            NameMatcher.ReadDirs(_s.Mirrored, out var dh, out var dv);
            int best = -1;
            foreach (var def in _s.Pool)
            {
                if (def.Name.IndexOf(ch) < 0) continue;
                foreach (var d in new[] { dh, dv })
                    for (int c = 0; c < Board.Cols; c++)
                        for (int r = 0; r < Board.Rows; r++)
                            best = Math.Max(best, ScorePlacement(def.Name, ch, c, r, d, out _));
            }
            return best;
        }

        /// <summary>棋盘满了：清掉场上凑不成名字的孤字，给兵腾地方。</summary>
        bool TryClearOrphan()
        {
            var b = _s.Board;
            bool campHasUnit = false;
            foreach (var p in _s.Camp) if (p != null && p.Type == PieceType.Unit) campHasUnit = true;
            if (!campHasUnit) return false;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    if (b.Placeable(c, r) && b.Pieces[c, r] == null) return false;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    var q = b.Pieces[c, r];
                    if (q == null || q.Type != PieceType.HeroChar || _s.HeroAt(c, r) != null) continue;
                    bool partner = false;
                    foreach (var n in b.Neighbors4(new CellPos(c, r)))
                    {
                        var o = b.Pieces[n.c, n.r];
                        if (o == null || o.Type != PieceType.HeroChar) continue;
                        foreach (var def in _s.Pool)
                            if (def.Name.IndexOf(q.Char) >= 0 && def.Name.IndexOf(o.Char) >= 0) partner = true;
                    }
                    if (!partner) return _s.DismissCell(c, r);
                }
            return false;
        }

        bool TryMergeCamp()
        {
            var camp = _s.Camp;
            for (int i = 0; i < camp.Length; i++)
                for (int j = i + 1; j < camp.Length; j++)
                    if (camp[i] != null && camp[i].Type == PieceType.Unit && Side.CanMerge(camp[i], camp[j]))
                        return _s.CampToCamp(i, j);
            return false;
        }

        /// <summary>棋盘满了：用山寨里更高级的兵换下场上同类里最弱的那个。</summary>
        bool TryUpgradeSwap()
        {
            var b = _s.Board;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    if (b.Placeable(c, r) && b.Pieces[c, r] == null) return false;
            for (int i = 0; i < _s.Camp.Length; i++)
            {
                var p = _s.Camp[i];
                if (p == null || p.Type != PieceType.Unit) continue;
                int bc = -1, br = -1, worst = p.Level;
                for (int c = 0; c < Board.Cols; c++)
                    for (int r = 0; r < Board.Rows; r++)
                    {
                        var q = b.Pieces[c, r];
                        if (q == null || q.Type != PieceType.Unit || q.Level >= worst) continue;
                        worst = q.Level; bc = c; br = r;
                    }
                if (bc >= 0) return _s.CampToCell(i, bc, br);
            }
            return false;
        }

        bool TryMergeBoard()
        {
            var b = _s.Board;
            var list = new List<CellPos>();
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    if (b.Pieces[c, r] != null && b.Pieces[c, r].Type == PieceType.Unit) list.Add(new CellPos(c, r));
            list.Sort((x, y) => b.Pieces[x.c, x.r].Level.CompareTo(b.Pieces[y.c, y.r].Level));
            for (int i = 0; i < list.Count; i++)
                for (int j = 0; j < list.Count; j++)
                {
                    if (i == j) continue;
                    var a = list[i];
                    var t = list[j];
                    if (!Side.CanMerge(b.Pieces[a.c, a.r], b.Pieces[t.c, t.r])) continue;
                    var cov = _coverage[(int)b.Pieces[a.c, a.r].Kind];
                    if (cov[a.c, a.r] > cov[t.c, t.r]) { var tmp = a; a = t; t = tmp; }
                    return _s.CellToCell(a.c, a.r, t.c, t.r);
                }
            return false;
        }
    }
}
