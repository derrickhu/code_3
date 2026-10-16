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
        public string shopDay = "";
        public List<int> shopOffer = new List<int>();
        public List<int> shopSold = new List<int>();
        public bool gang, flag, marsh, stele, xuming, far;
        public int menghan, tianshu, baozi, shovels, fu, trap;
        public bool sfx = true;
        public int wins, battles;
        public List<int> weapons = new List<int>();
        public List<int> armed = new List<int>();
        public string claimDay = "";
        public int claimTier, claimStars;
        public bool claimTaken = true;

        public Loadout PeekLoadout()
        {
            var l = new Loadout
            {
                Gang = gang, Flag = flag, Marsh = marsh, Stele = stele, MengHan = menghan, TianShu = tianshu,
                Baozi = baozi, Xuming = xuming, Shovels = shovels, Fu = fu, Trap = trap, Far = far,
                Armed = armed == null ? new List<int>() : new List<int>(armed),
                WeaponLv = new int[HeroDef.All.Length],
            };
            if (armed != null)
                foreach (var id in armed)
                    if (id >= 0 && id < l.WeaponLv.Length) l.WeaponLv[id] = Save.WeaponCount(id);
            return l;
        }

        public Loadout TakeLoadout()
        {
            var l = PeekLoadout();
            ClearItems();
            return l;
        }

        public void ClearItems()
        {
            gang = flag = marsh = stele = xuming = far = false;
            menghan = tianshu = baozi = shovels = fu = trap = 0;
        }

        public bool HasAnyItem => gang || flag || marsh || stele || xuming || far || menghan > 0 || tianshu > 0 || baozi > 0 || shovels > 0 || fu > 0 || trap > 0;
    }

    public static class Save
    {
        const string Key = "rescue_save_v1";
        public static SaveData D { get; private set; }

        public static readonly string[] Tiers = { "喽啰", "小头目", "步军头领", "马军头领", "八骠骑", "五虎将", "天罡星" };

        public static string RankName => Tiers[Mathf.Clamp(D.tier, 0, Tiers.Length - 1)];

        public static void Load()
        {
            var json = PlayerPrefs.GetString(Key, "");
            try { D = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json); }
            catch (Exception) { D = new SaveData(); }
            if (D == null) D = new SaveData();
            if (D.heroes == null) D.heroes = new List<int>();
            if (D.shopOffer == null) D.shopOffer = new List<int>();
            if (D.shopSold == null) D.shopSold = new List<int>();
            if (D.weapons == null) D.weapons = new List<int>();
            if (D.armed == null) D.armed = new List<int>();
            D.armed.RemoveAll(id => WeaponCount(id) <= 0 || id < 0 || id >= HeroDef.All.Length);
            Sfx.On = D.sfx;
        }

        /// <summary>自动截图等验收模式下不落盘，避免污染真实存档。</summary>
        public static bool Volatile;

        public static void Write()
        {
            if (Volatile) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
        }

        public static string Today => DateTime.Now.ToString("yyyyMMdd");

        /// <summary>神秘商人每天换货；没用掉的道具过了今天作废。</summary>
        public static void RefreshShopDay()
        {
            if (D.shopDay == Today && D.shopOffer.Count == OfferCount) return;
            if (!string.IsNullOrEmpty(D.shopDay) && D.shopDay != Today) D.ClearItems();
            D.shopDay = Today;
            D.shopSold.Clear();
            D.shopOffer.Clear();
            var rng = new System.Random(int.Parse(Today));
            void Pick(bool active, int n)
            {
                var pool = new List<int>();
                foreach (var it in ShopItem.All) if (it.Active == active) pool.Add(it.Id);
                for (int i = 0; i < n && pool.Count > 0; i++)
                {
                    int k = rng.Next(pool.Count);
                    D.shopOffer.Add(pool[k]);
                    pool.RemoveAt(k);
                }
            }
            Pick(true, 2);
            Pick(false, OfferCount - 2);
            Write();
        }

        public const int OfferCount = 3;

        public static bool Tut(int bit) => (D.tutorial & (1 << bit)) != 0;
        public static void SetTut(int bit) { D.tutorial |= 1 << bit; Write(); }

        public struct RankDelta
        {
            public bool Promoted, Demoted, DroppedStar;
            public int OldTier, OldStars;
        }

        /// <summary>胜加星，满五星再胜升阶；败掉星，零星再败掉阶。</summary>
        public static RankDelta ApplyResult(bool win)
        {
            var d = new RankDelta { OldTier = D.tier, OldStars = D.stars };
            D.battles++;
            if (win) D.wins++;
            int tier = D.tier, stars = D.stars;
            RankRules.Apply(ref tier, ref stars, win, Tiers.Length, out d.Promoted, out d.Demoted);
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
            D.claimTier = Mathf.Clamp(D.tier, 0, Tiers.Length - 1);
            D.claimStars = Mathf.Clamp(D.stars, 0, RankRules.Stars);
            D.claimDay = Today;
            D.claimTaken = first && D.battles <= 0;
            Write();
            return !D.claimTaken;
        }

        public static int DailyCoins => 8 + D.claimTier * 8 + D.claimStars * 2;

        public static int WeaponCount(int hero)
        {
            if (D.weapons == null) return 0;
            int n = 0;
            foreach (var id in D.weapons) if (id == hero) n++;
            return n;
        }

        public const int MaxArmed = 3;
        public const int RecycleCoins = 22;

        public static string RollWeapon(BattleResult r)
        {
            if (r.Quit || r.Wave < 4) return "";
            HeroId pick = default;
            bool found = false;
            foreach (var id in r.Formed)
                if (!string.IsNullOrEmpty(HeroDef.Get(id).Weapon)) { pick = id; found = true; break; }
            if (!found)
                foreach (var id in ChapterDef.All[Mathf.Clamp(r.Chapter, 0, ChapterDef.All.Length - 1)].Pool)
                    if (!string.IsNullOrEmpty(HeroDef.Get(id).Weapon)) { pick = id; found = true; break; }
            if (!found || UnityEngine.Random.value > 0.72f) return "";
            if (D.weapons.Count >= 24)
            {
                D.coins += RecycleCoins;
                return "兵器囊已满，换成铜钱 " + RecycleCoins;
            }
            D.weapons.Add((int)pick);
            var d = HeroDef.Get(pick);
            return d.Name + "的" + d.Weapon;
        }
    }

    public static class Stamina
    {
        public const int Max = 10;
        public const int RegenSeconds = 300;

        static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

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
            if (Save.D.stamina <= 0) return false;
            if (Save.D.stamina >= Max) Save.D.staminaAt = Now;
            Save.D.stamina--;
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
