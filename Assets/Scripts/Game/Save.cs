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
        public int menghan, tianshu, baozi, shovels, fu, trap, pen;
        /// <summary>山寨传家底牌：跨零点保留不清空的那件核心被动（-1 表示自动挑品质最高的一件）。</summary>
        public int heirloom = -1;
        /// <summary>跨日结算待弹出的护航提示（旧货折银数与保留的底牌名），弹过即清。</summary>
        public int carryCoinsNotice;
        public string carryItemNotice = "";
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
        /// <summary>水浒小课堂已经答对领过赏钱的回目序号（0..56）。</summary>
        public List<int> loreQuizzed = new List<int>();
        /// <summary>服务器按 IP 定的省份，只用来显示。空串表示还没联网拿到，「江湖」表示查不到省（海外等）。</summary>
        public string region = "";
        /// <summary>微信昵称。空着时排行榜用「好汉」加四位数。</summary>
        public string nick = "";
        /// <summary>微信头像地址。空着时用默认立绘。</summary>
        public string avatar = "";
        /// <summary>每日一战：今天打过没、赢过没。</summary>
        public string dailyDay = "";
        public bool dailyWon;
        public int dailyTries;
        public bool sfx = true;
        public int wins, battles;
        public List<int> weapons = new List<int>();
        /// <summary>旧档：装备自己成名兵器的好汉。版本 3 起改成 armWhat，版本 5 起改成兵器架。</summary>
        public List<int> armed = new List<int>();
        /// <summary>旧档 3、4：交给别人带的兵器。版本 5 起挪进兵器架。</summary>
        public List<int> armWhat = new List<int>();
        /// <summary>兵器架，按顺序发：值是兵器出自谁。局里凑出好汉时依次领走。</summary>
        public List<int> rack = new List<int>();
        /// <summary>兵器架有几格。起步三格，花铜钱扩到五格。</summary>
        public int rackSlots = Save.RackBase;
        /// <summary>兵器铺：今天摆出的三件、已经打过的。</summary>
        public string forgeDay = "";
        public List<int> forgeOffer = new List<int>();
        public List<int> forgeSold = new List<int>();
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
        /// <summary>好汉列传（0..35）已通关的序号。</summary>
        public List<int> bioCleared = new List<int>();
        /// <summary>水泊名役（0..11）已通关的序号。</summary>
        public List<int> campaignCleared = new List<int>();

        public Loadout PeekLoadout()
        {
            bool On(int id) => Save.PassiveFielded(id);
            var l = new Loadout
            {
                Gang = gang && On(0), Flag = flag && On(1), Marsh = marsh && On(2), Stele = stele && On(3),
                MengHan = Save.FieldCount(4), TianShu = Save.FieldCount(5), Baozi = Save.FieldCount(6),
                Xuming = xuming && On(7), Shovels = On(8) ? shovels : 0, Fu = Save.FieldCount(9), Trap = Save.FieldCount(11), Pen = Save.FieldCount(14), Far = far && On(10),
                Stove = stove && On(12), Wine = wine && On(13),
                Rack = new List<int>(),
                RackLv = new List<int>(),
            };
            if (rack != null)
                foreach (var w in rack)
                {
                    l.Rack.Add(w);
                    l.RackLv.Add(Save.WeaponCount(w));
                }
            return l;
        }

        public void ClearItems()
        {
            gang = flag = marsh = stele = xuming = far = stove = wine = false;
            menghan = tianshu = baozi = shovels = fu = trap = pen = 0;
            heirloom = -1;
            if (activeOn != null) activeOn.Clear();
            activeSet = true;
            if (passiveOn != null) passiveOn.Clear();
            passiveSet = true;
        }

        public bool HasAnyItem => gang || flag || marsh || stele || xuming || far || stove || wine || menghan > 0 || tianshu > 0 || baozi > 0 || shovels > 0 || fu > 0 || trap > 0 || pen > 0;
    }

    public static class Save
    {
        const string Key = "rescue_save_v1";
        public static SaveData D { get; private set; }

        public const int Version = 6;

        public static string RankName => RankRules.Name(D.tier);
        public static int Major => RankRules.Major(D.tier);

        public static bool HasLocal => PlayerPrefs.HasKey(Key);
        public static string Raw => PlayerPrefs.GetString(Key, "");

        public static void Load()
        {
            D = Parse(PlayerPrefs.GetString(Key, ""));
            Normalize();
        }

        /// <summary>
        /// 换成一份新档并标成刚写过。调用方再 CloudSync.FlushNow，云端一起盖掉。
        /// 只删本地的话，下次启动会把云端旧档拉回来。
        /// </summary>
        public static void Wipe()
        {
            D = new SaveData();
            Normalize();
            Write();
        }

        /// <summary>用云端原文覆盖本地。失败时保持内存里的当前档。微信头像昵称记在本机，空档盖不掉。</summary>
        public static bool ApplyCloud(string json)
        {
            var next = Parse(json);
            if (next == null) return false;
            D = next;
            Normalize();
            WxSession.MergeProfileIntoSave();
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
            if (D.armWhat == null) D.armWhat = new List<int>();
            if (D.rack == null) D.rack = new List<int>();
            if (D.forgeOffer == null) D.forgeOffer = new List<int>();
            if (D.forgeSold == null) D.forgeSold = new List<int>();
            D.rackSlots = Mathf.Clamp(D.rackSlots, RackBase, RackMax);
            // 5 起兵器不再交给某个人，带着的兵器按原顺序挂上兵器架。
            if (D.ver < 5 && D.rack.Count == 0)
                D.rack.AddRange(D.ver < 3 ? D.armed : D.armWhat);
            D.armed.Clear();
            D.armWhat.Clear();
            if (D.team == null) D.team = new List<int>();
            if (D.loreQuizzed == null) D.loreQuizzed = new List<int>();
            if (D.bioCleared == null) D.bioCleared = new List<int>();
            if (D.campaignCleared == null) D.campaignCleared = new List<int>();
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
                // 6 起主线从 57 回精补扩充至 72 回：把老档进度与已答讲古卷无损映射到新 72 回序号。
                if (D.ver > 0 && D.ver < 6)
                {
                    D.unlocked = ChapterDef.MigrateUnlocked57To72(D.unlocked);
                    var mapped = new List<int>();
                    foreach (var oldIdx in D.loreQuizzed)
                    {
                        if (oldIdx >= 0 && oldIdx < ChapterDef.Legacy57To72.Length)
                        {
                            int nextIdx = ChapterDef.Legacy57To72[oldIdx];
                            if (!mapped.Contains(nextIdx)) mapped.Add(nextIdx);
                        }
                    }
                    D.loreQuizzed = mapped;
                }
                D.ver = Version;
            }
            if (D.passiveOn == null) D.passiveOn = new List<int>();
            if (D.adScene == null) D.adScene = new List<string>();
            if (D.adCount == null) D.adCount = new List<int>();
            if (D.codexPaid == null) D.codexPaid = new List<int>();
            if (D.region == null) D.region = "";
            if (D.carryItemNotice == null) D.carryItemNotice = "";
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
            TidyRack();
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

        /// <summary>只落本地，不往云端推。把头像昵称从本机授权记回存档时用。</summary>
        public static void Remember()
        {
            if (Volatile || D == null) return;
            Persist();
        }

        static void Persist()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
        }

        /// <summary>按北京时间算日子。拿到服务器时间后用服务器的，改手机时钟刷不了每日次数。</summary>
        public static string Today => LiveConfig.Now.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMdd");

        /// <summary>过零点护航结算：保留 1 件压寨传家被动 + 1 件护航主动，其余昨日旧货按每件 15 铜钱折旧贴补（上限 60），彻底告别次日白板坐牢。</summary>
        public static void RefreshShopDay()
        {
            if (D.lottoDay != Today)
            {
                D.lottoDay = Today;
                D.lottoUsed = 0;
            }
            bool newDay = D.shopDay != Today;
            bool newMatch = D.shopBattle != D.battles || D.shopOffer.Count != OfferCount;
            if (newDay && !string.IsNullOrEmpty(D.shopDay)) RolloverItemsForNewDay();
            else RestoreBoughtItems();
            EnsureHeirloom();
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

        /// <summary>跨零点时执行一次（GM 也可手动模拟测试）：保留压寨底牌与一件护航主动，多余旧货折算启动铜钱。</summary>
        public static void RolloverItemsForNewDay()
        {
            EnsureHeirloom();
            int keptPassive = PassiveOwned(D.heirloom) ? D.heirloom : -1;
            int keptActive = -1;
            int bestActRank = -1, bestActPrice = -1;
            int totalPieces = 0;

            foreach (var it in ShopItem.All)
            {
                int cnt = it.Count();
                if (cnt <= 0) continue;
                if (!it.Active)
                {
                    totalPieces += 1;
                }
                else
                {
                    totalPieces += cnt;
                    int rr = RarityRank(it);
                    if (rr > bestActRank || (rr == bestActRank && it.Price > bestActPrice))
                    {
                        bestActRank = rr;
                        bestActPrice = it.Price;
                        keptActive = it.Id;
                    }
                }
            }

            int keptCount = (keptPassive >= 0 ? 1 : 0) + (keptActive >= 0 ? 1 : 0);
            int expiredPieces = Mathf.Max(0, totalPieces - keptCount);
            int refund = Mathf.Min(60, expiredPieces * 15);

            D.ClearItems();
            var keptNames = new List<string>();
            if (keptPassive >= 0 && keptPassive < ShopItem.All.Length)
            {
                ShopItem.All[keptPassive].Give();
                D.heirloom = keptPassive;
                keptNames.Add(ShopItem.All[keptPassive].Name);
            }
            if (keptActive >= 0 && keptActive < ShopItem.All.Length)
            {
                ShopItem.All[keptActive].Give();
                keptNames.Add(ShopItem.All[keptActive].Name);
            }
            if (refund > 0) D.coins += refund;
            D.carryCoinsNotice = refund;
            D.carryItemNotice = string.Join("、", keptNames);
            PrepareActiveField();
            PreparePassiveField();
        }

        public static void EnsureHeirloom()
        {
            if (PassiveOwned(D.heirloom)) return;
            D.heirloom = -1;
            int bestRank = -1, bestPrice = -1;
            foreach (var it in ShopItem.All)
            {
                if (it.Active || it.Count() <= 0) continue;
                int rr = RarityRank(it);
                if (rr > bestRank || (rr == bestRank && it.Price > bestPrice))
                {
                    bestRank = rr;
                    bestPrice = it.Price;
                    D.heirloom = it.Id;
                }
            }
        }

        public static bool SetHeirloom(int id)
        {
            if (!PassiveOwned(id)) return false;
            D.heirloom = id;
            Write();
            return true;
        }

        public static bool ConsumeCarryNotice(out string keptItems, out int refundCoins)
        {
            keptItems = D.carryItemNotice ?? "";
            refundCoins = D.carryCoinsNotice;
            if (string.IsNullOrEmpty(keptItems) && refundCoins <= 0) return false;
            D.carryItemNotice = "";
            D.carryCoinsNotice = 0;
            Write();
            return true;
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

        public static int DailyCoins => Economy.DailyCoins(D.claimTier, D.claimStars);

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
                case 14: return D.pen;
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

        /// <summary>买到或退掉被动之后调用。上场没满就自动带上，顺带维护压寨传家格。</summary>
        public static void NotePassive(int id)
        {
            if (!D.passiveSet) PreparePassiveField();
            D.passiveSet = true;
            if (!PassiveOwned(id))
            {
                D.passiveOn.Remove(id);
                EnsureHeirloom();
                return;
            }
            if (!D.passiveOn.Contains(id) && D.passiveOn.Count < MaxPassiveField) D.passiveOn.Add(id);
            EnsureHeirloom();
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

        // ───────────── 水浒小课堂考校赏钱 ─────────────

        public static bool LoreAnswered(int chapter) => D.loreQuizzed != null && D.loreQuizzed.Contains(chapter);

        public static int LoreAnsweredCount => D.loreQuizzed == null ? 0 : D.loreQuizzed.Count;

        public static int LoreQuizPending
        {
            get
            {
                int done = Mathf.Min(D.unlocked, ChapterDef.StoryCount);
                int n = 0;
                for (int i = 0; i < done; i++)
                    if (!LoreAnswered(i)) n++;
                return n;
            }
        }

        public static int FirstPendingLoreChapter()
        {
            int done = Mathf.Min(D.unlocked, ChapterDef.StoryCount);
            for (int i = 0; i < done; i++)
                if (!LoreAnswered(i)) return i;
            return Mathf.Clamp(done - 1, 0, ChapterDef.StoryCount - 1);
        }

        public static int ClaimLoreQuiz(int chapter)
        {
            if (chapter < 0 || chapter >= ChapterDef.StoryCount || LoreAnswered(chapter)) return 0;
            D.loreQuizzed.Add(chapter);
            D.coins += ShuihuLore.QuizRewardCoins;
            Write();
            return ShuihuLore.QuizRewardCoins;
        }

        // ───────────── 好汉列传（36 折）与水泊名役（12 幕） ─────────────

        public const int BioFirstRewardCoins = 45;
        public const int CampaignFirstRewardCoins = 80;

        public static bool BioCleared(int bioIdx) => D.bioCleared != null && D.bioCleared.Contains(bioIdx);
        public static int BioClearedCount => D.bioCleared == null ? 0 : D.bioCleared.Count;

        /// <summary>主角入伙后解锁其个人列传；前一折通关再开下一折。</summary>
        public static bool BioOpen(int bioIdx)
        {
            if (bioIdx < 0 || bioIdx >= ChapterDef.Bios.Length) return false;
            var ep = ChapterDef.BioHeroes[bioIdx / 3];
            if (!Joined((int)ep.Hero)) return false;
            int actInHero = bioIdx % 3;
            return actInHero == 0 || BioCleared(bioIdx - 1);
        }

        public static bool WinBio(int bioIdx)
        {
            if (bioIdx < 0 || bioIdx >= ChapterDef.Bios.Length) return false;
            if (D.bioCleared == null) D.bioCleared = new List<int>();
            if (D.bioCleared.Contains(bioIdx)) return false;
            D.bioCleared.Add(bioIdx);
            D.coins += BioFirstRewardCoins;
            Write();
            return true;
        }

        public static bool CampaignCleared(int camIdx) => D.campaignCleared != null && D.campaignCleared.Contains(camIdx);
        public static int CampaignClearedCount => D.campaignCleared == null ? 0 : D.campaignCleared.Count;

        public static bool CampaignOpen(int camIdx)
        {
            if (camIdx < 0 || camIdx >= ChapterDef.Campaigns.Length) return false;
            return D.unlocked > ChapterDef.CampaignMetas[camIdx].UnlockStoryIndex;
        }

        public static bool WinCampaign(int camIdx)
        {
            if (camIdx < 0 || camIdx >= ChapterDef.Campaigns.Length) return false;
            if (D.campaignCleared == null) D.campaignCleared = new List<int>();
            if (D.campaignCleared.Contains(camIdx)) return false;
            D.campaignCleared.Add(camIdx);
            D.coins += CampaignFirstRewardCoins;
            Write();
            return true;
        }

        /// <summary>某好汉成名兵器：不把玩家还没入伙的好汉通过列传名役偷渡进好汉谱，若未入伙则改选已入伙中最高品级的一把。</summary>
        public static WeaponFind GrantCampaignWeapon(HeroId preferred)
        {
            HeroId pick = preferred;
            if (!Joined((int)pick) || string.IsNullOrEmpty(HeroDef.Get(pick).Weapon))
            {
                int bestG = -1;
                foreach (var id in D.joined)
                {
                    var cand = HeroDef.Get((HeroId)id);
                    if (!string.IsNullOrEmpty(cand.Weapon) && cand.WGrade > bestG)
                    {
                        bestG = cand.WGrade;
                        pick = cand.Id;
                    }
                }
            }
            var d = HeroDef.Get(pick);
            if (!Joined((int)pick) || string.IsNullOrEmpty(d.Weapon)) return default;
            if (D.weapons.Count >= MaxWeapons)
            {
                D.coins += RecycleCoins;
                return new WeaponFind("兵器囊已满，换成铜钱 " + RecycleCoins, pick, false);
            }
            D.weapons.Add((int)pick);
            if (!OnRack((int)pick) && RackCount < D.rackSlots) D.rack.Add((int)pick);
            return new WeaponFind(d.Weapon, pick, true);
        }

        public static int GrandClearedCount => Mathf.Min(D.unlocked, ChapterDef.StoryCount) + BioClearedCount + CampaignClearedCount;

        // ───────────── 每日一战 ─────────────

        /// <summary>打过第五回才开放。</summary>
        public const int DailyUnlock = 5;
        public const int DailyCoinsReward = 60;

        public static bool DailyOpen => D.unlocked >= DailyUnlock;
        public static bool DailyWonToday => D.dailyDay == Today && D.dailyWon;
        /// <summary>每天首局挑战每日一战免收 5 点体力，体力耗尽也能免费爽打一把。</summary>
        public static bool DailyFreeToday => DailyOpen && (D.dailyDay != Today || D.dailyTries <= 0);

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

        public const int RackBase = 3;
        public const int RackMax = 5;
        public static int RackCount => D.rack == null ? 0 : D.rack.Count;
        public static bool OnRack(int weapon) => D.rack != null && D.rack.Contains(weapon);
        /// <summary>扩下一格兵器架的价钱。扩满返回 0。</summary>
        public static int RackSlotPrice => Economy.RackSlotPrice(D.rackSlots);

        public static bool BuyRackSlot(out string err)
        {
            err = null;
            int price = RackSlotPrice;
            if (price <= 0) { err = "兵器架已经扩满"; return false; }
            if (D.coins < price) { err = "铜钱不够"; return false; }
            D.coins -= price;
            D.rackSlots++;
            Write();
            return true;
        }

        /// <summary>挂到兵器架最后一格。</summary>
        public static bool PutOnRack(int weapon, out string err)
        {
            err = null;
            if (WeaponCount(weapon) <= 0) { err = "还没缴获这件兵器"; return false; }
            if (string.IsNullOrEmpty(HeroDef.Get((HeroId)weapon).Weapon)) { err = "这件兵器不能带"; return false; }
            if (OnRack(weapon)) return true;
            if (RackCount >= D.rackSlots) { err = "兵器架满了，先取下一件"; return false; }
            D.rack.Add(weapon);
            Write();
            return true;
        }

        public static void TakeOffRack(int weapon)
        {
            if (D.rack == null || !D.rack.Remove(weapon)) return;
            Write();
        }

        /// <summary>挪到兵器架第一格：局里第一个凑出来的好汉先领它。</summary>
        public static void RackToFront(int weapon)
        {
            if (D.rack == null || !D.rack.Remove(weapon)) return;
            D.rack.Insert(0, weapon);
            Write();
        }

        /// <summary>丢掉已经不在背包里、不能带、重复的；超出格数的取下。</summary>
        public static void TidyRack()
        {
            if (D.rack == null) D.rack = new List<int>();
            var keep = new List<int>();
            foreach (var w in D.rack)
            {
                if (keep.Count >= D.rackSlots) break;
                if (w < 0 || w >= HeroDef.All.Length || WeaponCount(w) <= 0) continue;
                if (string.IsNullOrEmpty(HeroDef.Get((HeroId)w).Weapon) || keep.Contains(w)) continue;
                keep.Add(w);
            }
            D.rack = keep;
        }

        public const int ForgePerDay = 3;
        public const int MaxWeaponTier = 5;

        public static int ForgePrice(int weapon) => Economy.ForgePrice(HeroDef.Get((HeroId)weapon).WGrade);

        /// <summary>兵器铺：每天从手里不满五阶的兵器里摆三件，打一次加一阶。当天不换货。</summary>
        public static List<int> ForgeOffers()
        {
            if (D.forgeDay != Today)
            {
                D.forgeDay = Today;
                D.forgeOffer.Clear();
                D.forgeSold.Clear();
                var pool = new List<int>();
                foreach (var w in D.weapons)
                    if (!pool.Contains(w) && WeaponCount(w) < MaxWeaponTier && !string.IsNullOrEmpty(HeroDef.Get((HeroId)w).Weapon)) pool.Add(w);
                pool.Sort();
                var rng = new System.Random(int.Parse(Today) * 131 + 7);
                while (D.forgeOffer.Count < ForgePerDay && pool.Count > 0)
                {
                    int k = rng.Next(pool.Count);
                    D.forgeOffer.Add(pool[k]);
                    pool.RemoveAt(k);
                }
                Write();
            }
            return D.forgeOffer;
        }

        public static bool BuyForge(int weapon, out string err)
        {
            err = null;
            if (!ForgeOffers().Contains(weapon) || D.forgeSold.Contains(weapon)) { err = "今天这件打过了"; return false; }
            if (WeaponCount(weapon) >= MaxWeaponTier) { err = "已经五阶了"; return false; }
            if (D.weapons.Count >= MaxWeapons) { err = "兵器囊满了，先回收几件"; return false; }
            int price = ForgePrice(weapon);
            if (D.coins < price) { err = "铜钱不够"; return false; }
            D.coins -= price;
            D.weapons.Add(weapon);
            D.forgeSold.Add(weapon);
            Write();
            return true;
        }

        /// <summary>回收一件给的铜钱：凡器、利器给钱；宝器、神兵只腾地方，免得刷广告抽来的钱越滚越多。</summary>
        public static int RecycleValue(int weapon) => Economy.RecycleValue(HeroDef.Get((HeroId)weapon).WGrade);
        public const int RecycleCoins = Economy.RecycleCoins;

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
            // 兵器架还有空格就直接挂上，新手不去兵器谱也能用上。
            if (!OnRack((int)pick) && RackCount < D.rackSlots) D.rack.Add((int)pick);
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
