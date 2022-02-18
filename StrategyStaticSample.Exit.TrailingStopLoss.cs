using NinjaTrader.Custom.Extensions.Bars;
using System;

namespace NinjaTrader.Custom.Strategies
{
    // the idea of trailing stop loss is to follow an instrument value with some distance, but never move away from it
    // TSL is useful in several context:
    //   - it might be used as the actual profit order: we don't really know where the intrument value will end at, so we simply follow it until it does a distance-drawback
    //   - it might be used as the risk-mitigation: a distant TSL makes sure that whenever the instrument value moves into the desired direction, the potential loss is less
    // this particular one:
    //   - activates at certain threshold (which is defined as a % of the consolidation used to open position)
    //   - stays at the fixed distance
    public partial class StrategyStaticSample : StrategyBase<StrategyStaticSample, StrategyStaticSample.SampleSignalContext, StrategyStaticSample.SampleSignal>
    {
        public class SampleTrailingStopLoss : TrailingStopLoss
        {
            private double activationDistance;

            public SampleTrailingStopLoss(StrategyStaticSample strategy, string signalName)
                : base(strategy, signalName)
            {
            }

            protected override BarsCollection BarsCollection
            {
                get { return this.Strategy.DefaultBarsCollection; }
            }

            protected override double StopLossDistance
            {
                get { return this.Strategy.TrailingStopLossDistance; }
            }

            protected override bool Activate(Bar nextBar)
            {
                switch (this.Strategy.Position.MarketPosition)
                {
                    case Cbi.MarketPosition.Long:
                        return nextBar.High >= this.Strategy.Position.AveragePrice + activationDistance;
                    case Cbi.MarketPosition.Short:
                        return nextBar.Low <= this.Strategy.Position.AveragePrice - activationDistance;
                    default:
                        return false;
                }
            }

            public override void Initialize(SampleSignalContext signalContext, SampleSignal signal, int initialPositionSize)
            {
                base.Initialize(signalContext, signal, initialPositionSize);
                var activationPercentage = Math.Max(0, Math.Min(100, this.Strategy.TrailingStopLossActivationPercentage));
                this.activationDistance = this.SignalContext.ConsolidationRange.SizeHighLow * (activationPercentage / 100.0);
            }
        }
    }
}
