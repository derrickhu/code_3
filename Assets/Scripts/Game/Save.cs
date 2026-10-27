using System;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;

namespace Rescue
{
    [Serializable]
    public sealed class SaveData
    {
        public int coins = 60;
        public int tier;
        public int stars = 2;
        public int unlocked;
        public List<int> heroes = new List<int>();
        public int tutorial;
        public int stamina = Stamina.Max;
        public long staminaAt;
        public int staminaRule;
        public string shopDay = "";
        /// <summary>这批货是第几局之后摆上的。打完一局才换，同一局再看不会刷新。</summary>
        public int shopBattle = -1;
        public List<int> shopOffer = new List<int>();
        public List<int> shopSold = new List<int>();
        public List<int> shopFree = new List<int>();
        public string lottoDay = "";
        public int lottoUsed;
        public bool gang, flag, marsh, stele, xuming, far, stove, wine;
        public int menghan, tianshu, baozi, shovels, fu, trap;
        /// <summary>今天带上场的主动道具。最多两件。没选过时，有的就带，超过两件只带前两件。</summary>
        public List<int> activeOn = new List<int>();
        public bool activeSet;
        /// <summary>今天带上场的被动道具。最多四件。没选过时按稀有度从高到低带满四件。</summary>
        public List<int> passiveOn = new List<int>();
        public bool passiveSet;
        /// <summary>今天各个广告入口看了几次。adScene 和 adCount 对齐。</summary>
        public string adDay = "";
        public List<string> adScene = new List<string>();
        public List<int> adCount = new List<int>();
        /// <summary>好汉谱里已经领过入伙赏钱的好汉。</summary>
        public List<int> codexPaid = new List<int>();
        /// <summary>排行榜上报的地区。空串表示还没选，「江湖」表示不分省。</summary>
        public string region = "";
        /// <summary>每日一战：今天打过没、赢过没。</summary>
        public string dailyDay = "";
        public bool dailyWon;
        public int dailyTries;
        public bool sfx = true;
        public int wins, battles;
        public List<int> weapons = new List<int>();
        /// <summary>旧档：装备自己成名兵器的好汉。版本 3 起改成 armWho / armWhat。</summary>
        public List<int> armed = new List<int>();
        /// <summary>带着兵器的人，和 armWhat 对齐。</summary>
        public List<int> armWho = new List<int>();
        /// <summary>兵器出自谁。同一件同时只能出现一次。</summary>
        public List<int> armWhat = new List<int>();
        public string claimDay = "";
        public int claimTier, claimStars;
        public bool claimTaken = true;
        /// <summary>存档版本。2 起是梁山座次三十四阶。3 起兵器可以交给别人带。</summary>
        public int ver;
        /// <summary>已入伙的好汉（好汉谱解锁）。heroes 只记“凑出过”。</summary>
        public List<int> joined = new List<int>();
        /// <summary>聚义厅：自选的五员大将、当前第几阵、最好成绩。</summary>
        public List<int> team = new List<int>();
        public int endlessRound = 1, endlessBest;
        /// <summary>一百单八将聚齐后排座次的仪式只放一次。</summary>
        public bool seated;

        public Loadout PeekLoadout()
        {
            bool On(int id) => Save.PassiveFielded(id);
            var l = new Loadout
            {
                Gang = gang && On(0), Flag = flag && On(1), Marsh = marsh && On(2), Stele = stele && On(3),
                MengHan = Save.FieldCount(4), TianShu = Save.FieldCount(5), Baozi = Save.FieldCount(6),
                Xuming = xuming && On(7), Shovels = On(8) ? shovels : 0, Fu = Save.FieldCount(9), Trap = Save.FieldCount(11), Far = far && On(10),
                Stove = stove && On(12), Wine = wine && On(13),
                Armed = new List<int>(),
                Gear = new int[HeroDef.All.Length],
                WeaponLv = new int[HeroDef.All.Length],
            };
            for (int i = 0; i < l.Gear.Length; i++) l.Gear[i] = -1;
            if (armWho != null && armWhat != null)
            {
                int n = armWho.Count < armWhat.Count ? armWho.Count : armWhat.Count;
                for (int i = 0; i < n; i++)
                {
                    int who = armWho[i];
                    int what = armWhat[i];
                    if (who < 0 || who >= l.Gear.Length) continue;
                    l.Gear[who] = what;
                    l.WeaponLv[who] = Save.WeaponCount(what);
                    l.Armed.Add(who);
                }
            }
            return l;
        }

