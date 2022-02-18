using System;
using System.Linq;

namespace NinjaTrader.Custom.Strategies
{
    public partial class StrategyAISample : StrategyBaseAI<StrategyAISample, StrategyAISample.SampleInputVector, StrategyAISample.SampleOutputVector>
    {
        public class SampleLearnModeSignal : AbstractEnterAILearnMode
        {
            public SampleLearnModeSignal(StrategyAISample strategy, InputVectorExtractor inputVectorExtractor)
                : base(strategy, inputVectorExtractor)
            {
            }

            protected override SampleOutputVector TranslateToSignal(SampleInputVector next)
            {
                // here: we have our input vector that have been captured by the extractor
                // also: the configured learn time has already passed, so we can simply grab whatever values from market we need
                // in this particular context: we need to know what was the price movement in the long / short direction since
                // the signal generation till now (again: now == time when the learn mode period has just passed)
                var rangeLong = 0.0;
                var rangeShort = 0.0;
                foreach (var testBar in this.Strategy.DefaultBarsCollection.AllBars.TakeWhile(bar => bar.Time > next.ConsolidationRange.End))
                {
                    rangeLong = Math.Max(rangeLong, testBar.High - next.ConsolidationRange.Last);
                    rangeShort = Math.Max(rangeShort, next.ConsolidationRange.Last - testBar.Low);
                }

                return new SampleOutputVector(next, rangeLong, rangeShort);
            }
        }

        protected override AbstractEnterAILearnMode CreateLearnModeEnterSignal()
        {
            return new SampleLearnModeSignal(this, new SampleInputVectorExtractor(this));
        }
    }
}
