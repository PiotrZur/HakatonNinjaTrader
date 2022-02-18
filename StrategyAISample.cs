using NinjaTrader.Custom.Extensions;
using NinjaTrader.Custom.Extensions.Bars;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript;
using System;
using System.ComponentModel.DataAnnotations;

namespace NinjaTrader.Custom.Strategies
{
    public partial class StrategyAISample : StrategyBaseAI<StrategyAISample, StrategyAISample.SampleInputVector, StrategyAISample.SampleOutputVector>
    {
        private BarsCollection consolidationBarsCollection;
        private BarsCollection suffixFormationBarsCollection;
        private BarsCollection referenceVolatilityBarsCollection;

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
                this.ConsolidationValidFor = DateTime.Today.AddHours(2);

                this.SuffixBarsPeriod = BarPeriod.Minute15;
                this.SuffixBarsCount = 10;

                this.VolatilityBarsPeriod = BarPeriod.Day;
                this.VolatilityBarsCount = 10;
            }
            else if (this.State == State.Configure)
            {
                this.consolidationBarsCollection = this.GetBarsBrowser(this.ConsolidationBarPeriod);
                this.suffixFormationBarsCollection = this.GetBarsBrowser(this.SuffixBarsPeriod);
                this.referenceVolatilityBarsCollection = this.GetBarsBrowser(this.VolatilityBarsPeriod);
            }
        }

        private const string ConsolidationGroupName = "Enter: Consolidation";

        [NinjaScriptProperty]
        [Display(Name = "Bar Period", Order = 0, GroupName = ConsolidationGroupName)]
        public BarPeriod ConsolidationBarPeriod { get; set; }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Start [time]", Order = 1, GroupName = ConsolidationGroupName)]
        public DateTime ConsolidationStart { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Start [time zone]", Order = 2, GroupName = ConsolidationGroupName)]
        public CustomTimeZone ConsolidationStartTimeZone { get; set; }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "End [time]", Order = 3, GroupName = ConsolidationGroupName)]
        public DateTime ConsolidationEnd { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "End [time zone]", Order = 4, GroupName = ConsolidationGroupName)]
        public CustomTimeZone ConsolidationEndTimeZone { get; set; }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Valid time", Order = 5, GroupName = ConsolidationGroupName)]
        public DateTime ConsolidationValidFor { get; set; }

        private const string SuffixFormationGroupName = "Enter: Suffix Formation";

        [NinjaScriptProperty]
        [Display(Name = "Bar Period", Order = 0, GroupName = SuffixFormationGroupName)]
        public BarPeriod SuffixBarsPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bar Count", Order = 1, GroupName = SuffixFormationGroupName)]
        public int SuffixBarsCount { get; set; }

        private const string ReferenceVolatilityGroupName = "Enter: Reference Volatility";

        [NinjaScriptProperty]
        [Display(Name = "Bar Period", Order = 0, GroupName = ReferenceVolatilityGroupName)]
        public BarPeriod VolatilityBarsPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bar Count", Order = 1, GroupName = ReferenceVolatilityGroupName)]
        public int VolatilityBarsCount { get; set; }
    }
}
