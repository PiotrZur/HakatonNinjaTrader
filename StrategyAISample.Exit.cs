using System.Collections.Generic;

namespace NinjaTrader.Custom.Strategies
{
    // a very simplistic exit signal; it will exit the full position after the learn-mode period has passed
    // in context of this particular strategy it sort-of make sense, since the enter signal simply a hint about
    // the price movement in the mentioned period - so, whatever happens after that period, is a mystery
    // SPECIAL NOTE: this can be more fancy - to see some samples please refer to static strategy sample
    public partial class StrategyAISample : StrategyBaseAI<StrategyAISample, StrategyAISample.SampleInputVector, StrategyAISample.SampleOutputVector>
    {
        public class TimeExitSignal : PositionExitSignal
        {
            public TimeExitSignal(StrategyAISample strategy, string signalName)
                : base(strategy, signalName, AdviceType.StopLoss)
            {
            }

            public override IEnumerable<CloseAdvice> CloseAdvices
            {
                get
                {
                    var exitTime = this.Strategy.LastPositionOpenTime.Add(this.Strategy.LearnModeDataPeriod.TimeOfDay);
                    if (this.Strategy.CurrentTime >= exitTime)
                    {
                        yield return this.CloseAdvice(this.InitialPositionSize);
                    }
                }
            }
        }

        protected override IEnumerable<PositionExitSignal> CreateExitSignals()
        {
            // only one exit, after the fixed amount of time
            yield return new TimeExitSignal(this, "Time Exit");
        }
    }
}
