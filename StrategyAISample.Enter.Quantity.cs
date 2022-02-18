namespace NinjaTrader.Custom.Strategies
{
    // in this particular sample: the amount we trade is simply 100k (for simplicity)
    // to see some samples on how this can be customized please refer to a static strategy sample
    public partial class StrategyAISample : StrategyBaseAI<StrategyAISample, StrategyAISample.SampleInputVector, StrategyAISample.SampleOutputVector>
    {
        protected override PositionQuantityAdvisor CreateQuanityAdvisor()
        {
            return new FixedQuantityAdvisor(this, 100000);
        }
    }
}
