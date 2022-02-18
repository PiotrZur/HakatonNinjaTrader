using NinjaTrader.Custom.Extensions;
using NinjaTrader.Custom.Extensions.Consolidation;
using System;
using System.Linq;
using System.Windows.Media;

namespace NinjaTrader.Custom.Strategies
{
    public partial class StrategyStaticSample : StrategyBase<StrategyStaticSample, StrategyStaticSample.SampleSignalContext, StrategyStaticSample.SampleSignal>
    {
        // context is something that the strategy extracts in order to make a decision whether to play or not
        public class SampleSignalContext : IEquatable<SampleSignalContext>
        {
            public SampleSignalContext(ConsolidationRange consolidationRange)
            {
                this.ConsolidationRange = consolidationRange;
            }

            public ConsolidationRange ConsolidationRange 
            { 
                get;
                private set;
            }

            public bool Equals(SampleSignalContext other)
            {
                if (other == null)
                {
                    return false;
                }

                if (other == this)
                {
                    return true;
                }

                if (this.ConsolidationRange == null)
                {
                    return other.ConsolidationRange == null;
                }

                if (other.ConsolidationRange == null)
                {
                    return false;
                }

                return this.ConsolidationRange.Date == other.ConsolidationRange.Date;
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as SampleSignalContext);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return this.ConsolidationRange == null ? 0 : (int)this.ConsolidationRange.Date.Ticks;
                }
            }

            public override string ToString()
            {
                if (this.ConsolidationRange == null)
                {
                    return null;
                }

                return this.ConsolidationRange.End.ToString("yyyy-MM-dd");
            }
        }

        // the actual signal is simply a context wrapped into a class with additional params
        // it does not make much sense in static context (as it is generated along with the context), but makes a lot more when we get to AI (see the samples there)
        public class SampleSignal
        {
            public SampleSignal(SampleSignalContext context, Cbi.MarketPosition marketPosition)
            {
                this.Context = context;
                this.MarketPosition = marketPosition;
            }

            public SampleSignalContext Context 
            { 
                get;
                private set;
            }

            public Cbi.MarketPosition MarketPosition
            {
                get;
                private set;
            }
        }

        // the actual enter signal - this class is about to decide whether to open a position or not
        public class SampleEnterSignal : PositionEnterSignal
        {
            private ConsolidationRange currentConsolidation;

            public SampleEnterSignal(StrategyStaticSample strategy)
                : base(strategy)
            {
            }

            // PLEASE NOTE: this class is an actual state-machine
            // this particular property makes sure that the consolidation range is calculated when needed, and thrown away when no longer needed
            // PLEASE NOTE: this will be invoked for each & every new bar event if the position has not been opened yet
            private ConsolidationRange ConsolidationRange
            {
                get
                {
                    // we drop this one as soon as the new day comes, in order to avoid working with the old data
                    if (this.currentConsolidation != null && this.currentConsolidation.Date != this.Strategy.CurrentTime.Date)
                    {
                        this.currentConsolidation = null;
                    }

                    if (this.currentConsolidation == null)
                    {
                        if (!this.Strategy.consolidationBarsCollection.IsMyEvent)
                        {
                            return null;
                        }

                        // the "real" end time, respecting the configured time zone
                        var requestedConsolidationEnd = this.Strategy.CurrentTime.Date
                            .Add(this.Strategy.ConsolidationEnd.TimeOfDay)
                            .ChangeTimeZone(this.Strategy.ConsolidationEndTimeZone, CustomTimeZone.Local);

                        // if the time has not come yet, then there's nothing to do here
                        if (this.Strategy.CurrentTime < requestedConsolidationEnd)
                        {
                            return null;
                        }

                        // the "real" start, respecting the configured time zone
                        var requestedConsolidationStart = this.Strategy.CurrentTime.Date
                            .Add(this.Strategy.ConsolidationStart.TimeOfDay)
                            .ChangeTimeZone(this.Strategy.ConsolidationStartTimeZone, CustomTimeZone.Local);

                        // if the start > end, then the start is the previous day (of course)
                        if (requestedConsolidationStart >= requestedConsolidationEnd)
                        {
                            requestedConsolidationStart = this.Strategy.CurrentTime.Date
                                .AddDays(-1)
                                .Add(this.Strategy.ConsolidationStart.TimeOfDay)
                                .ChangeTimeZone(this.Strategy.ConsolidationStartTimeZone, CustomTimeZone.Local);
                        }

                        // in the end: we calculate the stuff
                        this.currentConsolidation = ConsolidationRangeCalculator.Calculate(
                            this.Strategy.consolidationBarsCollection,
                            requestedConsolidationStart,
                            requestedConsolidationEnd,
                            Double.Epsilon,
                            Double.MaxValue);

                        // if it was properly calculated then we paint it on chart
                        if (this.currentConsolidation != null)
                        {
                            this.currentConsolidation.Paint(
                                this.Strategy,
                                "Consolidation:" + this.currentConsolidation.Date,
                                Colors.Green,
                                Colors.Red);
                        }
                    }

                    if (this.currentConsolidation == null)
                    {
                        return null;
                    }

                    return this.currentConsolidation;
                }
            }

            // a little helper : an open request that says that we should not be opening a position
            private OpenRequest DoNotOpen
            {
                get { return new OpenRequest(Cbi.MarketPosition.Flat, null, null); }
            }

            // PLEASE NOTE : this is going to be invoked for each new bar event if the position is not opened yet
            public override OpenRequest CurrentOpenRequest
            {
                get
                {
                    // if consolidation is not available - we're not going to open a new position
                    if (this.ConsolidationRange == null)
                    {
                        return this.DoNotOpen;
                    }

                    // only one trade per day; if we've had one already - then we're not going to open
                    if (this.Strategy.LastPositionOpenTime.Date == this.Strategy.CurrentTime.Date)
                    {
                        return this.DoNotOpen;
                    }

                    // we do not allow trading past this time
                    var maxEnterTime = this.Strategy.CurrentTime.Date
                        .Add(this.Strategy.MaxEnterTime.TimeOfDay)
                        .ChangeTimeZone(this.Strategy.MaxEnterTimeZone, CustomTimeZone.Local);

                    if (this.Strategy.CurrentTime > maxEnterTime)
                    {
                        return this.DoNotOpen;
                    }

                    // a very simplistic position enter: we simply assume that we'll break the consolidation bound 
                    // that is more close to the current value; simply put: if we've crossed the upper bound then
                    // we go long; if we've crossed the lower bound then we go short
                    var context = new SampleSignalContext(currentConsolidation);
                    var lastBar = this.Strategy.DefaultBarsCollection.AllBars.First();
                    if (lastBar.High > this.ConsolidationRange.RangeHigh)
                    {
                        var signal = new SampleSignal(context, Cbi.MarketPosition.Long);
                        return new OpenRequest(Cbi.MarketPosition.Long, context, signal);
                    }

                    if (lastBar.Low < this.ConsolidationRange.RangeLow)
                    {
                        var signal = new SampleSignal(context, Cbi.MarketPosition.Short);
                        return new OpenRequest(Cbi.MarketPosition.Short, context, signal);
                    }

                    return this.DoNotOpen;
                }
            }
        }

        protected override PositionEnterSignal CreateEnterSignal()
        {
            return new SampleEnterSignal(this);
        }
    }
}
