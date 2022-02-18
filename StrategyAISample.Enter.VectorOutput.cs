using System;

namespace NinjaTrader.Custom.Strategies
{
    public partial class StrategyAISample : StrategyBaseAI<StrategyAISample, StrategyAISample.SampleInputVector, StrategyAISample.SampleOutputVector>
    {
        protected override SampleOutputVector EmptySignal
        {
            get { return SampleOutputVector.EMPTY; }
        }

        public class SampleOutputVector : OutputVector, IEquatable<SampleOutputVector>
        {
            public static readonly SampleOutputVector EMPTY = new SampleOutputVector();

            private readonly double longRange;
            private readonly double shortRange;

            private SampleOutputVector()
                : base(SampleInputVector.EMPTY)
            {
                this.longRange = double.NaN;
                this.shortRange = double.NaN;
            }

            public SampleOutputVector(SampleInputVector signalContext, double longRange, double shortRange)
                : base(signalContext)
            {
                this.longRange = longRange;
                this.shortRange = shortRange;
            }

            public double LongRange
            {
                get { return this.longRange; }
            }

            public double ShortRange
            {
                get { return this.shortRange; }
            }

            public override string[] NormalizedHeader
            {
                get { return new string[] { "r.long, r.short" }; }
            }

            public override double[] NormalizedOutput
            {
                get { return new double[] { this.longRange / this.SignalContext.ReferenceVolatility, this.shortRange / this.SignalContext.ReferenceVolatility }; }
            }

            public override string[] RawHeader
            {
                get { return new string[] { "r.long, r.short" }; }
            }

            public override object[] RawOutput
            {
                get { return new object[] { this.longRange, this.shortRange }; }
            }

            public bool Equals(SampleOutputVector other)
            {
                return other != null && Object.Equals(this.SignalContext, other.SignalContext);
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as SampleOutputVector);
            }

            public override int GetHashCode()
            {
                return this.SignalContext == null ? 0 : this.SignalContext.GetHashCode();
            }
        }
    }
}