        public void ClearItems()
        {
            gang = flag = marsh = stele = xuming = far = stove = wine = false;
            menghan = tianshu = baozi = shovels = fu = trap = 0;
            if (activeOn != null) activeOn.Clear();
            activeSet = true;
            if (passiveOn != null) passiveOn.Clear();
            passiveSet = true;
        }

        public bool HasAnyItem => gang || flag || marsh || stele || xuming || far || stove || wine || menghan > 0 || tianshu > 0 || baozi > 0 || shovels > 0 || fu > 0 || trap > 0;
    }

    public static class Save
    {
        const string Key = "rescue_save_v1";
        public static SaveData D { get; private set; }

        public const int Version = 4;

        public static string RankName => RankRules.Name(D.tier);
        public static int Major => RankRules.Major(D.tier);

        public static bool HasLocal => PlayerPrefs.HasKey(Key);
        public static string Raw => PlayerPrefs.GetString(Key, "");

        public static void Load()
        {
            D = Parse(PlayerPrefs.GetString(Key, ""));
            Normalize();
        }

        /// <summary>用云端原文覆盖本地。失败时保持内存里的当前档。</summary>
        public static bool ApplyCloud(string json)
        {
            var next = Parse(json);
            if (next == null) return false;
            D = next;
            Normalize();
            Persist();
            return true;
        }

