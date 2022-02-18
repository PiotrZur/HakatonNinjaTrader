using NinjaTrader.Custom.Extensions.Bars;
using NinjaTrader.Custom.Extensions.Consolidation;
using System;
using System.Collections.Generic;

namespace NinjaTrader.Custom.Strategies
{
    public partial class StrategyAISample : StrategyBaseAI<StrategyAISample, StrategyAISample.SampleInputVector, StrategyAISample.SampleOutputVector>
    {
        protected override SampleInputVector EmptySignalContext
        {
            get { return SampleInputVector.EMPTY; }
        }

        public class SampleInputVector : InputVector, IEquatable<SampleInputVector>
        {
            public static readonly SampleInputVector EMPTY = new SampleInputVector();

            private readonly ConsolidationRange consolidation;
            private readonly double referenceVolatility;
            private readonly string[] normalizedHeader;
            private readonly double[] normalizedInput;
            private readonly string[] rawHeader;
            private readonly object[] rawInput;

            private SampleInputVector()
                : base(DateTime.MinValue)
            {
                this.consolidation = null;
                this.referenceVolatility = double.NaN;
                this.normalizedHeader = new string[0];
                this.normalizedInput = new double[0];
                this.rawHeader = new string[0];
                this.rawInput = new object[0];
            }

            public SampleInputVector(DateTime intendedOpenTime, ConsolidationRange consolidation, List<Bar> lastBars, double referenceVolatility, double sma)
                : base(intendedOpenTime)
            {
                this.consolidation = consolidation;
                this.referenceVolatility = referenceVolatility;

                // * 5, since we will put 5 values per each bar
                this.normalizedHeader = new string[lastBars.Count * 5 + 10];

                // +5, since we'll also include the consolidation coorinates (start / end / high / low) and reference volatility)
                this.rawHeader = new string[lastBars.Count * 5 + 15];

                this.normalizedInput = new double[this.normalizedHeader.Length];
                this.rawInput = new object[this.rawHeader.Length];

                var timeRangeStart = consolidation.Start.Ticks;
                var timeRange = consolidation.End.Ticks - timeRangeStart;
                var valueRangeStart = consolidation.RangeLow;
                var valueRange = consolidation.RangeHigh - valueRangeStart;

                this.rawHeader[0] = "c.start";
                this.rawHeader[1] = "c.end";
                this.rawHeader[2] = "c.high";
                this.rawHeader[3] = "c.low";
                this.rawHeader[4] = "vol";

                this.rawInput[0] = consolidation.Start;
                this.rawInput[1] = consolidation.End;
                this.rawInput[2] = consolidation.RangeHigh;
                this.rawInput[3] = consolidation.RangeLow;
                this.rawInput[4] = referenceVolatility;

                for (var i = 0; i < lastBars.Count; i++)
                {
                    var offset = i * 5;

                    this.normalizedHeader[offset + 0] = "b[" + i + "].t";
                    this.normalizedHeader[offset + 1] = "b[" + i + "].o";
                    this.normalizedHeader[offset + 2] = "b[" + i + "].h";
                    this.normalizedHeader[offset + 3] = "b[" + i + "].l";
                    this.normalizedHeader[offset + 4] = "b[" + i + "].c";

                    // -5 here, since first 5 are occupied with metadata
                    Array.Copy(this.normalizedHeader, offset, this.rawHeader, offset + 5, 5);

                    var nextBar = lastBars[i];

                    this.normalizedInput[offset + 0] = (nextBar.Time.Ticks - timeRangeStart) / (double)timeRange;
                    this.normalizedInput[offset + 1] = (nextBar.Open - valueRangeStart) / valueRange;
                    this.normalizedInput[offset + 2] = (nextBar.High - valueRangeStart) / valueRange;
                    this.normalizedInput[offset + 3] = (nextBar.Low - valueRangeStart) / valueRange;
                    this.normalizedInput[offset + 4] = (nextBar.Close - valueRangeStart) / valueRange;

                    // like previously: + 5 since first 5 are occupied with consolidation metadata
                    this.rawInput[offset + 5 + 0] = nextBar.Time;
                    this.rawInput[offset + 5 + 1] = nextBar.Open;
                    this.rawInput[offset + 5 + 2] = nextBar.High;
                    this.rawInput[offset + 5 + 3] = nextBar.Low;
                    this.rawInput[offset + 5 + 4] = nextBar.Close;
                }

                int normalizedPosition = lastBars.Count * 5;
                int position = lastBars.Count * 5 + 5;
                for (int i = 0; i <=4; i++)
                {
                    this.rawHeader[this.rawHeader.Length - 1 + i] = "MACD" + (i+1);
                    this.normalizedHeader[this.normalizedHeader.Length - 1] = "Indicator";

                    this.rawInput[this.rawHeader.Length - 1] = sma;
                    this.normalizedInput[this.normalizedHeader.Length - 1] = sma;
                }
				
				this.rawHeader[this.rawHeader.Length-1]="Indicatior";
				this.normalizedHeader[this.normalizedHeader.Length-1]="Indicator";
				
				this.rawInput[this.rawHeader.Length-1] = sma;
				this.normalizedInput[this.normalizedHeader.Length-1] = sma;
            }

            public double ReferenceVolatility
            {
                get { return this.referenceVolatility; }
            }

            public ConsolidationRange ConsolidationRange
            {
                get { return this.consolidation; }
            }

            public override string[] NormalizedHeader
            {
                get { return this.normalizedHeader; }
            }

            public override double[] NormalizedInput
            {
                get { return this.normalizedInput; }
            }

            public override string[] RawHeader
            {
                get { return this.rawHeader; }
            }

            public override object[] RawInput
            {
                get { return this.rawInput; }
            }

            public bool Equals(SampleInputVector other)
            {
                return this.Equals((object)other);
            }
        }
    }
}
