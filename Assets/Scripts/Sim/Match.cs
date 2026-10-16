using System;
using System.Collections.Generic;

namespace Rescue.Sim
{
    public sealed class Match
    {
        public readonly ChapterDef Chapter;
        public readonly Side[] Sides = new Side[2];
        public readonly List<SimEvent> Events = new List<SimEvent>();

        public float Time;
        public int Wave;
        public bool Over;
        /// <summary>新手引导时冻结时间，玩家仍可操作。</summary>
        public bool Hold;
        public int Winner = -1;

        readonly Rng _waveRng;
        readonly List<EnemyKind> _queue = new List<EnemyKind>();
        int _qi;
        float _spawnT;
        float _interval;
        float _crowdScale = 1f;
        float _nextWaveT = Tuning.FirstWaveAt;
        int _nextEnemyId = 1;

        public Side Player => Sides[0];
        public Side Rival => Sides[1];

        public Match(ChapterDef chapter, uint seed, Loadout playerItems = null)
        {
            Chapter = chapter;
            _waveRng = new Rng(seed * 2654435761u + 17);
            Sides[0] = new Side(0, this, false, seed * 7 + 1, playerItems);
            Sides[1] = new Side(1, this, true, seed * 13 + 5, null);
            Sides[1].IncomeMul = chapter.RivalIncome;
            if (playerItems != null && playerItems.Xuming) Sides[1].AddMaxHp(6);
        }

        public float NextWaveIn => _qi < _queue.Count ? 0 : Math.Max(0, _nextWaveT - Time);

        public void Step(float dt)
        {
            if (Over || Hold) return;
            Time += dt;

            if (_qi >= _queue.Count && Time >= _nextWaveT) StartWave();
            if (_qi < _queue.Count)
            {
                _spawnT -= dt;
                if (_spawnT <= 0)
                {
                    var kind = _queue[_qi++];
                    foreach (var s in Sides) SpawnOn(s, kind, 0);
                    _spawnT = _interval;
                    if (_qi >= _queue.Count) _nextWaveT = Time + Tuning.WaveGap;
                }
            }

            foreach (var s in Sides) s.Tick(dt);

            bool p = Player.Dead, r = Rival.Dead;
            if (p || r)
            {
                Over = true;
                Winner = p && r ? (Player.Kills >= Rival.Kills ? 0 : 1) : p ? 1 : 0;
                Events.Add(new SimEvent { Type = Ev.End, A = Winner });
            }
        }

        void StartWave()
        {
            Wave++;
            _queue.Clear();
            _qi = 0;
            _spawnT = 0;
            _interval = Math.Max(0.28f, 0.85f - 0.025f * Wave);

            // 后期人数封顶、单兵变厚，总血量和收益不变，画面不至于糊成一片
            int crowd = Math.Min(10 + 2 * (Wave - 1), 44);
            int n = Math.Min(crowd, Tuning.MaxWaveCount);
            _crowdScale = (float)crowd / n;
            for (int i = 0; i < n; i++)
            {
                var k = EnemyKind.Zu;
                if (Wave >= 3 && _waveRng.Chance(0.22f)) k = EnemyKind.Chai;
                else if (Wave >= 5 && _waveRng.Chance(0.22f)) k = EnemyKind.Jia;
                _queue.Add(k);
            }

            bool elite = Wave >= 4 && Wave % 3 == 1;
            if (elite)
            {
                int bu = Wave / 4 + 1;
                for (int i = 0; i < bu; i++) _queue.Insert(Math.Min(_queue.Count, (i + 1) * _queue.Count / (bu + 1)), EnemyKind.Bu);
            }
            bool boss = Wave % 8 == 0;
            if (boss) _queue.Add(EnemyKind.Boss);

            Events.Add(new SimEvent { Type = Ev.WaveStart, A = Wave, Side = -1 });
            if (elite) Events.Add(new SimEvent { Type = Ev.EliteWarn, A = Wave, Side = -1 });
            if (boss) Events.Add(new SimEvent { Type = Ev.BossSpawn, A = Wave, Side = -1, Text = Chapter.Boss.Name });
        }

        public void SpawnOn(Side s, EnemyKind kind, float progress)
        {
            if (s.Dead) return;
            var def = EnemyDef.Get(kind);
            float hp = Tuning.WaveHp(Math.Max(1, Wave)) * def.HpMul;
            bool grunt = kind == EnemyKind.Zu || kind == EnemyKind.Chai || kind == EnemyKind.Jia;
            float scale = grunt ? _crowdScale : 1f;
            hp *= scale;
            var e = new Enemy
            {
                Id = _nextEnemyId++,
                Def = def,
                Hp = hp,
                MaxHp = hp,
                RewardMul = scale,
                LeakDamage = (int)Math.Round(def.LeakDamage * scale),
                Progress = progress,
                Glyph = def.Glyph,
                SkillT = 3f,
            };
            if (kind == EnemyKind.Boss)
            {
                e.Boss = Chapter.Boss;
                e.Glyph = Chapter.Boss.Name;
            }
            s.AddEnemy(e);
        }
    }
}
