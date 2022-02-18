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

            public SampleInputVector(DateTime intendedOpenTime, ConsolidationRange consolidation, List<Bar> lastBars, double referenceVolatility, double[] indicators)
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

                //stochastic przez 100 podzielic
                //+1 /2 dla MACD    -1 - > 0 1 ->1 
         
                int normalizedPosition = lastBars.Count * 5;
                int position = lastBars.Count * 5 + 5;

                for (int i = 0; i <=4; i++)
                {
                    this.rawHeader[position + i] = "MACD" + (i+1);
                    this.normalizedHeader[normalizedPosition + i] = "MACD" + (i + 1);

                    this.rawInput[position + i] = indicators[i];
                    this.normalizedInput[normalizedPosition + i] = indicators[i];
                }
				
				this.rawHeader[position + 4] ="StochasticsK1";
                this.rawInput[position + 4] = indicators[4];
                this.normalizedHeader[normalizedPosition + 4] = "StochasticsK1";
				this.normalizedInput[normalizedPosition + 4] = indicators[4]/100;

                this.rawHeader[position + 5] = "StochasticsD1";
                this.rawInput[position + 5] = indicators[5];
                this.normalizedHeader[normalizedPosition + 5] = "StochasticsD1";
                this.normalizedInput[normalizedPosition + 5] = indicators[5] / 100;

                this.rawHeader[position + 6] = "StochasticsK2";
                this.rawInput[position + 6] = indicators[6];
                this.normalizedHeader[normalizedPosition + 6] = "StochasticsK2";
                this.normalizedInput[normalizedPosition + 6] = indicators[6] / 100;

                this.rawHeader[position + 7] = "StochasticsD2";
                this.rawInput[position + 7] = indicators[7];
                this.normalizedHeader[normalizedPosition + 7] = "StochasticsD2";
                this.normalizedInput[normalizedPosition + 7] = indicators[7] / 100;

                this.rawHeader[position + 8] = "StochasticsK3";
                this.rawInput[position + 8] = indicators[8];
                this.normalizedHeader[normalizedPosition + 8] = "StochasticsK3";
                this.normalizedInput[normalizedPosition + 8] = indicators[8] / 100;

                this.rawHeader[position + 9] = "StochasticsD3";
                this.rawInput[position + 9] = indicators[9];
                this.normalizedHeader[normalizedPosition + 9] = "StochasticsD3";
                this.normalizedInput[normalizedPosition + 9] = indicators[9] / 100;
               
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
