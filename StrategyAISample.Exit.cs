using NinjaTrader.Custom.Extensions.Consolidation;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.DrawingTools;
using System;
using System.Collections.Generic;
using System.Windows.Media;

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

        public class StopLossSignal : PositionExitSignal
        {
            public StopLossSignal(StrategyAISample strategy, string signalName)
                : base(strategy, signalName, AdviceType.StopLoss)
            {
            }

            public override IEnumerable<CloseAdvice> CloseAdvices
            {
                get
                {
                    double currentPrice = this.Strategy.Close[0];
                    double longRange = this.Signal.LongRange;
                    double shortRange = this.Signal.ShortRange;

                    double stopLossPrice;
                    if (longRange > shortRange)
                    {
                       stopLossPrice = currentPrice - Math.Abs(longRange - shortRange) * this.Strategy.StopLossModifier;
                    } else
                    {
                        stopLossPrice = currentPrice + Math.Abs(longRange - shortRange) * this.Strategy.StopLossModifier;
                    }
                   
                        yield return this.CloseAdvice(this.InitialPositionSize, stopLossPrice);

                }
            }
        }

        public class TakeProfitSignal : PositionExitSignal
        {
            public TakeProfitSignal(StrategyAISample strategy, string signalName)
                : base(strategy, signalName, AdviceType.StopLoss)
            {
            }

            public override IEnumerable<CloseAdvice> CloseAdvices
            {
                get
                {
                    double currentPrice = this.Strategy.Close[0];
                    double longRange = this.Signal.LongRange;
                    double shortRange = this.Signal.ShortRange;

                    double takeProfitPrice;
                    if (longRange > shortRange)
                    {
                        takeProfitPrice = currentPrice +  longRange * this.Strategy.TakeProfitModifier;
                    }
                    else
                    {
                        takeProfitPrice = currentPrice - shortRange  * this.Strategy.TakeProfitModifier;
                    }

                    yield return this.CloseAdvice(this.InitialPositionSize, takeProfitPrice);

                }
            }
        }


        protected override IEnumerable<PositionExitSignal> CreateExitSignals()
        {
            // only one exit, after the fixed amount of time
            yield return new TimeExitSignal(this, "Time Exit");
            yield return new StopLossSignal(this, "Stop loss");
            //yield return new TakeProfitSignal(this, "TakeProfit");
        }
    }
}
