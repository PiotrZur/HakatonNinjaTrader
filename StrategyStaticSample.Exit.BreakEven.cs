using NinjaTrader.Custom.Extensions;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.DrawingTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace NinjaTrader.Custom.Strategies
{
    // concept of break-even is to place a stop-loss order on a level where, if hit, it will generate no loss (or little loss / little profit)
    // quite often used if we want to:
    //   - make sure that, after certain profit was already made, we do not turn it into a loss when some unpredictable MKT movement happens
    //   - save yourself from loss when it was detected that there is a risk of high volatility period
    //   - save yourself from loss when it was detected that the base scenario you assumed to happen is expired (like: we assumed up-move in next 3 hrs, 3 hrs passed, we had an up-move, now the unknown awaits)
    //   - the above are just samples, there are many more reasons why you might want to say: 'OK, now whatever happens I want to be at least at 0 balance'
    // this particular implementation:
    //   - will activate break-even at certain hour
    //   - will activate break-even only if we were on the profit side
    public partial class StrategyStaticSample : StrategyBase<StrategyStaticSample, StrategyStaticSample.SampleSignalContext, StrategyStaticSample.SampleSignal>
    {
        public class SampleBreakEven: PositionExitSignal
        {
            private bool breakEvenActive = false;
            private double breakEvenLevel = double.NaN;
            private DateTime breakEvenActivationTime;

            public SampleBreakEven(StrategyStaticSample strategy, string signalName)
                : base(strategy, signalName, AdviceType.StopLoss)
            {
            }

            public override IEnumerable<CloseAdvice> CloseAdvices
            {
                get
                {
                    if (this.breakEvenActive)
                    {
                        yield return this.CloseAdvice(this.InitialPositionSize, this.breakEvenLevel);
                        yield break;
                    }

                    var activationTime = this.Strategy.CurrentTime.Date
                        .Add(this.ActivationTime)
                        .ChangeTimeZone(this.ActivationTimeZone, CustomTimeZone.Local);

                    if (this.Strategy.CurrentTime < activationTime)
                    {
                        yield break;
                    }

                    var currentProfit = this.Strategy.Position.MarketPosition == Cbi.MarketPosition.Long
                        ? (this.Strategy.DefaultBarsCollection.AllBars.First().Close - this.Strategy.Position.AveragePrice)
                        : (this.Strategy.Position.AveragePrice - this.Strategy.DefaultBarsCollection.AllBars.First().Close);

                    if (currentProfit >= this.ActivationProfit)
                    {
                        this.breakEvenActive = true;
                        this.breakEvenActivationTime = this.Strategy.CurrentTime;
                        var offset = this.Strategy.Position.MarketPosition == Cbi.MarketPosition.Long
                            ? this.BreakEvenOffset
                            : -this.BreakEvenOffset;
                        this.breakEvenLevel = this.Strategy.Position.AveragePrice + offset;
                        yield return this.CloseAdvice(this.InitialPositionSize, this.Strategy.Position.AveragePrice);
                        yield break;
                    }
                }
            }

            public TimeSpan ActivationTime
            {
                get { return this.Strategy.BreakEvenActivationTime.TimeOfDay; }
            }

            public CustomTimeZone ActivationTimeZone
            {
                get { return this.Strategy.BreakEvenActivationTimeZone; }
            }

            public double ActivationProfit
            {
                get { return this.Strategy.BreakEvenActivationProfit; }
            }

            public double BreakEvenOffset
            {
                get { return 0.0001; }
            }

            public override void Initialize(SampleSignalContext signalContext, SampleSignal signal, int initialPositionSize)
            {
                base.Initialize(signalContext, signal, initialPositionSize);
                this.breakEvenActive = false;
                this.breakEvenLevel = double.NaN;
                this.breakEvenActivationTime = DateTime.MinValue;
            }

            public override void Cleanup()
            {
                base.Cleanup();
                if (!double.IsNaN(this.breakEvenLevel))
                {
                    Draw.Line(
                       this.Strategy,
                       this.Strategy.LastPositionOpenTime + this.SignalName,
                       false,
                       this.breakEvenActivationTime,
                       this.breakEvenLevel,
                       this.Strategy.CurrentTime,
                       this.breakEvenLevel,
                       Brushes.LightBlue,
                       DashStyleHelper.Solid,
                       1);
                }
            }
        }
    }
}
