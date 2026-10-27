using System.Collections.Generic;
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
                Assert.GreaterOrEqual(potential, 16, ch.Name + " 能开荒的地太少");
                Assert.IsTrue(b.AnyDiggable(), ch.Name);
            }
        }

        [Test]
        public void MapsHaveNoDeadCells()
        {
            var gong = UnitDef.Get(UnitKind.Gong).Range;
            foreach (var key in ChapterDef.Maps.Keys)
            {
                var b = new Board(ChapterDef.Maps[key]);
                bool pair = false;
                for (int c = 0; c < Board.Cols; c++)
                    for (int r = 0; r < Board.Rows; r++)
                    {
                        var cell = b.Cells[c, r];
                        if (cell == Cell.Path || cell == Cell.Rock) continue;
                        var at = new CellPos(c, r).Center;
                        int melee = 0, bow = 0;
                        for (int i = 1; i < b.Path.Count; i++)
                        {
                            float d = V2.Dist(at, b.Path[i].Center);
                            if (d <= 1.5f) melee++;
                            if (d <= gong) bow++;
                        }
                        Assert.Greater(melee, 0, $"{key} ({c},{r}) 放刀打不到路");
                        Assert.GreaterOrEqual(bow, 3, $"{key} ({c},{r}) 放弓也只够得着一两格路");
                        if (b.Open(c, r) && (b.Open(c + 1, r) || b.Open(c, r - 1))) pair = true;
                    }
                Assert.IsTrue(pair, key + " 开局白地至少要能并排摆下两字好汉");
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
        public void LevelGrowthFavorsAttackSpeedAndWavesOutpaceBoss()
        {
            var dao = UnitDef.Get(UnitKind.Dao);
            Assert.AreEqual(3.15f * (float)Tuning.LevelMul, dao.DamageAt(2), 0.02f);
            float speedUp = dao.CooldownAt(1) / dao.CooldownAt(5);
            Assert.Greater(speedUp, 1.7f, "五级攻速要比一级快出一截");
            Assert.Less(speedUp, 1.85f);

            var hero = HeroDef.Get(HeroId.LinChong);
            Assert.Less(hero.CooldownAt(5), hero.CooldownAt(1) * 0.6f, "好汉升级也要加快出手");
            Assert.Greater(hero.DamageAt(5), hero.DamageAt(1) * 10f);

            Assert.Less(Tuning.WaveHp(3) / Tuning.WaveHp(1), 1.6f, "前三波别一下子变厚");
            Assert.Greater(Tuning.WaveHp(8) / Tuning.WaveHp(1), 3.4f, "头目那一波的小兵要比开局厚一截");
            float boss = EnemyDef.Get(EnemyKind.Boss).HpMul;
            float elite = EnemyDef.Get(EnemyKind.Bu).HpMul;
            Assert.Greater(boss, elite, "头目要比捕快厚");
            Assert.Less(boss, elite * 2.2f, "头目不要再厚过同波捕快一倍多");

            Assert.AreEqual(1f, ChapterDef.All[0].FoeHp, 0.001f);
            Assert.AreEqual(1f, ChapterDef.All[0].RivalFoeHp, 0.001f);
            Assert.AreEqual(1f, ChapterDef.All[5].RivalFoeHp, 0.001f);
            Assert.Greater(ChapterDef.All[ChapterDef.StoryCount - 1].FoeHp, ChapterDef.All[0].FoeHp);
        }

        [Test]
        public void MaxLevelDoesNotMerge()
        {
            Assert.IsFalse(Side.CanMerge(Piece.Unit(UnitKind.Qi, UnitDef.MaxLevel), Piece.Unit(UnitKind.Qi, UnitDef.MaxLevel)));
            Assert.IsFalse(Side.CanMerge(Piece.Hero('武'), Piece.Hero('武')));
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
        public void HeroCharsDoNotMerge_LevelStaysMinOfName()
        {
            var s = PoolMatch(HeroId.WuSong).Player;
            Clear(s);
            var at = FindRun(s, 2, new CellPos(1, 0));
            var wu = Piece.Hero('武', 2);
            var song = Piece.Hero('松', 1);
            s.Board.Pieces[at.c, at.r] = wu;
            s.Board.Pieces[at.c + 1, at.r] = song;
            s.RecomputeHeroes();
            Assert.AreEqual(1, s.Heroes[0].Level);
            var fresh = Piece.Hero('松', 1);
            s.Camp[0] = fresh;
            Assert.IsTrue(s.CampToCell(0, at.c + 1, at.r));
            Assert.AreSame(fresh, s.Board.Pieces[at.c + 1, at.r], "同字相叠只换位");
            Assert.AreSame(song, s.Camp[0]);
            Assert.AreEqual(1, fresh.Level);
            Assert.AreEqual(2, wu.Level);
            Assert.AreEqual(1, s.Heroes[0].Level);
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
            Assert.IsFalse(s.Recruit(), "对手只补空位，山寨满了不再聚义");
        }

        [Test]
        public void RefreshCampReplacesEveryPiece()
        {
            var s = NewMatch().Player;
            s.Buns = 1000;
            var old = new int[s.Camp.Length];
            for (int i = 0; i < s.Camp.Length; i++)
            {
                s.Camp[i] = i == 0 ? Piece.Shovel() : Piece.Hero((char)('甲' + i));
                old[i] = s.Camp[i].Id;
            }
            int stock = s.ShovelStock;
            int cost = s.RecruitCost;
            Assert.IsTrue(s.RefreshCamp());
            Assert.AreEqual(1000 - cost, s.Buns);
            Assert.AreEqual(cost + Tuning.CostStep, s.RecruitCost);
            Assert.AreEqual(stock + 1, s.ShovelStock, "整排换掉时，铲子退回库存，不和字一起丢掉");
            Assert.AreEqual(0, s.EmptyCampSlots);
            for (int i = 0; i < s.Camp.Length; i++)
                Assert.IsFalse(System.Array.IndexOf(old, s.Camp[i].Id) >= 0, "原来的字必须全部换掉");
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
            Assert.AreEqual(Tuning.AdShovels - 1, s.ShovelStock);
            s.DismissCamp(1);
            Assert.AreEqual(PieceType.Shovel, s.Camp[1].Type, "空出来的位置自动补铲子");
            Assert.AreEqual(Tuning.AdShovels - 2, s.ShovelStock);
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
            m.SpawnOn(s, EnemyKind.Zu, Tuning.SpawnCover);
            s.Enemies[0].Pos = h.Pos;
            s.Enemies[0].StunT = 999;
            for (int i = 0; i < 30 && h.Level == 1; i++) m.Step(0.05f);
            Assert.AreEqual(2, h.Level);
            Assert.AreEqual(2, s.Board.Pieces[at.c, at.r].Level);
            Assert.AreEqual(2, s.Board.Pieces[at.c + 1, at.r].Level);
        }

        [Test]
        public void RescuePushesEnemiesBackToTheFront()
        {
            var m = NewMatch();
            var s = m.Player;
            for (int i = 0; i < 4; i++) m.SpawnOn(s, EnemyKind.Zu, s.Board.PathLength - 0.5f);
            m.SpawnOn(s, EnemyKind.Boss, s.Board.PathLength - 1f);
            var boss = s.Enemies[s.Enemies.Count - 1];
            float bossHp = boss.Hp;
            Assert.IsTrue(s.AtTheGate);
            Assert.IsTrue(s.Rescue());
            Assert.IsFalse(s.Rescue(), "一局只能请一次");
            Assert.AreEqual(bossHp, boss.Hp, "推回去不掉血");
            foreach (var e in s.Enemies)
            {
                Assert.IsFalse(e.Dead);
                Assert.Less(e.Progress, Tuning.SpawnCover + 2f, "应推到路口");
            }
            int hp = s.Hp;
            m.Step(0.4f);
            Assert.AreEqual(hp, s.Hp, "推回路口之后一时漏不到宋江");
        }

        [Test]
        public void BaoziMovesOneHeartEitherWay()
        {
            var s = new Match(ChapterDef.All[0], 42, new Loadout { Baozi = 1 }).Player;
            s.Hp = 2;
            Assert.IsTrue(s.UseBaozi());
            Assert.That(s.Hp == 3 || s.Hp == 1, "包子只会回或掉一颗心");
            Assert.IsFalse(s.UseBaozi());
        }

        [Test]
        public void XumingAddsHeartsOnlyForThePlayer()
        {
            var m = new Match(ChapterDef.All[0], 42, new Loadout { Xuming = true });
            Assert.AreEqual(Tuning.MaxHp + Tuning.XumingHearts, m.Player.Hp);
            Assert.AreEqual(11, m.Player.MaxHearts);
            Assert.AreEqual(Tuning.MaxHp, m.Rival.Hp);
        }

        [Test]
        public void GruntLeakRemovesOneHeartAndPaysBuns()
        {
            var m = NewMatch();
            var s = m.Player;
            Assert.AreEqual(3, s.Hp);
            int buns = s.Buns;
            m.SpawnOn(s, EnemyKind.Zu, s.Board.PathLength - 0.01f);
            m.Step(0.05f);
            Assert.AreEqual(2, s.Hp);
            Assert.AreEqual(buns + Tuning.RainBuns, s.Buns);
            Assert.AreEqual(1, s.Rains);
        }

        [Test]
        public void XumingSurvivesTenGruntLeaks()
        {
            var m = new Match(ChapterDef.All[0], 7, new Loadout { Xuming = true });
            var s = m.Player;
            for (int i = 0; i < 10; i++) m.SpawnOn(s, EnemyKind.Zu, s.Board.PathLength - 0.01f);
            m.Step(0.05f);
            Assert.AreEqual(1, s.Hp, "十一颗心吃满十个卒还剩一颗");
            Assert.IsFalse(s.Dead);
        }

        [Test]
        public void LosingAHeartTriggersTimelyRain()
        {
            var m = NewMatch();
            var s = m.Player;
            int buns = s.Buns;
            for (int i = 0; i < 2; i++) m.SpawnOn(s, EnemyKind.Zu, s.Board.PathLength - 0.01f);
            m.Step(0.05f);
            Assert.AreEqual(1, s.Hp);
            Assert.AreEqual(2, s.Rains, "每掉一颗心发一次及时雨");
            Assert.AreEqual(buns + Tuning.RainBuns * 2, s.Buns);
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
            Assert.Greater(m.Time, 70f, "对局太短");
            Assert.Less(m.Time, 12 * 60f, "对局太长");
        }

        /// <summary>代打玩家打一局。ads 为真时：白地剩两格就看铲子广告，官兵压到家门口就看公孙胜作法。</summary>
        static bool PlayerWins(int chapter, float skill, bool ads, uint seed)
        {
            var ch = ChapterDef.All[chapter];
            var m = new Match(ch, seed);
            var player = new RivalAI(m.Player, skill, seed * 3);
            var rival = new RivalAI(m.Rival, ch.RivalSkill, seed * 5);
            bool shovelAd = false;
            const float dt = 1f / 20f;
            while (!m.Over && m.Time < 15 * 60)
            {
                player.Tick(dt);
                rival.Tick(dt);
                var p = m.Player;
                if (ads)
                {
                    bool hasShovel = p.ShovelStock > 0;
                    foreach (var q in p.Camp) if (q != null && q.Type == PieceType.Shovel) hasShovel = true;
                    if (!shovelAd && !hasShovel && p.Board.AnyDiggable() && p.EmptyCells() <= 2) { shovelAd = true; p.GrantShovels(Tuning.AdShovels); }
                    if (p.AtTheGate && !p.RescueUsed) p.Rescue();
                }
                m.Step(dt);
                m.Events.Clear();
            }
            return m.Winner == 0;
        }

        static int WinsOf(int chapter, float skill, bool ads, int runs)
        {
            int wins = 0;
            for (uint s = 1001; s < 1001 + runs; s++) if (PlayerWins(chapter, skill, ads, s)) wins++;
            return wins;
        }

        [Test]
        public void FirstChapterIsFriendly()
        {
            const int runs = 24;
            int plain = WinsOf(0, 0.5f, false, runs), ads = WinsOf(0, 0.5f, true, runs);
            UnityEngine.Debug.Log($"[平衡] 第一回 普通玩家 不看广告 {plain}/{runs}，看广告 {ads}/{runs}");
            Assert.GreaterOrEqual(plain, runs * 4 / 10, "第一回不看广告也该赢一半上下");
            Assert.GreaterOrEqual(ads, runs * 6 / 10, "第一回看广告要明显好打");
        }

        [Test]
        public void MidChaptersNeedSkillOrAds([Values(12, 30)] int chapter)
        {
            const int runs = 48;
            int casual = WinsOf(chapter, 0.5f, false, runs);
            int expert = WinsOf(chapter, 1f, false, runs);
            int helped = WinsOf(chapter, 0.5f, true, runs);
            UnityEngine.Debug.Log($"[平衡] {ChapterDef.All[chapter].Name} 普通不看 {casual}/{runs}，高手不看 {expert}/{runs}，普通看广告 {helped}/{runs}");
            Assert.LessOrEqual(casual, runs / 2, "普通玩家不看广告不该轻松赢");
            Assert.Greater(expert, casual, "打得好要比打得差赢得多");
            Assert.GreaterOrEqual(helped, runs * 6 / 10, "看了铲子广告要相对轻松");
            Assert.GreaterOrEqual(helped - casual, runs / 5, "广告要明显拉开胜率");
        }

        [Test]
        public void FirstChapterPacing()
        {
            var fresh = new Match(ChapterDef.All[0], 1).Player;
            Assert.IsTrue(fresh.Recruit());
            Assert.AreEqual(10, fresh.Buns, "摆完第一轮还剩 10 个包子");
            Assert.IsFalse(fresh.CanRecruit);

            float second = 0, end = 0, foeLeak = 0, playerHp = 0;
            const int runs = 6;
            for (uint seed = 1; seed <= runs; seed++)
            {
                var m = new Match(ChapterDef.All[0], seed);
                var player = new RivalAI(m.Player, 0.6f, seed * 3);
                var rival = new RivalAI(m.Rival, ChapterDef.All[0].RivalSkill, seed * 5);
                float secondAt = 0;
                int leakWave = 0;
                while (!m.Over && m.Time < 20 * 60)
                {
                    player.Tick(1f / 30f);
                    rival.Tick(1f / 30f);
                    m.Step(1f / 30f);
                    m.Events.Clear();
                    if (secondAt == 0 && m.Player.Recruits >= 2) secondAt = m.Time;
                    if (leakWave == 0 && m.Rival.Hp < m.Rival.MaxHp) leakWave = m.Wave;
                }
                second += secondAt;
                end += m.Time;
                foeLeak += leakWave == 0 ? m.Wave : leakWave;
                playerHp += m.Player.Hp;
            }
            second /= runs; end /= runs; foeLeak /= runs; playerHp /= runs;
            UnityEngine.Debug.Log($"[平衡] 第一回 第二次征兵 {second:0}s, 对手首次掉心第 {foeLeak:0.0} 波, 对局 {end:0}s, 玩家剩血 {playerHp:0}");
            Assert.Less(second, 35f, "第一波打完就该接上第二次征兵");
            Assert.Less(foeLeak, 12f, "对手最迟要在头目波开始掉心");
            Assert.Less(end, 270f, "第一回别拖太久");
        }

        [Test]
        public void EnemyInSpawnBrushCannotBeHit()
        {
            var m = NewMatch();
            var s = m.Player;
            Clear(s);
            var sp = s.Board.Spawn;
            Assert.IsTrue(s.Board.Placeable(sp.c + 1, sp.r - 1));
            s.Board.Pieces[sp.c + 1, sp.r - 1] = Piece.Unit(UnitKind.Gong);
            m.SpawnOn(s, EnemyKind.Zu, 0f);
            var e = s.Enemies[0];
            float hp = e.Hp;
            m.Step(0.05f);
            Assert.AreEqual(hp, e.Hp, 0.01f, "草堆里不该掉血");
            e.Progress = Tuning.SpawnCover;
            m.Step(0.05f);
            Assert.Less(e.Hp, hp, "走出草堆就该挨打");
        }

        [Test]
        public void RankStarsPromoteAndNeverLeaveMajor()
        {
            int tier = 0, stars = 0;
            RankRules.Apply(ref tier, ref stars, false, out bool up, out bool down);
            Assert.AreEqual(0, tier);
            Assert.AreEqual(0, stars, "喽啰·叁没有星，再败也没得掉");
            Assert.IsFalse(down);

            stars = 2;
            RankRules.Apply(ref tier, ref stars, false, out up, out down);
            Assert.AreEqual(1, stars, "最低阶输了也掉星");
            Assert.AreEqual(0, tier);
            Assert.IsFalse(down);

            stars = 0;
            for (int i = 0; i < 4; i++) RankRules.Apply(ref tier, ref stars, true, out up, out down);
            Assert.AreEqual(4, stars);
            Assert.IsFalse(up);
            RankRules.Apply(ref tier, ref stars, true, out up, out down);
            Assert.IsTrue(up, "第五颗星升一小阶");
            Assert.AreEqual(1, tier);
            Assert.AreEqual(0, stars);
            Assert.AreEqual("喽啰·贰", RankRules.Name(tier));

            RankRules.Apply(ref tier, ref stars, false, out up, out down);
            Assert.IsTrue(down, "没星再败降回上一小阶");
            Assert.AreEqual(0, tier);
            Assert.AreEqual(4, stars);

            for (int t = 0; t < RankRules.Top; t++)
                Assert.AreEqual(RankRules.MaxStars, RankRules.StarsAt(t), RankRules.Name(t) + " 应是五星");
            Assert.AreEqual(0, RankRules.StarsAt(RankRules.Top));

            tier = 4 * RankRules.SubPerMajor + 1;
            stars = 0;
            RankRules.Apply(ref tier, ref stars, false, out up, out down);
            Assert.IsTrue(down);
            Assert.AreEqual(4 * RankRules.SubPerMajor, tier);
            Assert.AreEqual(RankRules.MaxStars - 1, stars);

            stars = 0;
            RankRules.Apply(ref tier, ref stars, false, out up, out down);
            Assert.IsFalse(down, "大阶最低一小阶不再往下掉");
            Assert.AreEqual(4 * RankRules.SubPerMajor, tier);

            tier = RankRules.Top;
            stars = 40;
            RankRules.Apply(ref tier, ref stars, true, out up, out down);
            Assert.AreEqual(RankRules.Top, tier, "天魁星封顶");
            Assert.AreEqual(41, stars);
            RankRules.Apply(ref tier, ref stars, false, out up, out down);
            Assert.AreEqual(RankRules.Top, tier, "天魁星败了只掉星");
            Assert.AreEqual(40, stars);
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
            Assert.Greater(b.RivalIncome, a.RivalIncome);
            Assert.Greater(b.FoeHp, a.FoeHp);
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
        public void WeaponDropsOnlyAfterJoining()
        {
            var pool = ChapterDef.All[0].Pool;
            var none = new List<HeroId>();
            Weapons.CollectDropCandidates(none, pool, id => false, none);
            Assert.AreEqual(0, none.Count, "第一关没人入伙时，撑过四波也不该掉史进的兵器");

            var formed = new List<HeroId> { HeroId.ShiJin };
            var beforeJoin = new List<HeroId>();
            Weapons.CollectDropCandidates(formed, pool, id => false, beforeJoin);
            Assert.AreEqual(0, beforeJoin.Count, "凑出了名字但还没入伙，也不掉");

            var afterJoin = new List<HeroId>();
            Weapons.CollectDropCandidates(formed, pool, id => id == HeroId.ShiJin, afterJoin);
            Assert.AreEqual(1, afterJoin.Count);
            Assert.AreEqual(HeroId.ShiJin, afterJoin[0]);

            var fallback = new List<HeroId>();
            Weapons.CollectDropCandidates(new List<HeroId>(), pool, id => id == HeroId.ChenDa || id == HeroId.ShiJin, fallback);
            CollectionAssert.AreEquivalent(new[] { HeroId.ChenDa, HeroId.ShiJin }, fallback);
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
        public void BorrowedWeaponUsesTheWeaponsGradeNotTheWearer()
        {
            var m = NewMatch();
            var s = m.Player;
            int n = HeroDef.All.Length;
            s.Items.Gear = new int[n];
            s.Items.WeaponLv = new int[n];
            for (int i = 0; i < n; i++) s.Items.Gear[i] = -1;
            var wearer = HeroDef.Get(HeroId.HuaRong);
            var blade = HeroDef.Get(HeroId.WuSong);
            s.Items.Gear[(int)wearer.Id] = (int)blade.Id;
            s.Items.WeaponLv[(int)wearer.Id] = 2;

            Assert.AreEqual(blade, s.GearOf(wearer));
            Assert.IsTrue(s.Armed(wearer));
            Assert.IsFalse(s.Armed(blade));
            Assert.AreEqual(HeroDef.ArmedMul(2, blade.WGrade), s.GearMul(wearer), 0.001f);
            Assert.AreEqual(1.1f, Weapons.Affix(blade, wearer).Dmg, 0.001f);
            Assert.AreEqual(1f, s.GearMul(blade), 0.001f);

            var song = HeroDef.Get(HeroId.WuSong);
            var spear = HeroDef.Get(HeroId.LinChong);
            s.Items.Gear[(int)song.Id] = (int)spear.Id;
            s.Items.WeaponLv[(int)song.Id] = 1;
            Assert.AreEqual(song.RangeWith(s.Items.Far) + 0.3f, s.ShownRange(song), 0.001f);
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
            s.Board.Pieces[5, 2] = Piece.Hero('李');
            s.Board.Pieces[6, 2] = Piece.Hero('逵');
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

        static Enemy CastFirst(BossSkill skill, out Match m)
        {
            ChapterDef ch = null;
            foreach (var c in ChapterDef.All) if (c.Boss.Skills[0] == skill) { ch = c; break; }
            Assert.IsNotNull(ch, "没有以此绝技开场的头目：" + skill);
            m = new Match(ch, 42);
            var s = m.Player;
            Clear(s);
            OpenAll(s);
            m.SpawnOn(s, EnemyKind.Boss, 2f);
            var boss = s.Enemies[0];
            boss.SkillT = 0f;
            m.Events.Clear();
            return boss;
        }

        [Test]
        public void DazeBossFreezesStrongestPieces()
        {
            var boss = CastFirst(BossSkill.Daze, out var m);
            var s = m.Player;
            var weak = Piece.Unit(UnitKind.Gong);
            var strong = Piece.Unit(UnitKind.Gong);
            strong.Level = 3;
            s.Board.Pieces[1, 1] = weak;
            s.Board.Pieces[2, 1] = strong;
            m.Step(0.05f);
            Assert.Greater(strong.StunT, 0f, "定身要先定最高阶的字");
            bool hex = false;
            foreach (var ev in m.Events) if (ev.Type == Ev.BossHex && ev.B == (int)BossSkill.Daze) hex = true;
            Assert.IsTrue(hex, "定身要发出表现事件");
            for (int i = 0; i < 80; i++) m.Step(0.05f);
            Assert.LessOrEqual(strong.StunT, 0f, "定身几秒后要解开");
        }

        [Test]
        public void SealAndFogBlockPlacement([Values(BossSkill.Seal, BossSkill.Fog)] BossSkill skill)
        {
            var boss = CastFirst(skill, out var m);
            var b = m.Player.Board;
            int before = m.Player.EmptyCells();
            m.Step(0.05f);
            boss.SkillT = 999f;
            int blocked = 0, covered = 0;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    if (b.Open(c, r) && !b.Placeable(c, r)) blocked++;
                    if (b.Seal[c, r] > 0 || b.Fog[c, r] > 0) covered++;
                }
            Assert.AreEqual(skill == BossSkill.Seal ? 3 : 6, covered, "封条封三格、乌云罩六格");
            Assert.GreaterOrEqual(blocked, 3, "罩住的格子不能再摆");
            Assert.AreEqual(before - blocked, m.Player.EmptyCells());
            for (int i = 0; i < 160; i++) m.Step(0.05f);
            int still = 0;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    if (b.Seal[c, r] > 0 || b.Fog[c, r] > 0) still++;
            Assert.AreEqual(0, still, "旧的封条乌云要随时间散去");
        }

        [Test]
        public void BossEnragesAtHalfHp()
        {
            var boss = CastFirst(BossSkill.Summon, out var m);
            boss.SkillT = 5f;
            boss.Hp = boss.MaxHp * 0.45f;
            m.Step(0.05f);
            Assert.IsTrue(boss.Enraged);
            bool rage = false;
            foreach (var ev in m.Events) if (ev.Type == Ev.BossRage) rage = true;
            Assert.IsTrue(rage, "狂暴要通知表现层");
            Assert.LessOrEqual(boss.SkillT, 0.4f, "狂暴马上补放一门绝技");
        }

        [Test]
        public void ManualUltWaitsForTheSkillButton()
        {
            var ch = Named("白龙庙");
            var m = new Match(ch.WithPool(HeroId.LiKui), 42);
            var s = m.Player;
            Clear(s);
            s.ManualUlt = true;
            m.SpawnOn(s, EnemyKind.Boss, 2f);
            s.Board.Pieces[5, 2] = Piece.Hero('李');
            s.Board.Pieces[6, 2] = Piece.Hero('逵');
            s.RecomputeHeroes();
            var h = s.LeadHero;
            Assert.IsNotNull(h);
            h.UltCd = 0f;
            m.Events.Clear();
            m.Step(0.05f);
            foreach (var ev in m.Events)
                Assert.IsFalse(ev.Type == Ev.Ult && ev.A == h.Id && ev.Flag == 0, "手动模式下大招不该自己放");
            Assert.IsTrue(s.LeadUltReady);
            Assert.IsTrue(s.CastLeadUlt(), "点技能钮应能放出大招");
            Assert.Greater(h.UltCd, 0f);
            Assert.IsFalse(s.CastLeadUlt(), "冷却中不能再放");
        }

        [Test]
        public void ActiveItemsWaitOutTheirCooldown()
        {
            var m = NewMatch();
            var s = m.Player;
            s.Items.MengHan = 2;
            s.Items.TianShu = 1;
            s.Items.Fu = 2;

            Clear(s);
            Assert.IsFalse(s.UseTianShu(1, 1), "没有兵不能用天书");
            Assert.AreEqual(1, s.Items.TianShu);
            Assert.AreEqual(0f, s.ShuCd, 0.001f);

            Assert.IsTrue(s.UseFu());
            Assert.AreEqual(1, s.Items.Fu);
            Assert.AreEqual(Tuning.CdFu, s.FuCd, 0.01f);
            Assert.Greater(s.ItemHasteT, 0f);
            Assert.IsFalse(s.UseFu(), "冷却中不能连放");
            Assert.AreEqual(1, s.Items.Fu);

            Assert.IsTrue(s.UseMengHan());
            Assert.AreEqual(1, s.Items.MengHan);
            Assert.IsFalse(s.UseMengHan(), "冷却中不能连放");
            m.Step(1f);
            Assert.AreEqual(Tuning.CdMeng - 1f, s.MengCd, 0.02f);
            s.Tick(s.MengCd);
            Assert.AreEqual(0f, s.MengCd, 0.001f);
            Assert.IsTrue(s.UseMengHan());
            Assert.AreEqual(0, s.Items.MengHan);
        }

        [Test]
        public void TianShuUpgradesOnlyTheChosenCell()
        {
            var m = NewMatch();
            var s = m.Player;
            Clear(s);
            OpenAll(s);
            s.Items.TianShu = 2;
            var low = Piece.Unit(UnitKind.Gong);
            var picked = Piece.Unit(UnitKind.Dao, 3);
            var full = Piece.Unit(UnitKind.Qiang, UnitDef.MaxLevel);
            s.Board.Pieces[0, 0] = low;
            s.Board.Pieces[1, 0] = picked;
            s.Board.Pieces[2, 0] = full;
            s.Board.Pieces[3, 0] = Piece.Hero('林');

            Assert.IsFalse(s.UseTianShu(2, 0), "满级兵不能再升");
            Assert.IsFalse(s.UseTianShu(3, 0), "金字不吃天书");
            Assert.IsFalse(s.UseTianShu(4, 0), "空格落空");
            Assert.AreEqual(2, s.Items.TianShu, "落空不消耗");
            Assert.AreEqual(0f, s.ShuCd, 0.001f, "落空不进冷却");

            Assert.IsTrue(s.UseTianShu(1, 0));
            Assert.AreEqual(4, picked.Level, "拖到谁身上就升谁");
            Assert.AreEqual(1, low.Level, "不会自动挑最低级的");
            Assert.AreEqual(1, s.Items.TianShu);
            Assert.AreEqual(Tuning.CdShu, s.ShuCd, 0.01f);
        }

        [Test]
        public void StoveBakesUpToItsCapAndCannotBeBanked()
        {
            var m = new Match(ChapterDef.All[0], 42, new Loadout { Stove = true });
            var s = m.Player;
            Assert.IsTrue(s.FindStove(out int c, out int r), "带灶开局就摆在场上");
            var stove = s.Board.Pieces[c, r];
            Assert.AreEqual(PieceType.Stove, stove.Type);
            Assert.IsFalse(new Match(ChapterDef.All[0], 42).Player.FindStove(out _, out _), "没带灶就没有");
            Assert.IsFalse(m.Rival.FindStove(out _, out _), "对手不带灶");

            int slot = System.Array.IndexOf(s.Camp, null);
            if (slot < 0) { s.Camp[0] = null; slot = 0; }
            Assert.IsFalse(s.CellToCamp(c, r, slot), "灶不能收进山寨");
            Assert.IsFalse(s.DismissCell(c, r), "灶不能遣散换炊饼");

            int start = s.Buns;
            for (int i = 0; i < 20 * 60; i++) s.Tick(0.1f);
            Assert.AreEqual(Side.StoveCapAt(1), s.StoveMade, "一级灶一局最多出这么多");
            Assert.GreaterOrEqual(s.Buns - start, Side.StoveCapAt(1));

            s.Items.TianShu = 3;
            Assert.IsTrue(s.UseTianShu(c, r), "天书能升灶");
            s.ShuCd = 0;
            Assert.IsTrue(s.UseTianShu(c, r));
            s.ShuCd = 0;
            Assert.AreEqual(Tuning.StoveMaxLevel, stove.Level);
            Assert.IsFalse(s.UseTianShu(c, r), "灶最多三级");
            for (int i = 0; i < 20 * 60; i++) s.Tick(0.1f);
            Assert.AreEqual(Side.StoveCapAt(3), s.StoveMade, "升级抬高上限，但已出的不清零");

            OpenAll(s);
            int tc = (c + 1) % Board.Cols;
            s.Board.Pieces[tc, r] = null;
            Assert.IsTrue(s.CellToCell(c, r, tc, r), "灶可以挪位置");
            for (int i = 0; i < 600; i++) s.Tick(0.1f);
            Assert.AreEqual(Side.StoveCapAt(3), s.StoveMade, "挪灶不会重新计数");
        }

        [Test]
        public void WineRaisesHeroCharOdds()
        {
            int Chars(bool wine)
            {
                var s = new Match(ChapterDef.All[12], 42, new Loadout { Wine = wine }).Player;
                int n = 0;
                for (int i = 0; i < 400; i++)
                {
                    for (int k = 0; k < s.Camp.Length; k++) s.Camp[k] = null;
                    s.Buns = 99999;
                    s.Recruit();
                    foreach (var p in s.Camp) if (p != null && p.Type == PieceType.HeroChar) n++;
                }
                return n;
            }
            int plain = Chars(false), wine = Chars(true);
            UnityEngine.Debug.Log($"[平衡] 招贤酒 金字 {plain} → {wine}");
            Assert.Greater(wine, plain * 1.15f, "带酒金字明显变多");
            Assert.Less(wine, plain * 1.6f, "也不会多得离谱");
        }

        static Side Armed(HeroId wearer, HeroId weapon, out Match m)
        {
            m = new Match(ChapterDef.All[0].WithPool(wearer), 42);
            var s = m.Player;
            Clear(s);
            OpenAll(s);
            int n = HeroDef.All.Length;
            s.Items.Gear = new int[n];
            s.Items.WeaponLv = new int[n];
            for (int i = 0; i < n; i++) s.Items.Gear[i] = -1;
            s.Items.Gear[(int)wearer] = (int)weapon;
            s.Items.WeaponLv[(int)wearer] = 1;
            var name = HeroDef.Get(wearer).Name;
            for (int i = 0; i < name.Length; i++) s.Board.Pieces[1 + i, 1] = Piece.Hero(name[i]);
            s.RecomputeHeroes();
            Assert.AreEqual(1, s.Heroes.Count, name + " 应能凑出");
            s.Heroes[0].UltCd = 999f;
            return s;
        }

        /// <summary>在离好汉最近的路上放一个打不死的小兵，让好汉出一下手。</summary>
        static Enemy FirstShot(Side s, Match m)
        {
            var at = s.Heroes[0].Pos;
            float progress = 0f, best = float.MaxValue;
            for (float d = 2f; d < s.Board.PathLength - 1f; d += 0.1f)
            {
                float dist = V2.Dist(s.Board.PosAt(d), at);
                if (dist < best) { best = dist; progress = d; }
            }
            m.SpawnOn(s, EnemyKind.Zu, progress);
            var e = s.Enemies[s.Enemies.Count - 1];
            e.Hp = e.MaxHp = 1e6f;
            s.Heroes[0].Cd = 0f;
            s.Heroes[0].UltCd = 999f;
            s.Tick(0.02f);
            return e;
        }

        [Test]
        public void NamedWeaponsChangeHowHeroesFight()
        {
            Assert.AreEqual(WeaponMove.Snake, Weapons.MoveOf(HeroDef.Get(HeroId.LinChong)));
            Assert.AreEqual(WeaponMove.None, Weapons.MoveOf(HeroDef.Get(HeroId.WuSong)));
            foreach (WeaponMove mv in System.Enum.GetValues(typeof(WeaponMove)))
            {
                if (mv == WeaponMove.None) continue;
                Assert.IsNotEmpty(Weapons.MoveName(mv));
                Assert.IsNotEmpty(Weapons.MoveText(mv));
            }

            Assert.AreEqual(0f, Side.FarBowBonus(0f), 0.001f);
            Assert.AreEqual(Weapons.FarBowCap, Side.FarBowBonus(40f), 0.001f, "雕弓加成封顶");

            var hook = Armed(HeroId.WuSong, HeroId.XuNing, out var m1);
            var slowed = FirstShot(hook, m1);
            Assert.Greater(slowed.SlowT, 0f, "钩镰枪命中就绊住");

            var staff = Armed(HeroId.WuSong, HeroId.LuZhiShen, out var m2);
            var knocked = FirstShot(staff, m2);
            var sameSpot = FirstShot(Armed(HeroId.WuSong, HeroId.XuNing, out var m2b), m2b);
            Assert.Less(knocked.Progress, sameSpot.Progress - Weapons.StaffKnock * 0.8f, "禅杖打退一大步");

            var blade = Armed(HeroId.WuSong, HeroId.YangZhi, out var m3);
            var cut = FirstShot(blade, m3);
            float first = cut.MaxHp - cut.Hp;
            Assert.IsTrue(cut.Cut);
            blade.Heroes[0].Cd = 0f;
            blade.Tick(0.02f);
            float second = cut.MaxHp - cut.Hp - first;
            Assert.Greater(first, second * 1.4f, "宝刀只有头一下重");

            var plain = Armed(HeroId.WuSong, HeroId.WuSong, out var m4);
            var bare = FirstShot(plain, m4);
            Assert.AreEqual(0f, bare.SlowT, 0.001f, "普通兵器没有招式");
            Assert.IsFalse(bare.Cut);
        }

        [Test]
        public void DailyIsSameForEveryoneAndChangesByDay()
        {
            var a = ChapterDef.Daily(20261027);
            var b = ChapterDef.Daily(20261027);
            Assert.IsTrue(a.IsDaily);
            Assert.IsFalse(a.IsEndless);
            Assert.AreEqual(a.Name, b.Name);
            Assert.AreSame(a.Map, b.Map);
            CollectionAssert.AreEqual(a.Boss.Skills, b.Boss.Skills);
            Assert.AreEqual(a.Seed, b.Seed);
            Assert.AreEqual(0, a.Joins.Length, "每日一战不收人");
            Assert.GreaterOrEqual(a.Boss.Skills.Length, 2);
            bool differs = false;
            for (int d = 1; d < 7; d++)
            {
                var o = ChapterDef.Daily(20261027 + d);
                if (o.Name != a.Name || !ReferenceEquals(o.Map, a.Map)) differs = true;
            }
            Assert.IsTrue(differs, "一周里每天不能全一样");
            foreach (var ch in ChapterDef.All) Assert.IsFalse(ch.IsDaily, "生成每日一战不能改到剧情回");

            int casual = 0, expert = 0;
            const int runs = 16;
            for (uint s = 1; s <= runs; s++)
            {
                foreach (var skill in new[] { 0.5f, 1f })
                {
                    var m = new Match(a, a.Seed + s);
                    var p = new RivalAI(m.Player, skill, s * 3);
                    var r = new RivalAI(m.Rival, a.RivalSkill, s * 5);
                    while (!m.Over && m.Time < 15 * 60) { p.Tick(0.05f); r.Tick(0.05f); m.Step(0.05f); m.Events.Clear(); }
                    if (m.Winner == 0) { if (skill < 1f) casual++; else expert++; }
                }
            }
            UnityEngine.Debug.Log($"[平衡] 每日一战 {a.Name} 普通 {casual}/{runs} 高手 {expert}/{runs}");
            Assert.GreaterOrEqual(expert, runs / 3, "每日一战打得好要能赢");
            Assert.LessOrEqual(casual, runs * 9 / 10, "也不能闭眼赢");
        }

        static bool LoadoutWins(int chapter, uint seed, Loadout items)
        {
            var ch = ChapterDef.All[chapter];
            var m = new Match(ch, seed, items);
            var player = new RivalAI(m.Player, 0.5f, seed * 3);
            var rival = new RivalAI(m.Rival, ch.RivalSkill, seed * 5);
            const float dt = 1f / 20f;
            while (!m.Over && m.Time < 15 * 60)
            {
                player.Tick(dt);
                rival.Tick(dt);
                m.Step(dt);
                m.Events.Clear();
            }
            return m.Winner == 0;
        }

        [Test]
        public void FourPassivesHelpButDoNotTrivialize()
        {
            const int runs = 32;
            int Wins(System.Func<Loadout> make)
            {
                int w = 0;
                for (uint s = 2001; s < 2001 + runs; s++) if (LoadoutWins(12, s, make())) w++;
                return w;
            }
            int none = Wins(() => new Loadout());
            int stove = Wins(() => new Loadout { Stove = true, Wine = true, Gang = true, Flag = true });
            int classic = Wins(() => new Loadout { Gang = true, Flag = true, Marsh = true, Stele = true });
            UnityEngine.Debug.Log($"[平衡] {ChapterDef.All[12].Name} 被动：不带 {none}/{runs}，灶酒纲旗 {stove}/{runs}，纲旗泊碣 {classic}/{runs}");
            Assert.Greater(stove, none, "带被动要比不带好打");
            Assert.Greater(classic, none);
            Assert.LessOrEqual(stove, runs * 9 / 10, "四个被动也不该稳赢");
            Assert.LessOrEqual(classic, runs * 9 / 10);
        }
    }
}
