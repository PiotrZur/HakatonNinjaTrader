using NinjaTrader.Custom.Extensions;
using NinjaTrader.Custom.Extensions.Bars;
using NinjaTrader.Custom.Extensions.Consolidation;
using System;
using System.Linq;
using System.Windows.Media;

namespace NinjaTrader.Custom.Strategies
{
    public partial class StrategyAISample : StrategyBaseAI<StrategyAISample, StrategyAISample.SampleInputVector, StrategyAISample.SampleOutputVector>
    {
        public class SampleInputVectorExtractor : InputVectorExtractor
        {
            private readonly StrategyAISample strategy;

            private ConsolidationRange currentConsolidation;
            private DateTime currentConsolidationEndOfLife;
			private double sma;

            public SampleInputVectorExtractor(StrategyAISample strategy)
            {
                this.strategy = strategy;
            }

            public override SampleInputVector CurrentInputVector
            {
                get
                {
                    // we drop this one as soon as the new day comes, in order to avoid working with the old data
                    if (this.currentConsolidation != null && this.currentConsolidation.Date != this.strategy.CurrentTime.Date)
                    {
                        this.currentConsolidation = null;
                    }

                    // it might happen that the life will end for our consolidation (as it will be valid for a limited period of time)
                    if (this.currentConsolidation != null && this.strategy.CurrentTime > this.currentConsolidationEndOfLife)
                    {
                        this.currentConsolidation = null;
                    }

                    if (this.currentConsolidation == null)
                    {
                        // building new consolidation happens only on its respective bar event
                        if (!this.strategy.consolidationBarsCollection.IsMyEvent)
                        {
                            return this.strategy.EmptySignalContext;
                        }

                        // we already had one today, that has reached its end of life - nothing to do
                        if (this.strategy.CurrentTime.Date == this.currentConsolidationEndOfLife.Date)
                        {
                            return this.strategy.EmptySignalContext;
                        }

                        // the "real" end time, respecting the configured time zone
                        var requestedConsolidationEnd = this.strategy.CurrentTime.Date
                            .Add(this.strategy.ConsolidationEnd.TimeOfDay)
                            .ChangeTimeZone(this.strategy.ConsolidationEndTimeZone, CustomTimeZone.Local);

                        // if the time has not come yet, then there's nothing to do here
                        if (this.strategy.CurrentTime < requestedConsolidationEnd)
                        {
                            return this.strategy.EmptySignalContext;
                        }

                        // the "real" start, respecting the configured time zone
                        var requestedConsolidationStart = this.strategy.CurrentTime.Date
                            .Add(this.strategy.ConsolidationStart.TimeOfDay)
                            .ChangeTimeZone(this.strategy.ConsolidationStartTimeZone, CustomTimeZone.Local);

                        // if the start > end, then the start is the previous day (of course)
                        if (requestedConsolidationStart >= requestedConsolidationEnd)
                        {
                            requestedConsolidationStart = this.strategy.CurrentTime.Date
                                .AddDays(-1)
                                .Add(this.strategy.ConsolidationStart.TimeOfDay)
                                .ChangeTimeZone(this.strategy.ConsolidationStartTimeZone, CustomTimeZone.Local);
                        }

                        // in the end: we calculate the stuff
                        this.currentConsolidation = ConsolidationRangeCalculator.Calculate(
                            this.strategy.consolidationBarsCollection,
                            requestedConsolidationStart,
                            requestedConsolidationEnd,
                            Double.Epsilon,
                            Double.MaxValue);

                        // if it was properly calculated, then we also gather the context and paint it on chart
                        // NOTE: it's sort-of important to do this ONCE: since gathering the context might be kinda expensive
                        if (this.currentConsolidation != null)
                        {
                            // if the consolidation was set - then we can already configure its end of life
                            // in order to stop generating more signals after this time has passed;
                            // in other words - we can open a position for a limited amount of time since we've found the consolidation)
                            this.currentConsolidationEndOfLife = this.currentConsolidation.End.Add(this.strategy.ConsolidationValidFor.TimeOfDay);
                            var drawTag = "Consolidation:" + this.currentConsolidation.Date;
                            this.currentConsolidation.Paint(
                                this.strategy,
                                drawTag,
                                Colors.Green,
                                Colors.Red);
                        }
                    }

                    // if this is still null, it means that either it's too early or input data has gaps
                    if (this.currentConsolidation == null)
                    {
                        return this.strategy.EmptySignalContext;
                    }

                    var suffixBars = this.SuffixBarsCollection
                        .AllBars
                        .Take(this.SuffixBarsCount)
                        .Reverse()
                        .ToList();

                    // this generally should not happen, unless we're configure some bars
                    // that are not supported or of a bigger period that is not yet available
                    if (suffixBars.Count != this.SuffixBarsCount)
                    {
                        return this.strategy.EmptySignalContext;
                    }

                    var volatility = this.ReferenceVolatility;
                    // it might be that the volatility can't be calculated - as it will typically require several bars
                    // of a higher period (like day), so if we start at day 0 then it will be undefined at start
                    if (Double.IsNaN(volatility))
                    {
                        return this.strategy.EmptySignalContext;
                    }

                    // in our case: the last bar in the suffix formation is the key driver of making our intent to open a new position
                    var intendedOpenTime = suffixBars.Last().Time;



					double sma = this.strategy.SMA(this.strategy.SMAPeriod)[0];
                    double MACD1 = this.strategy.MACD(12, 26, 9)[0];
                    double MACD2 = this.strategy.MACD(14, 24, 9)[0];
                    double MACD3 = this.strategy.MACD(6, 13, 9)[0];
                    double MACD4 = this.strategy.MACD(12, 26, 16)[0];

                    double stichasticK1 = this.strategy.Stochastics(10, 14, 6).K[0];
                    double stichasticD1 = this.strategy.Stochastics(10, 14, 6).D[0];
                    double stichasticK2 = this.strategy.Stochastics(10, 14, 12).K[0];
                    double stichasticD2 = this.strategy.Stochastics(10, 14, 12).D[0];
                    double stichasticK3 = this.strategy.Stochastics(5, 7, 6).K[0];
                    double stichasticD3 = this.strategy.Stochastics(5, 7, 6).D[0];
                    return new SampleInputVector(
                        intendedOpenTime,
                        this.currentConsolidation,
                        suffixBars,
                        volatility,
						sma);
                }
            }

            private BarsCollection SuffixBarsCollection
            {
                get { return this.strategy.suffixFormationBarsCollection; }
            }

            private int SuffixBarsCount
            {
                get { return this.strategy.SuffixBarsCount; }
            }

            private double ReferenceVolatility
            {
                get
                {
                    if (this.strategy.referenceVolatilityBarsCollection.Count < this.strategy.VolatilityBarsCount + 1)
                    {
                        return Double.NaN;
                    }

                    var atr = this.strategy.ATR(
                        this.strategy.referenceVolatilityBarsCollection.NinjaBars,
                        this.strategy.VolatilityBarsCount);
                    return atr[0];
                }
            }
        }
    }
}
