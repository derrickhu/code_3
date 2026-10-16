using NUnit.Framework;
using Rescue.Sim;

namespace Rescue.Tests
{
    public class SimTests
    {
        static Match NewMatch(int chapter = 0) => new Match(ChapterDef.All[chapter], 42);
        static Match PoolMatch(params HeroId[] pool) => new Match(ChapterDef.All[0].WithPool(pool), 42);

        static ChapterDef Named(string name)
        {
            foreach (var ch in ChapterDef.All) if (ch.Name == name) return ch;
            Assert.Fail("没有这一回：" + name);
            return null;
        }

        static void Clear(Side s)
        {
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    s.Board.Pieces[c, r] = null;
            for (int i = 0; i < s.Camp.Length; i++) s.Camp[i] = null;
        }

        static void OpenAll(Side s)
        {
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    if (s.Board.Cells[c, r] == Cell.Grass) s.Board.Cells[c, r] = Cell.Tile;
                    else if (s.Board.Cells[c, r] == Cell.Marsh) s.Board.Cells[c, r] = Cell.Water;
                }
        }

        static CellPos FindRun(Side s, int len, CellPos dir)
        {
            OpenAll(s);
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    bool ok = true;
                    for (int i = 0; i < len && ok; i++) ok = s.Board.Placeable(c + dir.c * i, r + dir.r * i);
                    if (ok) return new CellPos(c, r);
                }
            Assert.Fail("地图上找不到连续 " + len + " 格可放位置");
            return default;
        }

        [Test]
        public void AllChapterMapsHaveContinuousPath()
        {
            foreach (var ch in ChapterDef.All)
            {
                var b = new Board(ch.Map);
                Assert.Greater(b.Path.Count, 10, ch.Name);
                Assert.AreEqual(b.Goal, b.Path[b.Path.Count - 1], ch.Name);
                int start = b.PlaceableCount, potential = 0;
                for (int c = 0; c < Board.Cols; c++)
                    for (int r = 0; r < Board.Rows; r++)
                        if (b.Cells[c, r] != Cell.Path && b.Cells[c, r] != Cell.Rock) potential++;
                Assert.That(start, Is.InRange(5, 8), ch.Name + " 开局白地应是一小块");
                Assert.GreaterOrEqual(potential, 18, ch.Name + " 能开荒的地太少");
                Assert.IsTrue(b.AnyDiggable(), ch.Name);
            }
        }

        [Test]
        public void SameKindSameLevelMerges_OtherwiseSwaps()
        {
            var s = NewMatch().Player;
            Clear(s);
            var at = FindRun(s, 2, new CellPos(1, 0));
            s.Board.Pieces[at.c, at.r] = Piece.Unit(UnitKind.Dao);
            s.Camp[0] = Piece.Unit(UnitKind.Dao);
            Assert.IsTrue(s.CampToCell(0, at.c, at.r));
            Assert.AreEqual(2, s.Board.Pieces[at.c, at.r].Level);
            Assert.IsNull(s.Camp[0]);

            s.Camp[1] = Piece.Unit(UnitKind.Gong);
            Assert.IsTrue(s.CampToCell(1, at.c, at.r));
            Assert.AreEqual(UnitKind.Gong, s.Board.Pieces[at.c, at.r].Kind);
            Assert.AreEqual(UnitKind.Dao, s.Camp[1].Kind);
        }

        [Test]
        public void MaxLevelDoesNotMerge()
        {
            Assert.IsFalse(Side.CanMerge(Piece.Unit(UnitKind.Qi, UnitDef.MaxLevel), Piece.Unit(UnitKind.Qi, UnitDef.MaxLevel)));
            Assert.IsTrue(Side.CanMerge(Piece.Hero('武'), Piece.Hero('武')));
            Assert.IsFalse(Side.CanMerge(Piece.Hero('武'), Piece.Hero('松')));
            Assert.IsFalse(Side.CanMerge(Piece.Shovel(), Piece.Shovel()));
        }

        [Test]
        public void HeroFormsOnlyInReadingOrder()
        {
            var s = PoolMatch(HeroId.WuSong).Player;
            Clear(s);
            var at = FindRun(s, 2, new CellPos(1, 0));
            s.Board.Pieces[at.c, at.r] = Piece.Hero('松');
            s.Board.Pieces[at.c + 1, at.r] = Piece.Hero('武');
            s.RecomputeHeroes();
            Assert.AreEqual(0, s.Heroes.Count, "松武 不应成名");

            s.CellToCell(at.c, at.r, at.c + 1, at.r);
            Assert.AreEqual(1, s.Heroes.Count);
            Assert.AreEqual(HeroId.WuSong, s.Heroes[0].Def.Id);
        }

        [Test]
        public void VerticalNameReadsTopToBottom()
        {
            var s = new Match(Named("清风寨").WithPool(HeroId.HuaRong), 42).Player;
            Clear(s);
            var at = FindRun(s, 2, new CellPos(0, -1));
            s.Board.Pieces[at.c, at.r] = Piece.Hero('花');
            s.Board.Pieces[at.c, at.r - 1] = Piece.Hero('荣');
            s.RecomputeHeroes();
            Assert.AreEqual(1, s.Heroes.Count);
            Assert.AreEqual(HeroId.HuaRong, s.Heroes[0].Def.Id);
        }

        [Test]
        public void MirroredSideReadsInScreenOrder()
        {
            var s = PoolMatch(HeroId.HuaRong).Rival;
            Clear(s);
            var at = FindRun(s, 2, new CellPos(-1, 0));
            s.Board.Pieces[at.c, at.r] = Piece.Hero('花');
            s.Board.Pieces[at.c - 1, at.r] = Piece.Hero('荣');
            s.RecomputeHeroes();
            Assert.AreEqual(1, s.Heroes.Count);
        }

        [Test]
        public void SharedSurnameServesOnlyOneHero()
        {
            var s = PoolMatch(HeroId.ZhangShun, HeroId.ZhangHeng).Player;
            Clear(s);
            var at = FindRun(s, 3, new CellPos(1, 0));
            s.Board.Pieces[at.c + 1, at.r] = Piece.Hero('张');
            s.Board.Pieces[at.c + 2, at.r] = Piece.Hero('顺');
            s.RecomputeHeroes();
            Assert.AreEqual(HeroId.ZhangShun, s.Heroes[0].Def.Id);

            s.Board.Pieces[at.c, at.r] = Piece.Hero('横');
            s.Board.Pieces[at.c + 2, at.r] = Piece.Hero('横');
            s.RecomputeHeroes();
            Assert.AreEqual(1, s.Heroes.Count);
            Assert.AreEqual(HeroId.ZhangHeng, s.Heroes[0].Def.Id, "张字换了下家");
        }

        [Test]
        public void ThreeCharNameForms()
        {
            var s = PoolMatch(HeroId.RuanXiaoQi).Player;
            Clear(s);
            var at = FindRun(s, 3, new CellPos(1, 0));
            s.Board.Pieces[at.c, at.r] = Piece.Hero('阮');
            s.Board.Pieces[at.c + 1, at.r] = Piece.Hero('小');
            s.Board.Pieces[at.c + 2, at.r] = Piece.Hero('七');
            s.RecomputeHeroes();
            Assert.AreEqual(1, s.Heroes.Count);
            Assert.AreEqual(HeroId.RuanXiaoQi, s.Heroes[0].Def.Id);
            Assert.AreEqual(3, s.Heroes[0].Cells.Length);
        }

        [Test]
        public void HeroSurvivesUnrelatedChanges_AndLevelIsMinOfChars()
        {
            var s = PoolMatch(HeroId.WuSong).Player;
            Clear(s);
            var at = FindRun(s, 2, new CellPos(1, 0));
            s.Board.Pieces[at.c, at.r] = Piece.Hero('武', 2);
            s.Board.Pieces[at.c + 1, at.r] = Piece.Hero('松', 1);
            s.RecomputeHeroes();
            var h = s.Heroes[0];
            Assert.AreEqual(1, h.Level);
            s.Camp[0] = Piece.Hero('松', 1);
            s.CampToCell(0, at.c + 1, at.r);
            Assert.AreSame(h, s.Heroes[0], "合成后应仍是同一个好汉实例");
            Assert.AreEqual(2, s.Heroes[0].Level);
        }

        [Test]
        public void RecruitFillsEmptySlotsAndCostRises()
        {
            var s = NewMatch().Player;
            int buns = s.Buns, cost = s.RecruitCost;
            Assert.IsTrue(s.Recruit());
            Assert.AreEqual(0, s.EmptyCampSlots);
            Assert.AreEqual(buns - cost, s.Buns);
            Assert.AreEqual(cost + Tuning.CostStep, s.RecruitCost);
            Assert.IsFalse(s.Recruit(), "山寨满了不能聚义");
        }

        [Test]
        public void GoldPityGuaranteesHeroChar()
        {
            var s = NewMatch().Player;
            s.Buns = 100000;
            int gold = 0;
            for (int i = 0; i < 12; i++)
            {
                for (int k = 0; k < s.Camp.Length; k++) s.Camp[k] = null;
                s.Recruit();
                foreach (var p in s.Camp) if (p.Type == PieceType.HeroChar) gold++;
            }
            Assert.GreaterOrEqual(gold, 12 * 5 / (Tuning.GoldPity + 1));
        }

        [Test]
        public void ShovelDigsGrassIntoTile()
        {
            var s = NewMatch().Player;
            Clear(s);
            CellPos near = default, tile = default;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    if (s.Board.Cells[c, r] == Cell.Grass) near = new CellPos(c, r);
                    if (s.Board.Cells[c, r] == Cell.Tile) tile = new CellPos(c, r);
                }
            Assert.IsFalse(s.Board.Placeable(near.c, near.r), "草地开荒前不能摆");
            s.Camp[1] = Piece.Unit(UnitKind.Dao);
            Assert.IsFalse(s.CampToCell(1, near.c, near.r));

            s.Camp[0] = Piece.Shovel();
            Assert.IsFalse(s.CampToCell(0, s.Board.Spawn.c, s.Board.Spawn.r), "路不能挖");
            Assert.IsFalse(s.CampToCell(0, tile.c, tile.r), "白地不用再挖");
            int before = s.Board.PlaceableCount;
            Assert.IsTrue(s.CampToCell(0, near.c, near.r));
            Assert.IsNull(s.Camp[0]);
            Assert.AreEqual(Cell.Tile, s.Board.Cells[near.c, near.r]);
            Assert.AreEqual(before + 1, s.Board.PlaceableCount);
        }

        [Test]
        public void AdShovelsOverflowIntoStock()
        {
            var s = NewMatch().Player;
            Clear(s);
            for (int i = 1; i < s.Camp.Length; i++) s.Camp[i] = Piece.Unit(UnitKind.Dao);
            s.GrantShovels(Tuning.AdShovels);
            Assert.AreEqual(PieceType.Shovel, s.Camp[0].Type);
            Assert.AreEqual(1, s.ShovelStock);
            s.DismissCamp(1);
            Assert.AreEqual(PieceType.Shovel, s.Camp[1].Type, "空出来的位置自动补铲子");
            Assert.AreEqual(0, s.ShovelStock);
        }

        [Test]
        public void HeroGainsXpAndLevelsUpAllChars()
        {
            var m = PoolMatch(HeroId.WuSong);
            var s = m.Player;
            Clear(s);
            var at = FindRun(s, 2, new CellPos(1, 0));
            s.Board.Pieces[at.c, at.r] = Piece.Hero('武');
            s.Board.Pieces[at.c + 1, at.r] = Piece.Hero('松');
            s.RecomputeHeroes();
            var h = s.Heroes[0];
            h.UltCd = 999;
            h.Xp = HeroInstance.XpNeed(1) - 0.1f;
            m.SpawnOn(s, EnemyKind.Zu, 0);
            s.Enemies[0].Pos = h.Pos;
            s.Enemies[0].StunT = 999;
            for (int i = 0; i < 30 && h.Level == 1; i++) m.Step(0.05f);
            Assert.AreEqual(2, h.Level);
            Assert.AreEqual(2, s.Board.Pieces[at.c, at.r].Level);
            Assert.AreEqual(2, s.Board.Pieces[at.c + 1, at.r].Level);
        }

        [Test]
        public void RescueClearsGruntsAndShieldsGoal()
        {
            var m = NewMatch();
            var s = m.Player;
            for (int i = 0; i < 4; i++) m.SpawnOn(s, EnemyKind.Zu, s.Board.PathLength - 0.5f);
            Assert.IsTrue(s.InDanger);
            Assert.IsTrue(s.Rescue());
            Assert.IsFalse(s.Rescue(), "一局只能请一次");
            foreach (var e in s.Enemies) Assert.IsTrue(e.Dead);
            m.Step(0.02f);
            int hp = s.Hp;
            m.SpawnOn(s, EnemyKind.Zu, s.Board.PathLength - 0.01f);
            m.Step(0.05f);
            Assert.AreEqual(hp, s.Hp, "护身期间不掉血");
        }

        [Test]
        public void BaoziMovesOneHeartEitherWay()
        {
            var s = new Match(ChapterDef.All[0], 42, new Loadout { Baozi = 1 }).Player;
            s.Hp = 15;
            Assert.IsTrue(s.UseBaozi());
            Assert.That(s.Hp == 25 || s.Hp == 5, "包子只会回或掉一颗心");
            Assert.IsFalse(s.UseBaozi());
        }

        [Test]
        public void XumingRaisesBothSidesUnevenly()
        {
            var m = new Match(ChapterDef.All[0], 42, new Loadout { Xuming = true });
            Assert.AreEqual(Tuning.MaxHp + 10, m.Player.Hp);
            Assert.AreEqual(Tuning.MaxHp + 6, m.Rival.Hp);
            Assert.AreEqual(4, m.Player.MaxHearts);
        }

        [Test]
        public void LosingAHeartTriggersTimelyRain()
        {
            var m = NewMatch();
            var s = m.Player;
            s.Hp = Tuning.HeartHp * 2 + 1;
            int buns = s.Buns;
            for (int i = 0; i < 2; i++) m.SpawnOn(s, EnemyKind.Zu, s.Board.PathLength - 0.01f);
            m.Step(0.05f);
            Assert.AreEqual(Tuning.HeartHp * 2 - 1, s.Hp);
            Assert.AreEqual(1, s.Rains);
            Assert.Greater(s.Buns, buns);
        }

        [Test]
        public void NemesisDealsFiveTimesToBoss()
        {
            var ch = ChapterDef.All[0].WithPool(HeroId.HuaRong);
            ch.Boss = new BossDef { Name = ch.Boss.Name, Title = ch.Boss.Title, Skill = ch.Boss.Skill, Nemesis = HeroId.HuaRong };
            var m = new Match(ch, 42);
            var s = m.Player;
            Clear(s);
            m.SpawnOn(s, EnemyKind.Boss, 3f);
            var boss = s.Enemies[0];
            float hp = boss.Hp;
            var at = FindRun(s, 2, new CellPos(1, 0));
            s.Board.Pieces[at.c, at.r] = Piece.Hero('花');
            s.Board.Pieces[at.c + 1, at.r] = Piece.Hero('荣');
            s.RecomputeHeroes();
            s.Heroes[0].UltCd = 999;
            m.Step(0.02f);
            float expected = HeroDef.Get(HeroId.HuaRong).DamageAt(1) * Tuning.BossNemesisMul;
            Assert.AreEqual(hp - expected, boss.Hp, 0.01f);
        }

        [Test]
        public void AiVsAiMatchesEndInReasonableTime([Values(0, 1, 2, 24, 40, 56)] int chapter)
        {
            var m = new Match(ChapterDef.All[chapter], 7);
            var a = new RivalAI(m.Player, 0.8f, 11);
            var b = new RivalAI(m.Rival, 0.8f, 23);
            const float dt = 1f / 30f;
            while (!m.Over && m.Time < 20 * 60)
            {
                a.Tick(dt);
                b.Tick(dt);
                m.Step(dt);
                m.Events.Clear();
            }
            UnityEngine.Debug.Log($"[平衡] {m.Chapter.Name}: {m.Time / 60f:0.0} 分钟, 第 {m.Wave} 波, 胜者 {m.Winner}, " +
                $"好汉 {m.Player.FormedEver.Count}/{m.Rival.FormedEver.Count}, 聚义 {m.Player.Recruits}/{m.Rival.Recruits}, 及时雨 {m.Player.Rains}/{m.Rival.Rains}");
            Assert.IsTrue(m.Over, "20 分钟还没分出胜负");
            Assert.Greater(m.Time, 150f, "对局太短");
            Assert.Less(m.Time, 12 * 60f, "对局太长");
        }

        [Test]
        public void FirstChapterRivalIsBeatable()
        {
            int wins = 0;
            for (uint seed = 1; seed <= 8; seed++)
            {
                var m = new Match(ChapterDef.All[0], seed);
                var player = new RivalAI(m.Player, 0.6f, seed * 3);
                var rival = new RivalAI(m.Rival, ChapterDef.All[0].RivalSkill, seed * 5);
                while (!m.Over && m.Time < 20 * 60)
                {
                    player.Tick(1f / 30f);
                    rival.Tick(1f / 30f);
                    m.Step(1f / 30f);
                    m.Events.Clear();
                }
                if (m.Winner == 0) wins++;
            }
            UnityEngine.Debug.Log($"[平衡] 第一回 中等玩家胜率 {wins}/8");
            Assert.GreaterOrEqual(wins, 5);
        }

        [Test]
        public void RankStarsPromoteAndNeverLeaveMajor()
        {
            int tier = 0, stars = 0;
            RankRules.Apply(ref tier, ref stars, false, out bool up, out bool down);
            Assert.AreEqual(0, tier, "新手败了不掉");
            Assert.IsFalse(down);

            for (int i = 0; i < RankRules.StarsAt(0); i++) RankRules.Apply(ref tier, ref stars, true, out up, out down);
            Assert.AreEqual(RankRules.StarsAt(0), stars);
            RankRules.Apply(ref tier, ref stars, true, out up, out down);
            Assert.IsTrue(up);
            Assert.AreEqual(1, tier);
            Assert.AreEqual(0, stars);
            Assert.AreEqual("喽啰·贰", RankRules.Name(tier));

            tier = 4 * RankRules.SubPerMajor + 1;
            stars = 0;
            RankRules.Apply(ref tier, ref stars, false, out up, out down);
            Assert.IsTrue(down);
            Assert.AreEqual(4 * RankRules.SubPerMajor, tier);
            Assert.AreEqual(RankRules.StarsAt(tier) - 1, stars);

            stars = 0;
            RankRules.Apply(ref tier, ref stars, false, out up, out down);
            Assert.IsFalse(down, "大阶最低一小阶不再往下掉");
            Assert.AreEqual(4 * RankRules.SubPerMajor, tier);

            tier = RankRules.Top;
            stars = 40;
            RankRules.Apply(ref tier, ref stars, true, out up, out down);
            Assert.AreEqual(RankRules.Top, tier, "天魁星封顶");
            Assert.AreEqual(41, stars);
            Assert.AreEqual("天魁星", RankRules.Name(tier));

            Assert.AreEqual(12, RankRules.Majors.Length);
            Assert.AreEqual(RankRules.Count, RankRules.Top + 1);
            for (int t = 0; t + 1 < RankRules.Count; t++)
                Assert.Greater(RankRules.Score(t + 1, 0), RankRules.Score(t, RankRules.StarsAt(t)), "升阶分必须高过满星");
            Assert.AreEqual(0, RankRules.FromLegacy(0));
            Assert.AreEqual(10 * RankRules.SubPerMajor, RankRules.FromLegacy(6));
        }

        [Test]
        public void HundredEightHeroesHaveUniqueRanks()
        {
            var seen = new bool[109];
            int ranked = 0;
            foreach (var d in HeroDef.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(d.Name), d.Id.ToString());
                if (d.Rank <= 0) { Assert.IsTrue(d.Hidden, d.Name + " 没座次就该是隐藏好汉"); continue; }
                Assert.That(d.Rank, Is.InRange(1, 108), d.Name);
                Assert.IsFalse(seen[d.Rank], d.Name + " 座次重复");
                seen[d.Rank] = true;
                ranked++;
                Assert.IsFalse(string.IsNullOrEmpty(d.Bio), d.Name + " 缺生平");
                Assert.IsFalse(string.IsNullOrEmpty(d.Nick), d.Name + " 缺绰号");
                Assert.AreEqual(d.Rank > 36, d.Disha, d.Name);
            }
            Assert.AreEqual(108, ranked);
        }

        [Test]
        public void StoryJoinsEveryHeroExactlyOnce()
        {
            var joins = new int[HeroDef.All.Length];
            foreach (var ch in ChapterDef.All)
                foreach (var id in ch.Joins)
                {
                    joins[(int)id]++;
                    Assert.Contains(id, ch.Pool, $"{ch.Name}：入伙的 {HeroDef.Get(id).Name} 应在本回出场");
                }
            foreach (var d in HeroDef.All)
            {
                if (d.Rank > 0) Assert.AreEqual(1, joins[(int)d.Id], d.Name + " 入伙次数不对");
                else Assert.LessOrEqual(joins[(int)d.Id], 1, d.Name + " 隐藏好汉最多入伙一次");
            }
            Assert.AreEqual(ChapterDef.All[ChapterDef.StoryCount - 1].Name, "蓼儿洼", "剧情应以蓼儿洼收尾");
        }

        [Test]
        public void ChapterPoolsStaySmallAndNemesisCanFight()
        {
            foreach (var ch in ChapterDef.All)
            {
                Assert.That(ch.Pool.Length, Is.InRange(2, 6), ch.Name + " 池子大小");
                Assert.Contains(ch.Boss.Nemesis, ch.Pool, ch.Name + " 克星不在池里");
                Assert.IsFalse(HeroDef.Get(ch.Boss.Nemesis).NoAttack, ch.Name + " 克星不能出手");
                int fighters = 0;
                foreach (var id in ch.Pool) if (!HeroDef.Get(id).NoAttack) fighters++;
                Assert.GreaterOrEqual(fighters, 2, ch.Name + " 能出手的太少");
                Assert.IsFalse(string.IsNullOrEmpty(ch.Story), ch.Name);
                Assert.IsFalse(string.IsNullOrEmpty(ch.Hui), ch.Name);
            }
            for (int i = 1; i < ChapterDef.StoryCount; i++)
                Assert.GreaterOrEqual(ChapterDef.All[i].RivalSkill, ChapterDef.All[i - 1].RivalSkill, "难度应逐回不降");
        }

        [Test]
        public void EndlessUsesChosenTeamAndRamps()
        {
            var team = new[] { HeroId.WuSong, HeroId.LinChong, HeroId.HuaRong, HeroId.LiKui, HeroId.WuYong };
            var a = ChapterDef.Endless(1, team, 5);
            var b = ChapterDef.Endless(30, team, 5);
            Assert.IsTrue(a.IsEndless);
            CollectionAssert.AreEquivalent(team, a.Pool);
            Assert.Contains(a.Boss.Nemesis, a.Pool);
            Assert.IsFalse(HeroDef.Get(a.Boss.Nemesis).NoAttack);
            Assert.Greater(b.RivalSkill, a.RivalSkill);
            var m = new Match(a, 9);
            Assert.Greater(new Board(a.Map).Path.Count, 10);
            Assert.AreEqual(5, m.Player.Pool.Count);
        }

        [Test]
        public void BondBoostsDamageOnlyWhenMembersFormed()
        {
            var m = PoolMatch(HeroId.RuanXiaoEr, HeroId.RuanXiaoWu, HeroId.RuanXiaoQi);
            var s = m.Player;
            Clear(s);
            OpenAll(s);
            var spots = new System.Collections.Generic.List<CellPos>();
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    if (s.Board.Placeable(c, r) && s.Board.Placeable(c + 1, r) && s.Board.Placeable(c + 2, r)) spots.Add(new CellPos(c, r));
            Assert.GreaterOrEqual(spots.Count, 1);
            var names = new[] { "阮小二", "阮小五", "阮小七" };
            int placed = 0;
            var used = new System.Collections.Generic.HashSet<(int, int)>();
            foreach (var p in spots)
            {
                if (placed >= 3) break;
                if (used.Contains((p.c, p.r)) || used.Contains((p.c + 1, p.r)) || used.Contains((p.c + 2, p.r))) continue;
                for (int k = 0; k < 3; k++)
                {
                    s.Board.Pieces[p.c + k, p.r] = Piece.Hero(names[placed][k]);
                    used.Add((p.c + k, p.r));
                }
                for (int k = -1; k <= 3; k++) used.Add((p.c + k, p.r));
                placed++;
            }
            if (placed < 3) Assert.Inconclusive("地图放不下三个三字名");
            s.RecomputeHeroes();
            Assert.AreEqual(3, s.Heroes.Count);
            Assert.Greater(s.BondBonus(HeroId.RuanXiaoQi), 0f, "阮氏三雄应触发");
            Assert.Contains("阮氏三雄", s.ActiveBonds.ConvertAll(x => x.Name));
        }

        [Test]
        public void DismissRefundsBuns()
        {
            var m = new Match(ChapterDef.All[0], 3);
            var s = m.Player;
            s.Camp[0] = Piece.Unit(UnitKind.Dao, 3);
            int before = s.Buns;
            Assert.IsTrue(s.DismissCamp(0));
            Assert.IsNull(s.Camp[0]);
            Assert.AreEqual(before + Side.DismissRefund(Piece.Unit(UnitKind.Dao, 3)), s.Buns);
        }

        [Test]
        public void ArmedWeaponScalesAndNeverWeakerThanBare()
        {
            Assert.AreEqual(1f, HeroDef.ArmedMul(0), 0.001f);
            Assert.Greater(HeroDef.ArmedMul(1), 1.35f);
            Assert.Greater(HeroDef.ArmedMul(5), HeroDef.ArmedMul(4));
            Assert.AreEqual(HeroDef.ArmedMul(5), HeroDef.ArmedMul(9), 0.001f);
            for (int g = 1; g < Weapons.Grades.Length; g++)
                Assert.Greater(HeroDef.ArmedMul(1, g), HeroDef.ArmedMul(1, g - 1), "品级越高底子越厚");
            foreach (var d in HeroDef.All)
            {
                if (string.IsNullOrEmpty(d.Weapon)) continue;
                Assert.AreNotEqual(WeaponKind.None, d.WKind, d.Name + " 兵器没有分类");
                var aff = Weapons.Affix(d);
                Assert.GreaterOrEqual(aff.Dmg, 0.9f, d.Name);
                Assert.Greater(aff.Cd, 0f, d.Name);
            }
        }

        [Test]
        public void TrapDropsEnemiesAndStunsThem()
        {
            var m = NewMatch();
            var s = m.Player;
            m.SpawnOn(s, EnemyKind.Zu, 4f);
            var e = s.Enemies[0];
            s.Items.Trap = 1;
            Assert.IsTrue(s.UseTrap());
            Assert.AreEqual(0, s.Items.Trap);
            Assert.Less(e.Progress, 3f);
            Assert.GreaterOrEqual(e.StunT, 1.5f);
            Assert.IsFalse(s.UseTrap());
        }

        [Test]
        public void ResistBossHitIsMarkedHalf()
        {
            var ch = Named("白龙庙");
            Assert.AreEqual(BossSkill.Resist, ch.Boss.Skill);
            var m = new Match(ch.WithPool(HeroId.LiKui, ch.Boss.Nemesis), 42);
            var s = m.Player;
            Clear(s);
            m.SpawnOn(s, EnemyKind.Boss, 2f);
            var boss = s.Enemies[0];
            float hp = boss.Hp;
            s.Board.Pieces[5, 3] = Piece.Hero('李');
            s.Board.Pieces[6, 3] = Piece.Hero('逵');
            s.RecomputeHeroes();
            Assert.AreEqual(1, s.Heroes.Count, "白龙庙应能凑出李逵");
            s.Heroes[0].UltCd = 999f;
            m.Events.Clear();
            m.Step(0.05f);
            bool found = false;
            foreach (var ev in m.Events)
            {
                if (ev.Type != Ev.Hit) continue;
                found = true;
                Assert.AreEqual(3, ev.Flag, "铁布衫的伤害应标成半伤");
            }
            Assert.IsTrue(found, "李逵应打到黄文炳");
            float full = HeroDef.Get(HeroId.LiKui).DamageAt(1);
            Assert.AreEqual(hp - full * 0.5f, boss.Hp, 0.05f);
        }
    }
}
