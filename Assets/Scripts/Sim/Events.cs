namespace Rescue.Sim
{
    public enum Ev
    {
        Recruit, Merge, HeroFormed, Attack, HeroAttack, Hit, Kill, Leak, Rain, Ult,
        WaveStart, BossSpawn, EliteWarn, Dig, Buns, ItemUsed, End, Dismiss,
        HeroLevel, Shovels, Rescue, Baozi, Bond, Heal, Steal,
        /// <summary>头目绝技落在某一处：B 是 BossSkill，P 是落点，Q 是头目所在，Val 是持续秒数或数量。</summary>
        BossHex,
        /// <summary>头目血量过半，狂暴。</summary>
        BossRage,
    }

    /// <summary>A 通常是棋子 / 敌人 / 好汉的 Id；P、Q 是所在半区自己的格子坐标。</summary>
    public struct SimEvent
    {
        public Ev Type;
        public int Side;
        public int A, B;
        public V2 P, Q;
        public float Val;
        public int Flag;
        public string Text;
    }
}
