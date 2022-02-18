using NinjaTrader.Custom.Extensions;
using NinjaTrader.Custom.Extensions.Bars;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript;
using System;
using System.ComponentModel.DataAnnotations;

namespace NinjaTrader.Custom.Strategies
{
    public partial class StrategyStaticSample : StrategyBase<StrategyStaticSample, StrategyStaticSample.SampleSignalContext, StrategyStaticSample.SampleSignal>
    {
        public enum TradeRiskType
        {
            FixedSize,
            FixedLoss,
            PercentageLoss
        }

        private BarsCollection consolidationBarsCollection;

        protected override void OnStateChange()
        {
            base.OnStateChange();

            if (this.State == State.SetDefaults)
            {
                this.ConsolidationBarPeriod = BarPeriod.Minute30;
                this.ConsolidationStart = DateTime.Today.AddHours(17);
                this.ConsolidationStartTimeZone = CustomTimeZone.NewYork;
                this.ConsolidationEnd = DateTime.Today.AddHours(7);
                this.ConsolidationEndTimeZone = CustomTimeZone.London;
                this.MaxEnterTime = this.ConsolidationEnd.AddHours(4);
                this.MaxEnterTimeZone = this.ConsolidationEndTimeZone;

                this.QuantityRiskType = TradeRiskType.FixedLoss;
                this.QuantityFixedSize = 100000;
                this.QuantityFixedLoss = 500;
                this.QuantityPercentageLoss = 5;

                this.TimeStopEnabled = true;
                this.TimeStopMaxPositionTime = DateTime.Today.Add(TimeSpan.FromHours(8));

                this.StopLossFixedOffset = 0.0002;

                this.TakeProfit1Enabled = true;
                this.TakeProfit1PositionPercentage = 33;
                this.TakeProfit1Distance = 80;

                this.TakeProfit2Enabled = true;
                this.TakeProfit2PositionPercentage = 66;
                this.TakeProfit2Distance = 140;

                this.TakeProfit3Enabled = true;
                this.TakeProfit3PositionPercentage = 100;
                this.TakeProfit3Distance = 180;

                this.TrailingStopLossEnabled = true;
                this.TrailingStopLossDistance = 0.0020;
                this.TrailingStopLossActivationPercentage = 80;

                this.BreakEvenActive = true;
                this.BreakEvenActivationTime = DateTime.Today.Add(TimeSpan.FromHours(12));
                this.BreakEvenActivationTimeZone = CustomTimeZone.London;
                this.BreakEvenActivationProfit = 0.001;
            }
            else if (this.State == State.Configure)
            {
                this.consolidationBarsCollection = this.GetBarsBrowser(this.ConsolidationBarPeriod);
            }
        }

        private static class ConfigurationGroup
        {
            public const string PositionEnter = "Position Enter";

            public const string RiskType = "Trade Risk";

            public const string TimeStop = "Time Stop";

            public const string StopLoss = "Stop Loss";

            public const string TakeProfit = "Take Profit";

            public const string TrailingStopLoss = "Trailing Stop Loss";

            public const string BreakEven = "Break Even";
        }

        [NinjaScriptProperty]
        [Display(Name = "Bar Period", Order = 0, GroupName = ConfigurationGroup.PositionEnter)]
        public BarPeriod ConsolidationBarPeriod
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Start [time]", Order = 1, GroupName = ConfigurationGroup.PositionEnter)]
        public DateTime ConsolidationStart
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "Start [time zone]", Order = 2, GroupName = ConfigurationGroup.PositionEnter)]
        public CustomTimeZone ConsolidationStartTimeZone
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "End [time]", Order = 3, GroupName = ConfigurationGroup.PositionEnter)]
        public DateTime ConsolidationEnd
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "End [time zone]", Order = 4, GroupName = ConfigurationGroup.PositionEnter)]
        public CustomTimeZone ConsolidationEndTimeZone 
        { 
            get;
            set;
        }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Max Enter Time", Order = 5, GroupName = ConfigurationGroup.PositionEnter)]
        public DateTime MaxEnterTime
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "End [time zone]", Order = 6, GroupName = ConfigurationGroup.PositionEnter)]
        public CustomTimeZone MaxEnterTimeZone
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "Enabled", Order = 1, GroupName = ConfigurationGroup.TimeStop)]
        public bool TimeStopEnabled
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Max Position Time", Order = 2, GroupName = ConfigurationGroup.TimeStop)]
        public DateTime TimeStopMaxPositionTime 
        { 
            get; 
            set;
        }


        [NinjaScriptProperty]
        [Display(Name = "Fixed Offset", Order = 1, GroupName = ConfigurationGroup.StopLoss)]
        public double StopLossFixedOffset
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "TP1: Enabled", Order = 1, GroupName = ConfigurationGroup.TakeProfit)]
        public bool TakeProfit1Enabled
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "TP1: Position Size [%]", Order = 2, GroupName = ConfigurationGroup.TakeProfit)]
        public int TakeProfit1PositionPercentage
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "TP1: Distance [% of consolidation]", Order = 3, GroupName = ConfigurationGroup.TakeProfit)]
        public int TakeProfit1Distance
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "TP2: Enabled", Order = 4, GroupName = ConfigurationGroup.TakeProfit)]
        public bool TakeProfit2Enabled
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "TP2: Position Size [%]", Order = 5, GroupName = ConfigurationGroup.TakeProfit)]
        public int TakeProfit2PositionPercentage
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "TP2: Distance [% of consolidation]", Order = 6, GroupName = ConfigurationGroup.TakeProfit)]
        public int TakeProfit2Distance
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "TP3: Enabled", Order = 7, GroupName = ConfigurationGroup.TakeProfit)]
        public bool TakeProfit3Enabled
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "TP3: Position Size [%]", Order = 8, GroupName = ConfigurationGroup.TakeProfit)]
        public int TakeProfit3PositionPercentage
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "TP3: Distance [% of consolidation]", Order = 9, GroupName = ConfigurationGroup.TakeProfit)]
        public int TakeProfit3Distance
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "Enabled", Order = 1, GroupName = ConfigurationGroup.TrailingStopLoss)]
        public bool TrailingStopLossEnabled
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "Distance", Order = 2, GroupName = ConfigurationGroup.TrailingStopLoss)]
        public double TrailingStopLossDistance
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "Activation [% of consolidation]", Order = 3, GroupName = ConfigurationGroup.TrailingStopLoss)]
        public double TrailingStopLossActivationPercentage
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "Type", Order = 1, GroupName = ConfigurationGroup.RiskType)]
        public TradeRiskType QuantityRiskType
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "Fixed Quantity", Order = 2, GroupName = ConfigurationGroup.RiskType)]
        public int QuantityFixedSize
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "Fixed Loss [$]", Order = 3, GroupName = ConfigurationGroup.RiskType)]
        public int QuantityFixedLoss
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "Percentage Loss [%]", Order = 4, GroupName = ConfigurationGroup.RiskType)]
        public int QuantityPercentageLoss
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "Enabled", Order = 1, GroupName = ConfigurationGroup.BreakEven)]
        public bool BreakEvenActive
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Activation Time", Order = 2, GroupName = ConfigurationGroup.BreakEven)]
        public DateTime BreakEvenActivationTime
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "Activation Time Zone", Order = 3, GroupName = ConfigurationGroup.BreakEven)]
        public CustomTimeZone BreakEvenActivationTimeZone
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(Name = "Activation Profit", Order = 4, GroupName = ConfigurationGroup.BreakEven)]
        public double BreakEvenActivationProfit
        {
            get;
            set;
        }
    }
}
