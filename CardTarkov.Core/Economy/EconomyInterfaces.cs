using CardTarkov.Core.Game;

namespace CardTarkov.Core.Economy
{
    /// <summary>
    /// 入场门控。v0.1 为纯客户端，空实现永远放行；
    /// 以后接 TarkFX 同款 item/moving 队列扣入场费时实现这个接口。
    /// </summary>
    public interface IEntryGate
    {
        bool TryEnter(DdzConfig config);
    }

    /// <summary>
    /// 结算出口。v0.1 空实现；以后接邮件发奖 / 服务端记账时实现。
    /// </summary>
    public interface ISettlementSink
    {
        void Settle(GameResult result);
    }

    public sealed class NullEntryGate : IEntryGate
    {
        public bool TryEnter(DdzConfig config) => true;
    }

    public sealed class NullSettlementSink : ISettlementSink
    {
        public void Settle(GameResult result)
        {
        }
    }
}
