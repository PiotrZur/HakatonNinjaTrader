using System;
using System.Globalization;

namespace NinjaTrader.Custom.Strategies
{
    public partial class StrategyAISample : StrategyBaseAI<StrategyAISample, StrategyAISample.SampleInputVector, StrategyAISample.SampleOutputVector>
    {
        public class SamplePlayerSignal : AbstractEnterAIPlayer
        {
            public SamplePlayerSignal(StrategyAISample strategy, InputVectorExtractor inputVectorExtractor)
                : base(strategy, inputVectorExtractor)
            {
            }

            protected override DateTime CsvLineToDateTime(string[] line)
            {
                return DateTime.Parse(line[0], CultureInfo.InvariantCulture);
            }

            protected override SampleOutputVector CsvLineToOutput(SampleInputVector inputVector, string[] line)
            {
                return new SampleOutputVector(
                    inputVector,
                    Double.Parse(line[1], CultureInfo.InvariantCulture),
                    Double.Parse(line[2], CultureInfo.InvariantCulture));
            }

            protected override OpenRequest TranslateToOpenRequest(SampleOutputVector signal)
            {
                switch (Math.Sign(signal.LongRange - signal.ShortRange))
                {
                    case 1:
                        return new OpenRequest(Cbi.MarketPosition.Long, signal.SignalContext, signal);
                    case -1:
                        return new OpenRequest(Cbi.MarketPosition.Short, signal.SignalContext, signal);
                    default:
                        return new OpenRequest(Cbi.MarketPosition.Flat, signal.SignalContext, signal);
                }
            }
        }

        protected override AbstractEnterAIPlayer CreatePlayerEnterSignal()
        {
            return new SamplePlayerSignal(this, new SampleInputVectorExtractor(this));
        }
    }
}
