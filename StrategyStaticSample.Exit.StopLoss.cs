using NinjaTrader.Custom.Extensions.Consolidation;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.DrawingTools;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace NinjaTrader.Custom.Strategies
{
    // NOTE: this is the most basic stop loss order; it will be on the fixed level, so all calculations are made on the Initialize method
    public partial class StrategyStaticSample : StrategyBase<StrategyStaticSample, StrategyStaticSample.SampleSignalContext, StrategyStaticSample.SampleSignal>
    {
        // sample exit: places a stop loss order on the consolidation bound (upper if playing short, lower in playing long) + some fixed offset
        public class SampleStopLoss: PositionExitSignal
        {
            private double stopLossPrice;
            private DateTime positionOpen;

            public SampleStopLoss(StrategyStaticSample strategy, string signalName)
                : base(strategy, signalName, AdviceType.StopLoss)
            {
            }

            public override IEnumerable<CloseAdvice> CloseAdvices
            {
                get
                {
                    if (!double.IsNaN(this.stopLossPrice))
                    {
                        yield return this.CloseAdvice(this.InitialPositionSize, this.stopLossPrice);
                    }
                }
            }

            public double GetStopLossLevel(Cbi.MarketPosition marketPosition, ConsolidationRange consolidationRange)
            {
                switch (marketPosition)
                {
                    case Cbi.MarketPosition.Long:
                        return consolidationRange.RangeLow - this.FixedOffset;
                    case Cbi.MarketPosition.Short:
                        return consolidationRange.RangeHigh + this.FixedOffset;
                    default:
                        return double.NaN;
                }
            }

            public double FixedOffset
            {
                get { return this.Strategy.StopLossFixedOffset; }
            }

            public override void Initialize(SampleSignalContext signalContext, SampleSignal signal, int initialPositionSize)
            {
                base.Initialize(signalContext, signal, initialPositionSize);
                this.positionOpen = this.Strategy.LastPositionOpenTime;
                this.stopLossPrice = this.GetStopLossLevel(signal.MarketPosition, signal.Context.ConsolidationRange);
            }

            public override void Cleanup()
            {
                base.Cleanup();
                Draw.Line(
                   this.Strategy,
                   this.positionOpen + this.SignalName,
                   false,
                   this.positionOpen,
                   this.stopLossPrice,
                   this.Strategy.CurrentTime,
                   this.stopLossPrice,
                   Brushes.Red,
                   DashStyleHelper.Solid,
                   1);
            }
        }
    }
}
