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
        public int stars;
        public int unlocked;
        public List<int> heroes = new List<int>();
        public int tutorial;
        public int stamina = Stamina.Max;
        public long staminaAt;
        public string shopDay = "";
        public List<int> shopOffer = new List<int>();
        public List<int> shopSold = new List<int>();
        public bool gang, flag, marsh, stele;
        public int menghan, tianshu;
        public bool sfx = true;
        public int wins, battles;

        public Loadout TakeLoadout()
        {
            var l = new Loadout { Gang = gang, Flag = flag, Marsh = marsh, Stele = stele, MengHan = menghan, TianShu = tianshu };
            gang = flag = marsh = stele = false;
            menghan = tianshu = 0;
            return l;
        }

        public bool HasAnyItem => gang || flag || marsh || stele || menghan > 0 || tianshu > 0;
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

        public static bool Tut(int bit) => (D.tutorial & (1 << bit)) != 0;
        public static void SetTut(int bit) { D.tutorial |= 1 << bit; Write(); }

        /// <summary>胜 +1 星，满 5 星晋升；败 -1 星不掉阶。返回是否晋升。</summary>
        public static bool ApplyResult(bool win)
        {
            D.battles++;
            if (win)
            {
                D.wins++;
                D.stars++;
                if (D.stars >= 5 && D.tier < Tiers.Length - 1) { D.tier++; D.stars = 0; return true; }
                D.stars = Mathf.Min(D.stars, 5);
            }
            else if (D.stars > 0) D.stars--;
            return false;
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
