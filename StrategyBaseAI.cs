using NinjaTrader.Gui;
using NinjaTrader.NinjaScript;
using System;
using System.ComponentModel.DataAnnotations;

namespace NinjaTrader.Custom.Strategies
{
    public abstract partial class StrategyBaseAI<TSelf, TSignalContext, TSignal> : StrategyBase<TSelf, TSignalContext, TSignal>
        where TSelf : StrategyBaseAI<TSelf, TSignalContext, TSignal>
        where TSignalContext : StrategyBaseAI<TSelf, TSignalContext, TSignal>.InputVector, IEquatable<TSignalContext>
        where TSignal : StrategyBaseAI<TSelf, TSignalContext, TSignal>.OutputVector, IEquatable<TSignal>
    {
        protected override void OnStateChange()
        {
            base.OnStateChange();

            switch (this.State)
            {
                case State.SetDefaults:
                    this.LearnModeEnabled = true;
                    this.LearnModeDataPeriod = DateTime.Today.AddHours(3);
                    this.PlayerModeRegression = true;
                    this.PlayerModeRegressionFilePath = "c:/temp/ai_output.csv";
                    break;
                case State.DataLoaded:
                    {
                        var learnModeSignal = this.EnterSignal as AbstractEnterAILearnMode;
                        if (learnModeSignal != null)
                        {
                            learnModeSignal.InitBackgroundLogger();
                        }
                    }
                    
                    break;
                case State.Terminated:
                    {
                        var learnModeSignal = this.EnterSignal as AbstractEnterAILearnMode;
                        if (learnModeSignal != null)
                        {
                            learnModeSignal.FlushWriters();
                        }
                    }

                    break;
            }
        }

        protected const string LearnModeGroupName = "Learn Mode Settings";

        [NinjaScriptProperty]
        [Display(Name = "Learn Mode Enabled", Order = 1, GroupName = LearnModeGroupName)]
        public bool LearnModeEnabled { get; set; }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Predition Period", Order = 2, GroupName = LearnModeGroupName)]
        public DateTime LearnModeDataPeriod { get; set; }

        protected const string PlayerModeSettings = "Player Mode Settings";

        [NinjaScriptProperty]
        [Display(Name = "Regression Mode [use csv input]", Order = 1, GroupName = PlayerModeSettings)]
        public bool PlayerModeRegression { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Regression File Path", Order = 1, GroupName = PlayerModeSettings)]
        public string PlayerModeRegressionFilePath { get; set; }
    }
}
