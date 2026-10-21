using System;
using System.Collections.Generic;

namespace Rescue.Sim
{
    public struct CellPos : IEquatable<CellPos>
    {
        public int c, r;
        public CellPos(int c, int r) { this.c = c; this.r = r; }
        public bool Equals(CellPos o) => c == o.c && r == o.r;
        public override bool Equals(object o) => o is CellPos p && Equals(p);
        public override int GetHashCode() => c * 31 + r;
        public V2 Center => new V2(c + 0.5f, r + 0.5f);
    }

    public sealed class Board
    {
        public const int Cols = Tuning.Cols;
        public const int Rows = Tuning.Rows;

        public readonly Cell[,] Cells = new Cell[Cols, Rows];
        public readonly Piece[,] Pieces = new Piece[Cols, Rows];
        /// <summary>头目压下的乌云、贴上的封条，剩余秒数。盖住期间不能往里摆字。</summary>
        public readonly float[,] Fog = new float[Cols, Rows];
        public readonly float[,] Seal = new float[Cols, Rows];
        public readonly List<CellPos> Path = new List<CellPos>();
        public CellPos Spawn, Goal;
        public V2[] Waypoints;
        float[] _cum;
        public float PathLength;

        public Board(string[] map)
        {
            if (map.Length != Rows) throw new ArgumentException("地图行数应为 " + Rows);
            for (int i = 0; i < Rows; i++)
            {
                var line = map[i];
                if (line.Length != Cols) throw new ArgumentException($"地图第 {i} 行宽度应为 {Cols}：{line}");
                int r = Rows - 1 - i;
                for (int c = 0; c < Cols; c++)
                {
                    switch (line[c])
                    {
                        case 'S': Cells[c, r] = Cell.Path; Spawn = new CellPos(c, r); break;
                        case 'G': Cells[c, r] = Cell.Path; Goal = new CellPos(c, r); break;
                        case '#': Cells[c, r] = Cell.Path; break;
                        case 'o': Cells[c, r] = Cell.Tile; break;
                        case 'w': Cells[c, r] = Cell.Water; break;
                        case '~': Cells[c, r] = Cell.Marsh; break;
                        case 'x': Cells[c, r] = Cell.Rock; break;
                        default: Cells[c, r] = Cell.Grass; break;
                    }
                }
            }
            BuildPath();
        }

        void BuildPath()
        {
            var seen = new HashSet<CellPos>();
            var cur = Spawn;
            Path.Add(cur);
            seen.Add(cur);
            while (!cur.Equals(Goal))
            {
                CellPos next = default;
                int found = 0;
                foreach (var n in Neighbors4(cur))
                {
                    if (seen.Contains(n) || Cells[n.c, n.r] != Cell.Path) continue;
                    next = n;
                    found++;
                }
                if (found != 1) throw new InvalidOperationException($"路径在 ({cur.c},{cur.r}) 处断开或分叉（{found} 个出口）");
                cur = next;
                Path.Add(cur);
                seen.Add(cur);
            }

            Waypoints = new V2[Path.Count];
            _cum = new float[Path.Count];
            for (int i = 0; i < Path.Count; i++)
            {
                Waypoints[i] = Path[i].Center;
                if (i > 0) _cum[i] = _cum[i - 1] + V2.Dist(Waypoints[i - 1], Waypoints[i]);
            }
            PathLength = _cum[Path.Count - 1];
        }

        public V2 PosAt(float dist)
        {
            if (dist <= 0) return Waypoints[0];
            if (dist >= PathLength) return Waypoints[Waypoints.Length - 1];
            int i = 1;
            while (_cum[i] < dist) i++;
            float seg = _cum[i] - _cum[i - 1];
            return V2.Lerp(Waypoints[i - 1], Waypoints[i], (dist - _cum[i - 1]) / seg);
        }

        public static bool InBounds(int c, int r) => c >= 0 && c < Cols && r >= 0 && r < Rows;

        public bool Placeable(int c, int r) => Open(c, r) && Fog[c, r] <= 0 && Seal[c, r] <= 0;

        /// <summary>开好的地，不管头目有没有盖住。</summary>
        public bool Open(int c, int r) => InBounds(c, r) && (Cells[c, r] == Cell.Tile || Cells[c, r] == Cell.Water);

        public bool Blocked(int c, int r) => InBounds(c, r) && (Fog[c, r] > 0 || Seal[c, r] > 0);

        /// <summary>草地、苇荡要先用方便铲开出来才能摆字。</summary>
        public bool Diggable(int c, int r) => InBounds(c, r) && (Cells[c, r] == Cell.Grass || Cells[c, r] == Cell.Marsh);

        public bool Dig(int c, int r)
        {
            if (!Diggable(c, r)) return false;
            Cells[c, r] = Cells[c, r] == Cell.Marsh ? Cell.Water : Cell.Tile;
            return true;
        }

