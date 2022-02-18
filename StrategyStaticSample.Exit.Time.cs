using System;
using System.Collections.Generic;

namespace NinjaTrader.Custom.Strategies
{
    // sometimes it is a good idea to close the position based on the actual time; such examples:
    //   - we have made a prediction for the price movement in next 4 hrs; after they pass - we the unknown starts;
    //   - there are cetain times that does not fit the actual strategy scenario: for instance, playing on low-vol period is a classic day-trading;
    //     after the day is done  (for instance: low-vol is between NY close and LON open: so when NY closes then day is done) we might consider this
    //     to be an appropriate time to close the position (now why: we usually have some other ways to close it: via SL, via TP, etc. none of them worked,
    //     and the new day will give us some food for thought being a context for the next open - a new scenario, new opening)
    public partial class StrategyStaticSample : StrategyBase<StrategyStaticSample, StrategyStaticSample.SampleSignalContext, StrategyStaticSample.SampleSignal>
    {
        // sample exit: that will close 100% of the position after configured time has passed
        public class SampleTimeExit : PositionExitSignal
        {
            public SampleTimeExit(StrategyStaticSample strategy, string signalName)
                : base(strategy, signalName, AdviceType.StopLoss)
            {
            }

            public override IEnumerable<CloseAdvice> CloseAdvices
            {
                get
                {
                    if (this.Strategy.CurrentTime >= this.Strategy.LastPositionOpenTime.Add(this.MaxPositionTime))
                    {
                        yield return this.CloseAdvice(this.InitialPositionSize);
                    }
                }
            }

            private TimeSpan MaxPositionTime
            {
                get { return this.Strategy.TimeStopMaxPositionTime.TimeOfDay; }
            }
        }
    }
}
