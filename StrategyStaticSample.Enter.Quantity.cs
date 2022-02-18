using System;
using System.Linq;

namespace NinjaTrader.Custom.Strategies
{
    public partial class StrategyStaticSample : StrategyBase<StrategyStaticSample, StrategyStaticSample.SampleSignalContext, StrategyStaticSample.SampleSignal>
    {
        private SampleStopLoss stopLoss;

        // in our strategy we know where the stop loss will be placed in case we will actually open a position
        // this particular method says what will be the distance to that stop loss order
        // SPECIAL NOTE: this is an approximation: we have not opened a position just yet, so the open price may vary;
        //               here we assume that open price will be equal to the latest close
        //               (which will rarely be the case, but it should be close enough; unless the intention is to play on very short ranges the approx. should be OK)
        private double GetPotentialLossDistance(OpenRequest open)
        {
            if (this.stopLoss == null)
            {
                this.stopLoss = this.ExitSignal.AllSignals.OfType<SampleStopLoss>().First();
            }

            var stopLossLevel = this.stopLoss.GetStopLossLevel(open.MarketPosition, open.SignalContext.ConsolidationRange);
            return Math.Abs(this.DefaultBarsCollection.AllBars.First().Close - stopLossLevel);
        }

        // this particular implementation will calculate the size of position to be opened in a way that if we hit the SL order we will always loose the same amount of money
        // (see the base class if you want to know how it does it)
        public class FixedLossQuantityAdvisor : FixedRiskQuantityAdvisor
        {
            public FixedLossQuantityAdvisor(StrategyStaticSample strategy)
                : base(strategy, strategy.QuantityFixedLoss)
            {
            }

            protected override double GetPotentialLossDistance(OpenRequest open)
            {
                return this.Strategy.GetPotentialLossDistance(open);
            }
        }

        // this particular implemenatation will calculate the size of position to be opened in a way that we always loose the same portion of money we've got
        // (so, it risks a fixed % of our investment wallet value)
        // (see the base class if you want to know how it does it)
        public class PercentageLossQuantityAdvisor : PercentageRiskQuantityAdvisor
        {
            public PercentageLossQuantityAdvisor(StrategyStaticSample strategy)
                : base(strategy, strategy.QuantityPercentageLoss / 100.0)
            {
            }

            protected override double GetPotentialLossDistance(OpenRequest open)
            {
                return this.Strategy.GetPotentialLossDistance(open);
            }
        }

        // an example of how we can customize various risk strategies : simply be changing the configuration param
        // we can change the risk strategy, so we can quickly test how various options would affect the strategy outcome
        protected override PositionQuantityAdvisor CreateQuanityAdvisor()
        {
            switch (this.QuantityRiskType)
            {
                case TradeRiskType.FixedSize:
                    return new FixedQuantityAdvisor(this, this.QuantityFixedSize);
                case TradeRiskType.FixedLoss:
                    return new FixedLossQuantityAdvisor(this);
                case TradeRiskType.PercentageLoss:
                    return new PercentageLossQuantityAdvisor(this);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