        public bool AnyDiggable()
        {
            for (int c = 0; c < Cols; c++)
                for (int r = 0; r < Rows; r++)
                    if (Diggable(c, r)) return true;
            return false;
        }

        public int PlaceableCount
        {
            get
            {
                int n = 0;
                for (int c = 0; c < Cols; c++)
                    for (int r = 0; r < Rows; r++)
                        if (Open(c, r)) n++;
                return n;
            }
        }

        public IEnumerable<CellPos> Neighbors4(CellPos p)
        {
            if (p.c > 0) yield return new CellPos(p.c - 1, p.r);
            if (p.c < Cols - 1) yield return new CellPos(p.c + 1, p.r);
            if (p.r > 0) yield return new CellPos(p.c, p.r - 1);
            if (p.r < Rows - 1) yield return new CellPos(p.c, p.r + 1);
        }

    }

    public sealed class HeroInstance
    {
        static int _nextId = 1;

        public readonly int Id = _nextId++;
        public HeroDef Def;
        public CellPos[] Cells;
        public Piece[] Pieces;
        public int Level;
        public float Cd;
        public float UltCd;
        public bool OnWater;
        public float Xp;

        public static int XpNeed(int level) => 10 + 12 * level;

        public V2 Pos
        {
            get
            {
                var p = new V2(0, 0);
                foreach (var c in Cells) p = p + c.Center;
                return p * (1f / Cells.Length);
            }
        }
    }

    public static class NameMatcher
    {
        /// <summary>
        /// 横向按屏幕从左到右、纵向按屏幕从上到下读名字。对手半区旋转了 180°，所以读向在它自己的坐标里是反的。
        /// </summary>
        public static void ReadDirs(bool mirrored, out CellPos horizontal, out CellPos vertical)
        {
            horizontal = mirrored ? new CellPos(-1, 0) : new CellPos(1, 0);
            vertical = mirrored ? new CellPos(0, 1) : new CellPos(0, -1);
        }

        public static List<HeroInstance> Match(Board b, IList<HeroDef> pool, bool mirrored, List<HeroInstance> previous)
        {
            var result = new List<HeroInstance>();
            var used = new bool[Board.Cols, Board.Rows];

            if (previous != null)
            {
                foreach (var h in previous)
                {
                    bool intact = true;
                    for (int i = 0; i < h.Cells.Length && intact; i++)
                    {
                        var p = b.Pieces[h.Cells[i].c, h.Cells[i].r];
                        intact = p != null && ReferenceEquals(p, h.Pieces[i]) && p.Char == h.Def.Name[i];
                    }
                    if (!intact) continue;
                    foreach (var c in h.Cells) used[c.c, c.r] = true;
                    h.Level = MinLevel(h.Pieces);
                    result.Add(h);
                }
            }

            var defs = new List<HeroDef>(pool);
            defs.Sort((x, y) => y.Name.Length.CompareTo(x.Name.Length));
            ReadDirs(mirrored, out var dh, out var dv);
            var dirs = new[] { dh, dv };

            foreach (var def in defs)
            {
                for (int r = Board.Rows - 1; r >= 0; r--)
                {
                    for (int c = 0; c < Board.Cols; c++)
                    {
                        foreach (var d in dirs)
                        {
                            var cells = TryAt(b, used, def.Name, c, r, d);
                            if (cells == null) continue;
                            var pieces = new Piece[cells.Length];
                            for (int i = 0; i < cells.Length; i++)
                            {
                                pieces[i] = b.Pieces[cells[i].c, cells[i].r];
                                used[cells[i].c, cells[i].r] = true;
                            }
                            result.Add(new HeroInstance
                            {
                                Def = def, Cells = cells, Pieces = pieces, Level = MinLevel(pieces),
                                UltCd = def.UltCooldown * 0.4f,
                            });
                        }
                    }
                }
            }

            foreach (var h in result)
            {
                h.OnWater = false;
                foreach (var c in h.Cells) if (b.Cells[c.c, c.r] == Cell.Water) h.OnWater = true;
            }
            return result;
        }

        static CellPos[] TryAt(Board b, bool[,] used, string name, int c, int r, CellPos d)
        {
            var cells = new CellPos[name.Length];
            for (int i = 0; i < name.Length; i++)
            {
                int cc = c + d.c * i, rr = r + d.r * i;
                if (!Board.InBounds(cc, rr) || used[cc, rr]) return null;
                var p = b.Pieces[cc, rr];
                if (p == null || p.Type != PieceType.HeroChar || p.Char != name[i]) return null;
                cells[i] = new CellPos(cc, rr);
            }
            return cells;
        }

        static int MinLevel(Piece[] ps)
        {
            int m = int.MaxValue;
            foreach (var p in ps) m = Math.Min(m, p.Level);
            return m;
        }
    }
}
