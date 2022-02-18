using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.DrawingTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace NinjaTrader.Custom.Strategies
{
    public partial class StrategyStaticSample : StrategyBase<StrategyStaticSample, StrategyStaticSample.SampleSignalContext, StrategyStaticSample.SampleSignal>
    {
        // sample exit: places a take profit order with the distance equal to some percentage of consolidation, and can close
        // some percentage of a position; there are 3 different profits to be configured, so we can have partial closes on different levels

        public abstract class SampleAbstractTakeProfit : PositionExitSignal
        {
            private double profitPrice;
            private int profitSize;
            private DateTime positionOpen;

            public SampleAbstractTakeProfit(StrategyStaticSample strategy, string signalName)
                : base(strategy, signalName, AdviceType.TakeProfit)
            {
            }

            public override IEnumerable<CloseAdvice> CloseAdvices
            {
                get
                {
                    if (!double.IsNaN(this.profitPrice))
                    {
                        yield return this.CloseAdvice(this.profitSize, this.profitPrice);
                    }
                }
            }

            public abstract int PositionPercentage { get; }

            public abstract int ConsolidationSizePercentage { get; }

            // in this particular context - the profit order is static, so it is fine to set everything up on the 
            // initialize event; this is handy also because we can use some of the information to later draw the 
            // profit order level on chart (makes the analisys a lot easier when looking into what the strategy actually did)
            public override void Initialize(SampleSignalContext signalContext, SampleSignal signal, int initialPositionSize)
            {
                base.Initialize(signalContext, signal, initialPositionSize);
                this.positionOpen = this.Strategy.LastPositionOpenTime;
                var positionPercentage = Math.Max(0, Math.Min(100, this.PositionPercentage));
                if (positionPercentage == 0)
                {
                    this.profitPrice = double.NaN;
                    return;
                }

                this.profitSize = (int)((positionPercentage / 100.0) * this.InitialPositionSize);
                if (this.profitSize == 0)
                {
                    this.profitPrice = double.NaN;
                    return;
                }

                var consolidationPercentage = Math.Max(0, this.ConsolidationSizePercentage);
                if (consolidationPercentage == 0)
                {
                    profitPrice = double.NaN;
                    return;
                }

                var distance = this.SignalContext.ConsolidationRange.SizeHighLow * (consolidationPercentage / 100.0);
                switch (this.Signal.MarketPosition)
                {
                    case Cbi.MarketPosition.Long:
                        this.profitPrice = this.Strategy.Position.AveragePrice + distance;
                        return;
                    case Cbi.MarketPosition.Short:
                        this.profitPrice = this.Strategy.Position.AveragePrice - distance;
                        return;
                    default:
                        this.profitPrice = double.NaN;
                        return;
                }
            }

            public override void Cleanup()
            {
                base.Cleanup();
                Draw.Line(
                   this.Strategy,
                   this.positionOpen + this.SignalName,
                   false,
                   this.positionOpen,
                   this.profitPrice,
                   this.Strategy.CurrentTime,
                   this.profitPrice,
                   Brushes.Green,
                   DashStyleHelper.Solid,
                   1);
            }
        }

        public class SampleTakeProfit1: SampleAbstractTakeProfit
        {
            public SampleTakeProfit1(StrategyStaticSample strategy, string signalName)
                : base(strategy, signalName)
            {
            }

            public override int PositionPercentage
            {
                get { return this.Strategy.TakeProfit1PositionPercentage; }
            }

            public override int ConsolidationSizePercentage
            {
                get { return this.Strategy.TakeProfit1Distance; }
            }
        }

        public class SampleTakeProfit2 : SampleAbstractTakeProfit
        {
            public SampleTakeProfit2(StrategyStaticSample strategy, string signalName)
                : base(strategy, signalName)
            {
            }

            public override int PositionPercentage
            {
                get { return this.Strategy.TakeProfit2PositionPercentage; }
            }

            public override int ConsolidationSizePercentage
            {
                get { return this.Strategy.TakeProfit2Distance; }
            }
        }

        public class SampleTakeProfit3 : SampleAbstractTakeProfit
        {
            public SampleTakeProfit3(StrategyStaticSample strategy, string signalName)
                : base(strategy, signalName)
            {
            }

            public override int PositionPercentage
            {
                get { return this.Strategy.TakeProfit3PositionPercentage; }
            }

            public override int ConsolidationSizePercentage
            {
                get { return this.Strategy.TakeProfit3Distance; }
            }
        }
    }
}
