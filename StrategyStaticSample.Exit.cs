using System.Collections.Generic;

namespace NinjaTrader.Custom.Strategies
{
    // in this particular class : sample is about demonstrating that we can dynamically add various close strategies
    // for instance: we might implement a trailing stop loss, but in order to see whether it really makes a difference it would be worth to compare
    // the outcome with that one enabled and disabled (quite likely: run it enabled few times with different settings)
    // the whole idea is about making it possible to parallelize the testing / optimization and implementation of features
    public partial class StrategyStaticSample : StrategyBase<StrategyStaticSample, StrategyStaticSample.SampleSignalContext, StrategyStaticSample.SampleSignal>
    {
        protected override IEnumerable<PositionExitSignal> CreateExitSignals()
        {
            // stop loss is always enabled
            yield return new SampleStopLoss(this, "Stop Loss");

            if (this.TimeStopEnabled)
            {
                yield return new SampleTimeExit(this, "Time Exit");
            }
            
            if (this.TakeProfit1Enabled)
            {
                yield return new SampleTakeProfit1(this, "Take Profit 1");
            }

            if (this.TakeProfit2Enabled)
            {
                yield return new SampleTakeProfit2(this, "Take Profit 2");
            }

            if (this.TakeProfit3Enabled)
            {
                yield return new SampleTakeProfit3(this, "Take Profit 3");
            }

            if (this.TrailingStopLossEnabled)
            {
                yield return new SampleTrailingStopLoss(this, "Trailing Stop Loss");
            }

            if (this.BreakEvenActive)
            {
                yield return new SampleBreakEven(this, "Break Even");
            }
        }
    }
}