        static SaveData Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<SaveData>(json); }
            catch (Exception) { return null; }
        }

        static void Normalize()
        {
            if (D == null) D = new SaveData();
            if (D.heroes == null) D.heroes = new List<int>();
            if (D.shopOffer == null) D.shopOffer = new List<int>();
            if (D.shopSold == null) D.shopSold = new List<int>();
            if (D.shopFree == null) D.shopFree = new List<int>();
            if (D.joined == null) D.joined = new List<int>();
            if (D.weapons == null) D.weapons = new List<int>();
            // 旧规则没过关也会掉兵器，清掉还没入伙的人那几件。
            D.weapons.RemoveAll(id => id < 0 || id >= HeroDef.All.Length || !Joined(id));
            if (D.armed == null) D.armed = new List<int>();
            if (D.armWho == null) D.armWho = new List<int>();
            if (D.armWhat == null) D.armWhat = new List<int>();
            if (D.ver < 3 && D.armWho.Count == 0)
            {
                foreach (var id in D.armed)
                {
                    D.armWho.Add(id);
                    D.armWhat.Add(id);
                }
            }
            if (D.team == null) D.team = new List<int>();
            if (D.ver < Version)
            {
                if (D.ver < 2)
                {
                    D.tier = RankRules.FromLegacy(D.tier);
                    D.claimTier = RankRules.FromLegacy(D.claimTier);
                    D.stars = Mathf.Clamp(D.stars, 0, RankRules.StarsAt(D.tier));
                    D.unlocked = 0;
                }
                // 4 起被动一场只带四件：老档没选过，按稀有度挑最好的四件带上，其余留在背包。
                if (D.ver < 4) D.passiveSet = false;
                D.ver = Version;
            }
            if (D.passiveOn == null) D.passiveOn = new List<int>();
            if (D.adScene == null) D.adScene = new List<string>();
            if (D.adCount == null) D.adCount = new List<int>();
            if (D.codexPaid == null) D.codexPaid = new List<int>();
            if (D.region == null) D.region = "";
            D.tier = RankRules.Clamp(D.tier);
            D.unlocked = Mathf.Clamp(D.unlocked, 0, ChapterDef.StoryCount);
            D.team.RemoveAll(id => !Joined(id));
            if (D.endlessRound < 1) D.endlessRound = 1;
            if (D.staminaRule < 1)
            {
                if (D.stamina < Stamina.Max) D.stamina = Stamina.Max;
                D.staminaRule = 1;
            }
            Sfx.On = D.sfx;
            TidyGear();
            PrepareActiveField();
            PreparePassiveField();
        }

        /// <summary>自动截图等验收模式下不落盘，避免污染真实存档。</summary>
        public static bool Volatile;

        public static void Write()
        {
            if (Volatile || D == null) return;
            Persist();
            CloudSync.Touch();
        }

        static void Persist()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
        }

        /// <summary>按北京时间算日子。拿到服务器时间后用服务器的，改手机时钟刷不了每日次数。</summary>
        public static string Today => LiveConfig.Now.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMdd");

        /// <summary>过零点才清背包。打完一局只换货架上的新货，已经买下的留到当天结束。</summary>
        public static void RefreshShopDay()
        {
            if (D.lottoDay != Today)
            {
                D.lottoDay = Today;
                D.lottoUsed = 0;
            }
            bool newDay = D.shopDay != Today;
            bool newMatch = D.shopBattle != D.battles || D.shopOffer.Count != OfferCount;
            if (newDay && !string.IsNullOrEmpty(D.shopDay)) D.ClearItems();
            else RestoreBoughtItems();
            if (!newDay && !newMatch) return;
            D.shopDay = Today;
            D.shopBattle = D.battles;
            D.shopSold.Clear();
            D.shopFree.Clear();
            D.shopOffer.Clear();
            var rng = new System.Random(int.Parse(Today) * 397 + D.battles * 17 + 11);
            void Pick(bool active, int n)
            {
                var pool = new List<int>();
                foreach (var it in ShopItem.All)
                    if (it.Active == active && (active || !it.Owned())) pool.Add(it.Id);
                for (int i = 0; i < n && pool.Count > 0; i++)
                {
                    int k = rng.Next(pool.Count);
                    D.shopOffer.Add(pool[k]);
                    pool.RemoveAt(k);
                }
            }
            Pick(true, 2);
            int before = D.shopOffer.Count;
            Pick(false, OfferCount - 2);
            if (D.shopOffer.Count == before) Pick(true, OfferCount - D.shopOffer.Count);
            Write();
        }

        /// <summary>旧版本一进对局就清背包，货架却还写着已售完。同一天再进店时把买过、看过广告的货补回背包。</summary>
        static void RestoreBoughtItems()
        {
            bool changed = false;
            void Back(List<int> ids)
            {
                if (ids == null) return;
                foreach (var id in ids)
                {
                    if (id < 0 || id >= ShopItem.All.Length) continue;
                    var it = ShopItem.All[id];
                    if (it.Count() > 0) continue;
                    it.Give();
                    changed = true;
                }
            }
            Back(D.shopSold);
            Back(D.shopFree);
            if (changed) Write();
        }

        public const int OfferCount = 3;
        public const int LottoPerDay = 3;

        public static int LottoLeft => D.lottoDay == Today ? Mathf.Max(0, LottoPerDay - D.lottoUsed) : LottoPerDay;

        /// <summary>抽奖九宫格外圈八件，每天换一批。</summary>
        public static List<int> LottoPool()
        {
            var ids = new List<int>();
            foreach (var it in ShopItem.All) ids.Add(it.Id);
            var rng = new System.Random(int.Parse(Today) * 7 + 3);
            for (int i = ids.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (ids[i], ids[j]) = (ids[j], ids[i]);
            }
            return ids.GetRange(0, Mathf.Min(8, ids.Count));
        }

        public static bool Tut(int bit) => (D.tutorial & (1 << bit)) != 0;
        public static void SetTut(int bit) { D.tutorial |= 1 << bit; Write(); }

        public struct RankDelta
        {
            public bool Promoted, Demoted, DroppedStar;
            public int OldTier, OldStars;
        }

        /// <summary>胜加星，第五颗星升一小阶；败掉星，没星再败降一小阶（不跌出大阶）。</summary>
        public static RankDelta ApplyResult(bool win)
        {
            var d = new RankDelta { OldTier = D.tier, OldStars = D.stars };
            D.battles++;
            if (win) D.wins++;
            int tier = D.tier, stars = D.stars;
            RankRules.Apply(ref tier, ref stars, win, out d.Promoted, out d.Demoted);
            d.DroppedStar = d.Demoted || (!win && stars < d.OldStars);
            D.tier = tier;
            D.stars = stars;
            return d;
        }

        /// <summary>跨天记下昨日座次。老存档当天补发一次，全新存档不发。</summary>
        public static bool RollDaily()
        {
            if (D.claimDay == Today) return !D.claimTaken;
            bool first = string.IsNullOrEmpty(D.claimDay);
            D.claimTier = RankRules.Clamp(D.tier);
            D.claimStars = Mathf.Clamp(D.stars, 0, 99);
            D.claimDay = Today;
            D.claimTaken = first && D.battles <= 0;
            Write();
            return !D.claimTaken;
        }

        public static int DailyCoins => 8 + RankRules.Major(D.claimTier) * 8 + RankRules.Sub(D.claimTier) * 3 + Mathf.Min(D.claimStars, 10) * 2;

        public static bool Joined(int id) => D.joined.Contains(id);
        public static bool Joined(HeroId id) => D.joined.Contains((int)id);
        public static int JoinedCount => D.joined.Count;
        public static bool StoryDone => D.unlocked >= ChapterDef.StoryCount;
        /// <summary>一百单八将（不算隐藏的武大郎）是否聚齐。</summary>
        public static bool AllSeated
        {
            get
            {
                foreach (var d in HeroDef.All) if (d.Rank > 0 && !Joined(d.Id)) return false;
                return true;
            }
        }

        /// <summary>赢下剧情回：解锁下一回，回目里的好汉入伙。返回新入伙的人。</summary>
        public static List<HeroId> WinStory(int chapter)
        {
            var fresh = new List<HeroId>();
            if (chapter < 0 || chapter >= ChapterDef.StoryCount) return fresh;
            D.unlocked = Mathf.Max(D.unlocked, chapter + 1);
            foreach (var id in ChapterDef.All[chapter].Joins)
                if (!Joined(id)) { D.joined.Add((int)id); fresh.Add(id); }
            return fresh;
        }

        /// <summary>从当前这一回起，连续按通关结算若干回。不落盘，调用方自己 Write。</summary>
        public static int SkipStory(int count)
        {
            int n = 0;
            for (int i = 0; i < count && D.unlocked < ChapterDef.StoryCount; i++)
            {
                WinStory(D.unlocked);
                n++;
            }
            return n;
        }

        /// <summary>聚义厅阵容：存档里的五人，不足就从已入伙里随机补。</summary>
        public static List<HeroId> Team()
        {
            var t = new List<HeroId>();
            foreach (var id in D.team) if (Joined(id) && t.Count < ChapterDef.TeamSize) t.Add((HeroId)id);
            if (t.Count < ChapterDef.TeamSize) foreach (var id in RandomTeam(t)) if (t.Count < ChapterDef.TeamSize && !t.Contains(id)) t.Add(id);
            return t;
        }

        /// <summary>随机五将：至少三个能出手的，名字尽量不重字，凑字更顺。</summary>
        public static List<HeroId> RandomTeam(List<HeroId> keep = null)
        {
            var pick = keep == null ? new List<HeroId>() : new List<HeroId>(keep);
            var pool = new List<HeroId>();
            foreach (var id in D.joined) if (!pick.Contains((HeroId)id) && !HeroDef.Get((HeroId)id).Hidden) pool.Add((HeroId)id);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            int fighters = 0;
            foreach (var id in pick) if (!HeroDef.Get(id).NoAttack) fighters++;
            foreach (var id in pool)
            {
                if (pick.Count >= ChapterDef.TeamSize) break;
                var d = HeroDef.Get(id);
                int slotsLeft = ChapterDef.TeamSize - pick.Count;
                if (d.NoAttack && fighters + slotsLeft <= 3) continue;
                pick.Add(id);
                if (!d.NoAttack) fighters++;
            }
            return pick;
        }

        public const int MaxActiveField = 2;

        static int ActiveStock(int id)
        {
            switch (id)
            {
                case 4: return D.menghan;
                case 5: return D.tianshu;
                case 6: return D.baozi;
                case 9: return D.fu;
                case 11: return D.trap;
                default: return 0;
            }
        }

        /// <summary>带上场的件数。没选中的主动道具留在背包，这场不生效。</summary>
        public static int FieldCount(int id)
        {
            PrepareActiveField();
            return D.activeOn.Contains(id) ? ActiveStock(id) : 0;
        }

        public static bool ActiveFielded(int id)
        {
            PrepareActiveField();
            return D.activeOn.Contains(id);
        }

        /// <summary>买到或退掉之后调用。空位够就自动带上，满了就只进背包。</summary>
        public static void NoteActive(int id)
        {
            if (!D.activeSet) PrepareActiveField();
            if (D.activeOn == null) D.activeOn = new List<int>();
            D.activeSet = true;
            if (ActiveStock(id) <= 0)
            {
                D.activeOn.Remove(id);
                return;
            }
            if (!D.activeOn.Contains(id) && D.activeOn.Count < MaxActiveField) D.activeOn.Add(id);
        }

        /// <summary>背包里点一下：带上或拿下。场上同时最多两件。</summary>
        public static bool ToggleActiveField(int id, out string err)
        {
            err = null;
            PrepareActiveField();
            if (ActiveStock(id) <= 0) { err = "还没有这件"; return false; }
            if (D.activeOn.Contains(id))
            {
                D.activeOn.Remove(id);
                Write();
                return true;
            }
            if (D.activeOn.Count >= MaxActiveField) { err = "一场最多带两个主动技能"; return false; }
            D.activeOn.Add(id);
            Write();
            return true;
        }

        static void PrepareActiveField()
        {
            if (D.activeOn == null) D.activeOn = new List<int>();
            D.activeOn.RemoveAll(id => ActiveStock(id) <= 0);
            var seen = new HashSet<int>();
            D.activeOn.RemoveAll(id => !seen.Add(id));
            while (D.activeOn.Count > MaxActiveField) D.activeOn.RemoveAt(D.activeOn.Count - 1);
            if (D.activeSet) return;
            foreach (var it in ShopItem.All)
            {
                if (!it.Active || it.Count() <= 0 || D.activeOn.Contains(it.Id)) continue;
                D.activeOn.Add(it.Id);
                if (D.activeOn.Count >= MaxActiveField) break;
            }
            D.activeSet = true;
        }

        // ───────────── 被动：一场最多四件 ─────────────

        public const int MaxPassiveField = 4;
        /// <summary>背包被动一行七格。</summary>
        public const int MaxPassiveBag = 7;

        static bool PassiveOwned(int id) => id >= 0 && id < ShopItem.All.Length && !ShopItem.All[id].Active && ShopItem.All[id].Count() > 0;

        public static int PassiveKinds
        {
            get
            {
                int n = 0;
                foreach (var it in ShopItem.All) if (!it.Active && it.Count() > 0) n++;
                return n;
            }
        }

        public static int PassiveFieldCount
        {
            get
            {
                PreparePassiveField();
                return D.passiveOn.Count;
            }
        }

        public static bool PassiveFielded(int id)
        {
            PreparePassiveField();
            return D.passiveOn.Contains(id);
        }

        /// <summary>买到或退掉被动之后调用。上场没满就自动带上。</summary>
        public static void NotePassive(int id)
        {
            if (!D.passiveSet) PreparePassiveField();
            D.passiveSet = true;
            if (!PassiveOwned(id))
            {
                D.passiveOn.Remove(id);
                return;
            }
            if (!D.passiveOn.Contains(id) && D.passiveOn.Count < MaxPassiveField) D.passiveOn.Add(id);
        }

        public static bool TogglePassiveField(int id, out string err)
        {
            err = null;
            PreparePassiveField();
            if (!PassiveOwned(id)) { err = "还没有这件"; return false; }
            if (D.passiveOn.Remove(id)) { Write(); return true; }
            if (D.passiveOn.Count >= MaxPassiveField) { err = "一场最多带四个被动，先点掉一个"; return false; }
            D.passiveOn.Add(id);
            Write();
            return true;
        }

        static int RarityRank(ShopItem it) => Array.IndexOf(ShopItem.Rarities, it.Rarity);

        static void PreparePassiveField()
        {
            if (D.passiveOn == null) D.passiveOn = new List<int>();
            D.passiveOn.RemoveAll(id => !PassiveOwned(id));
            var seen = new HashSet<int>();
            D.passiveOn.RemoveAll(id => !seen.Add(id));
            while (D.passiveOn.Count > MaxPassiveField) D.passiveOn.RemoveAt(D.passiveOn.Count - 1);
            if (D.passiveSet) return;
            var owned = new List<ShopItem>();
            foreach (var it in ShopItem.All) if (!it.Active && it.Count() > 0 && !D.passiveOn.Contains(it.Id)) owned.Add(it);
            owned.Sort((a, b) => RarityRank(b) != RarityRank(a) ? RarityRank(b).CompareTo(RarityRank(a)) : b.Price.CompareTo(a.Price));
            foreach (var it in owned)
            {
                if (D.passiveOn.Count >= MaxPassiveField) break;
                D.passiveOn.Add(it.Id);
            }
            D.passiveSet = true;
        }

        // ───────────── 广告：各入口每天的次数 ─────────────

        public static int AdUsed(string scene)
        {
            if (D.adDay != Today) return 0;
            int i = D.adScene.IndexOf(scene);
            return i < 0 || i >= D.adCount.Count ? 0 : D.adCount[i];
        }

        public static void NoteAd(string scene)
        {
            if (D.adDay != Today)
            {
                D.adDay = Today;
                D.adScene.Clear();
                D.adCount.Clear();
            }
            int i = D.adScene.IndexOf(scene);
            if (i < 0 || i >= D.adCount.Count)
            {
                D.adScene.Add(scene);
                D.adCount.Add(1);
            }
            else D.adCount[i]++;
            Write();
        }

        // ───────────── 好汉谱入伙赏钱 ─────────────

        public const int CodexCoinsTiangang = 20;
        public const int CodexCoinsDisha = 10;

        public static int CodexCoins(HeroDef d) => d.Rank <= 0 ? CodexCoinsDisha : d.Disha ? CodexCoinsDisha : CodexCoinsTiangang;

        public static bool CodexClaimable(int id) => Joined(id) && !D.codexPaid.Contains(id);

        public static int CodexPending
        {
            get
            {
                int n = 0;
                foreach (var id in D.joined) if (!D.codexPaid.Contains(id)) n++;
                return n;
            }
        }

        /// <summary>领一位好汉的入伙赏钱。老玩家已入伙的人也能补领一次。</summary>
        public static int ClaimCodex(int id)
        {
            if (!CodexClaimable(id)) return 0;
            int coins = CodexCoins(HeroDef.Get((HeroId)id));
            D.codexPaid.Add(id);
            D.coins += coins;
            Write();
            return coins;
        }

        public static int ClaimAllCodex()
        {
            int total = 0;
            foreach (var id in new List<int>(D.joined))
            {
                if (D.codexPaid.Contains(id)) continue;
                total += CodexCoins(HeroDef.Get((HeroId)id));
                D.codexPaid.Add(id);
            }
            if (total <= 0) return 0;
            D.coins += total;
            Write();
            return total;
        }

        // ───────────── 每日一战 ─────────────

        /// <summary>打过第五回才开放。</summary>
        public const int DailyUnlock = 5;
        public const int DailyCoinsReward = 60;

        public static bool DailyOpen => D.unlocked >= DailyUnlock;
        public static bool DailyWonToday => D.dailyDay == Today && D.dailyWon;

        public static void NoteDailyTry()
        {
            if (D.dailyDay != Today) { D.dailyDay = Today; D.dailyWon = false; D.dailyTries = 0; }
            D.dailyTries++;
            Write();
        }

        /// <summary>每日一战赢了：当天头一回给铜钱，返回是否首胜。座次不动。</summary>
        public static bool WinDaily()
        {
            if (D.dailyDay != Today) { D.dailyDay = Today; D.dailyTries = 0; D.dailyWon = false; }
            if (D.dailyWon) return false;
            D.dailyWon = true;
            D.coins += DailyCoinsReward;
            Write();
            return true;
        }

        public static int WeaponCount(int hero)
        {
            if (D.weapons == null) return 0;
            int n = 0;
            foreach (var id in D.weapons) if (id == hero) n++;
            return n;
        }

        public static int ArmedCount => D.armWho == null ? 0 : D.armWho.Count;

        /// <summary>这件兵器现在在谁身上。没有人带时返回 -1。</summary>
        public static int WearerOf(int weapon)
        {
            if (D.armWhat == null) return -1;
            int i = D.armWhat.IndexOf(weapon);
            if (i < 0 || i >= D.armWho.Count) return -1;
            return D.armWho[i];
        }

        /// <summary>这位好汉带着哪件兵器。空手返回 -1。</summary>
        public static int WeaponOn(int hero)
        {
            if (D.armWho == null) return -1;
            int i = D.armWho.IndexOf(hero);
            if (i < 0 || i >= D.armWhat.Count) return -1;
            return D.armWhat[i];
        }

        public static void UnequipWeapon(int weapon)
        {
            if (D.armWhat == null) return;
            int i = D.armWhat.IndexOf(weapon);
            if (i < 0) return;
            D.armWhat.RemoveAt(i);
            D.armWho.RemoveAt(i);
        }

        /// <summary>把兵器交给一位已入伙的好汉。他原来带着的、以及这件原来的主人，都会卸下来。</summary>
        public static bool Equip(int hero, int weapon, out string err)
        {
            err = null;
            if (WeaponCount(weapon) <= 0) { err = "还没缴获这件兵器"; return false; }
            if (hero < 0 || hero >= HeroDef.All.Length || !Joined((HeroId)hero)) { err = "这位好汉还没入伙"; return false; }
            if (string.IsNullOrEmpty(HeroDef.Get((HeroId)weapon).Weapon)) { err = "这件兵器不能带"; return false; }
            int curWho = WearerOf(weapon);
            int curWhat = WeaponOn(hero);
            if (curWho == hero) return true;
            bool newSlot = curWhat < 0 && curWho < 0;
            if (newSlot && ArmedCount >= MaxArmed) { err = "最多三位好汉同时带兵器"; return false; }
            if (curWho >= 0) UnequipWeapon(weapon);
            int still = WeaponOn(hero);
            if (still >= 0) UnequipWeapon(still);
            D.armWho.Add(hero);
            D.armWhat.Add(weapon);
            TidyGear();
            Write();
            return true;
        }

        /// <summary>丢掉已经不在背包里、或人还没入伙的搭配。同一件、同一个人只留第一份。</summary>
        public static void TidyGear()
        {
            if (D.armWho == null) D.armWho = new List<int>();
            if (D.armWhat == null) D.armWhat = new List<int>();
            int n = D.armWho.Count < D.armWhat.Count ? D.armWho.Count : D.armWhat.Count;
            var who = new List<int>();
            var what = new List<int>();
            var seenWho = new HashSet<int>();
            var seenWhat = new HashSet<int>();
            for (int i = 0; i < n && who.Count < MaxArmed; i++)
            {
                int h = D.armWho[i];
                int w = D.armWhat[i];
                if (h < 0 || h >= HeroDef.All.Length || !Joined((HeroId)h)) continue;
                if (w < 0 || w >= HeroDef.All.Length || WeaponCount(w) <= 0) continue;
                if (string.IsNullOrEmpty(HeroDef.Get((HeroId)w).Weapon)) continue;
                if (!seenWho.Add(h) || !seenWhat.Add(w)) continue;
                who.Add(h);
                what.Add(w);
            }
            D.armWho = who;
            D.armWhat = what;
        }

        public const int MaxArmed = 3;
        public const int RecycleCoins = 22;

        public const int MaxWeapons = 60;

        /// <summary>这一局缴获的兵器。没掉到时 Text 为空；囊满时 Text 是换成铜钱，IsWeapon 为假。</summary>
        public readonly struct WeaponFind
        {
            public readonly string Text;
            public readonly HeroId Hero;
            public readonly bool IsWeapon;
            public WeaponFind(string text, HeroId hero, bool weapon) { Text = text; Hero = hero; IsWeapon = weapon; }
        }

        /// <summary>撑到第四波掉一件兵器：只掉已经入伙的，凑出来的优先，品级越高越难掉。过关入伙发生在这一步之前。</summary>
        public static WeaponFind RollWeapon(BattleResult r)
        {
            bool sure = r.GmDrop || r.SureDrop;
            if (r.Quit || (!sure && r.Wave < 4)) return default;
            var cands = new List<HeroId>();
            Weapons.CollectDropCandidates(r.Formed, r.Pool, Joined, cands);
            if (cands.Count == 0 && r.SureDrop) foreach (var id in D.joined) if (!string.IsNullOrEmpty(HeroDef.Get((HeroId)id).Weapon)) cands.Add((HeroId)id);
            if (cands.Count == 0 || (!sure && UnityEngine.Random.value > 0.72f)) return default;
            int total = 0;
            foreach (var id in cands) total += Weapons.DropWeight(HeroDef.Get(id).WGrade);
            int x = UnityEngine.Random.Range(0, total);
            HeroId pick = cands[0];
            foreach (var id in cands)
            {
                x -= Weapons.DropWeight(HeroDef.Get(id).WGrade);
                if (x < 0) { pick = id; break; }
            }
            if (D.weapons.Count >= MaxWeapons)
            {
                D.coins += RecycleCoins;
                return new WeaponFind("兵器囊已满，换成铜钱 " + RecycleCoins, pick, false);
            }
            D.weapons.Add((int)pick);
            var d = HeroDef.Get(pick);
            return new WeaponFind(d.Weapon, pick, true);
        }
    }

    public static class Stamina
    {
        public const int Max = 30;
        public const int Cost = 5;
        public const int RegenSeconds = 300;

        static long Now => LiveConfig.Now.ToUnixTimeSeconds();

        public static int Current
        {
            get
            {
                Tick();
                return Save.D.stamina;
            }
        }

        public static int SecondsToNext
        {
            get
            {
                Tick();
                if (Save.D.stamina >= Max) return 0;
                return (int)(RegenSeconds - (Now - Save.D.staminaAt));
            }
        }

        static void Tick()
        {
            var d = Save.D;
            if (d.stamina >= Max) { d.staminaAt = Now; return; }
            long gained = (Now - d.staminaAt) / RegenSeconds;
            if (gained <= 0) return;
            d.stamina = (int)Math.Min(Max, d.stamina + gained);
            d.staminaAt += gained * RegenSeconds;
        }

        public static bool Spend()
        {
            Tick();
            if (Save.D.stamina < Cost) return false;
            if (Save.D.stamina >= Max) Save.D.staminaAt = Now;
            Save.D.stamina -= Cost;
            Save.Write();
            return true;
        }

        public static void Add(int n)
        {
            Tick();
            Save.D.stamina = Math.Min(Max + 5, Save.D.stamina + n);
            Save.Write();
        }
    }
}
