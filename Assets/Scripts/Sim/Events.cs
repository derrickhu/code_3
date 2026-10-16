namespace Rescue.Sim
{
    public enum Ev
    {
        Recruit, Merge, HeroFormed, Attack, HeroAttack, Hit, Kill, Leak, Rain, Ult,
        WaveStart, BossSpawn, EliteWarn, Dig, Buns, ItemUsed, End, Dismiss,
        HeroLevel, Shovels, Rescue, Baozi,
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
