namespace CardTarkov.Core.Game
{
    /// <summary>
    /// AI 难度。v0.1 只有一档可玩策略，枚举先保留给 UI / 后续实现。
    /// </summary>
    public enum AiDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    public sealed class DdzConfig
    {
        /// <summary>实际使用的随机种子（界面留空时由 UI 生成）。</summary>
        public int Seed { get; set; }

        /// <summary>不洗牌模式：保留炸弹分组，炸弹出现率更高。</summary>
        public bool NoShuffle { get; set; }

        /// <summary>不洗牌模式的控制系数：越小炸弹越多（默认 50）。</summary>
        public int NoShuffleControlFactor { get; set; } = 50;

        /// <summary>AI 强度接口；v0.1 只有一档实现。</summary>
        public AiDifficulty AiDifficulty { get; set; } = AiDifficulty.Normal;

        /// <summary>
        /// 最大倍数接口。v0.1 不做倍数，0 表示未启用；
        /// 后续炸弹/春天/加倍实现时由结算层读取。
        /// </summary>
        public int MaxMultiplier { get; set; }
    }
}
