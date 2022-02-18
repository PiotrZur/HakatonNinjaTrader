using NinjaTrader.Cbi;
using NinjaTrader.Custom.Extensions.Bars;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.DrawingTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace NinjaTrader.Custom.Strategies
{
    public abstract partial class StrategyBase<TSelf, TSignalContext, TSignal>
    {
        public abstract class TrailingStopLoss : PositionExitSignal
        {
            private bool isActive;
            private double? lastLevel;
            private readonly List<Bar> barsSinceOpen = new List<Bar>();
            private readonly List<ChartPoint> chartPoints = new List<ChartPoint>();
            
            private class ChartPoint
            {
                public DateTime Time { get; set; }
                public double Value { get; set; }
            }

            public TrailingStopLoss(TSelf strategy, string signalName)
                : base(strategy, signalName, AdviceType.StopLoss)
            {
            }

            protected abstract BarsCollection BarsCollection { get; }

            public override IEnumerable<CloseAdvice> CloseAdvices
            {
                get
                {
                    var nextBars = this.barsSinceOpen.Count == 0
                        ? this.BarsCollection.AllBars.TakeWhile(bar => bar.Time > this.Strategy.LastPositionOpenTime)
                        : this.BarsCollection.AllBars.TakeWhile(bar => bar.Time > barsSinceOpen[barsSinceOpen.Count - 1].Time);

                    foreach (var bar in nextBars)
                    {
                        if (!this.isActive)
                        {
                            this.isActive = this.Activate(bar);
                        }

                        if (!this.isActive)
                        {
                            continue;
                        }

                        var nextLevel = this.Strategy.Position.MarketPosition == MarketPosition.Long
                            ? bar.Low - this.StopLossDistance
                            : bar.High + this.StopLossDistance;

                        if (!this.lastLevel.HasValue)
                        {
                            this.lastLevel = nextLevel;
                        }
                        else
                        {
                            this.lastLevel = this.Strategy.Position.MarketPosition == MarketPosition.Long
                                ? Math.Max(this.lastLevel.Value, nextLevel)
                                : Math.Min(this.lastLevel.Value, nextLevel);
                        }
                    }

                    if (!this.lastLevel.HasValue)
                    {
                        yield break;
                    }

                    var chartPoint = new ChartPoint
                    {
                        Time = this.Strategy.CurrentTime,
                        Value = this.lastLevel.Value
                    };

                    if (this.chartPoints.Count == 0)
                    {
                        this.chartPoints.Add(chartPoint);
                    }
                    else
                    {
                        var last = this.chartPoints[this.chartPoints.Count - 1];
                        if (Math.Abs(last.Value - chartPoint.Value) > 1e-8)
                        {
                            this.chartPoints.Add(chartPoint);
                        }
                    }

                    yield return this.CloseAdvice(this.InitialPositionSize, this.lastLevel.Value);
                }
            }

            protected virtual bool Activate(Bar nextBar)
            {
                return true;
            }

            protected abstract double StopLossDistance { get; }

            public override void Initialize(TSignalContext signalContext, TSignal signal, int initialPositionSize)
            {
                base.Initialize(signalContext, signal, initialPositionSize);
                this.chartPoints.Clear();
                this.barsSinceOpen.Clear();
            }

            public override void Cleanup()
            {
                base.Cleanup();
                this.isActive = false;
                this.lastLevel = null;
                for (int i = 1; i < this.chartPoints.Count; i++)
                {
                    var start = this.chartPoints[i - 1];
                    var end = this.chartPoints[i];
                    Draw.Line(
                       this.Strategy,
                       this.Strategy.LastPositionOpenTime + this.SignalName + start.Time + "line",
                       false,
                       start.Time,
                       start.Value,
                       end.Time,
                       start.Value,
                       Brushes.Orange,
                       DashStyleHelper.Solid,
                       1);
                    Draw.Line(
                       this.Strategy,
                       this.Strategy.LastPositionOpenTime + this.SignalName + start.Time + "up",
                       false,
                       end.Time,
                       start.Value,
                       end.Time,
                       end.Value,
                       Brushes.Orange,
                       DashStyleHelper.Solid,
                       1);
                }

                if (this.chartPoints.Count != 0)
                {
                    var last = this.chartPoints[chartPoints.Count - 1];
                    Draw.Line(
                       this.Strategy,
                       this.Strategy.LastPositionOpenTime + this.SignalName + last.Time + "line",
                       false,
                       last.Time,
                       last.Value,
                       this.Strategy.CurrentTime,
                       last.Value,
                       Brushes.Orange,
                       DashStyleHelper.Solid,
                       1);
                }
            }
        }
    }
}
