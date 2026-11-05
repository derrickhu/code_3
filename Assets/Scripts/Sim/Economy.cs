using System;

namespace Rescue.Sim
{
    /// <summary>
    /// 铜钱进出的数。道具买下当天每局都能用，过零点清掉，所以价钱按「一天的进账」定：
    /// 中期不看广告打六局，刚好买得起核心四件；常看广告的人多出来的钱，去兵器铺升阶、扩兵器架。
    /// </summary>
    public static class Economy
    {
        /// <summary>商店货价，下标是货号：生辰纲、替天行道旗、水泊、石碣、蒙汗药、天书、包子、续命丹、方便铲、攻速符、御敌千里、陷阱、十字坡灶、招贤酒。</summary>
        public static readonly int[] ItemPrices = { 100, 240, 220, 340, 140, 360, 90, 320, 160, 220, 200, 80, 180, 220 };

        /// <summary>方便铲、十字坡灶、招贤酒、蒙汗药。</summary>
        public static readonly int[] CoreItems = { 8, 12, 13, 4 };

        public static int CoreCost
        {
            get
            {
                int n = 0;
                foreach (var id in CoreItems) n += ItemPrices[id];
                return n;
            }
        }

        public static int WinCoins(int chapter, int round) =>
            round > 0 ? 40 + 5 * Math.Min(round, 30) : 30 + 4 * Math.Min(chapter, 45);

        public static int LoseCoins(int wave) => 8 + wave;

        /// <summary>俸禄：按昨日座次，天魁星满星约六十。</summary>
        public static int DailyCoins(int tier, int stars) =>
            6 + RankRules.Major(tier) * 4 + RankRules.Sub(tier) + Math.Min(Math.Max(stars, 0), 10);

        /// <summary>抽到放不下的货，退两成。</summary>
        public static int LottoRefund(int price) => price / 5;

        public const int RecycleCoins = 22;

        /// <summary>凡器、利器回收给钱；宝器、神兵只腾地方。</summary>
        public static int RecycleValue(int grade) => grade <= 1 ? RecycleCoins : 0;

        public static int ForgePrice(int grade) => grade <= 0 ? 600 : grade == 1 ? 800 : grade == 2 ? 1100 : 1500;

        /// <summary>兵器架从 slots 格扩到下一格的价钱。扩满返回 0。</summary>
        public static int RackSlotPrice(int slots) => slots <= 3 ? 3000 : slots == 4 ? 8000 : 0;
    }
}
