using System;
using System.Globalization;

namespace NinjaTrader.Custom.Strategies
{
    // the position enter is AI-bound, so we'll simply make sure that we're going to provide an abstraction for interaction with the AI
    partial class StrategyBaseAI<TSelf, TSignalContext, TSignal>
    {
        public abstract class InputVector : IEquatable<InputVector>
        {
            // each signal context has to be bound to something that will be a PK from the strategy perspective
            // in our context: this PK is the current time (for which we did extract the input vector)
            protected InputVector(DateTime intendedOpenTime)
            {
                this.IntendedOpenTime = intendedOpenTime;
            }

            public DateTime IntendedOpenTime { get; private set; }

            // ---- there are 2 views on the input vector itself: normalized & RAW
            // ------- normalized is something that can be posted into the AI directly; ideally:
            //            -  all values should be normalized to [0..1] domain
            //            -  actually, it does not have to be [0..1] but rather [x..y], with a note that if the input values
            //               domain is within upfront-defined boundaries it will greatly simplify the work on the AI end
            //            -  the restriction is not 'fixed' in a strict sense, of course; this MIGHT be an open domain, or
            //               might have the normalization domain defined in a way that the values will occasionally be outside
            //               its bounds, but the tests we've been executing indicate that putting the "raw" market values
            //               (like: current open / high / low / close, or  current time ticks), makes it much more
            //               difficult for AI to come up with something reasonable
            // ------- RAW is the pure, RAW values that we use, without any normalization
            //            -  ninja is not very test-friendly, and this practice has proven itself to be very helpful when
            //               it comes to early debugging of the extracting market values
            // ------- SPECIAL NOTES:
            //            -  if this is to be effective the vector sizes should be the same
            //            -  of course you might have completely different idea for the AI integration - then just throw away this class and create something new :)

            // field names for the normalized input vector
            public abstract string[] NormalizedHeader { get; }

            // field values for the normalized input vector
            public abstract double[] NormalizedInput { get; }

            // field names for the RAW extract of market data
            public abstract string[] RawHeader { get; }

            // field values for the RAW extract of market data
            public abstract object[] RawInput { get; }

            public override string ToString()
            {
                return this.IntendedOpenTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            }

            public bool Equals(InputVector other)
            {
                return other != null && this.IntendedOpenTime.Equals(other.IntendedOpenTime);
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as InputVector);
            }

            public override int GetHashCode()
            {
                return this.IntendedOpenTime.GetHashCode();
            }
        }

        // output vector is the answer from the AI, and it is (at generic level) very similar to input vector
        // apart from being a wrapper in the input, it adds its own fields (both normalized & RAW)
        // having this output defined this way is going to be a great boost to the initial testing process
        // (as we would be in position to rapidly test various parameters based on once-generated csv files)
        public abstract class OutputVector : IEquatable<OutputVector>
        {
            protected OutputVector(TSignalContext signalContext)
            {
                this.SignalContext = signalContext;
            }

            // signal context is actually an input vector - but a specific one, the once that enclosing class will define
            public TSignalContext SignalContext { get; private set; }

            // field names for the normalized input vector
            public abstract string[] NormalizedHeader { get; }

            // field values for the normalized input vector
            public abstract double[] NormalizedOutput { get; }

            // field names for the RAW extract of market data
            public abstract string[] RawHeader { get; }

            // field values for the RAW extract of market data
            public abstract object[] RawOutput { get; }

            public override string ToString()
            {
                return this.SignalContext.ToString();
            }

            public bool Equals(OutputVector other)
            {
                return other != null && Object.Equals(this.SignalContext, other.SignalContext);
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj as OutputVector);
            }

            public override int GetHashCode()
            {
                return this.SignalContext.GetHashCode();
            }
        }

        // a helper property, that will allow to create an empty advice
        protected abstract TSignalContext EmptySignalContext { get; }

        // a helper property, that will allow to create an empty advice
        protected abstract TSignal EmptySignal { get; }

        // an interface for extracting the input vector from the strategy
        // separated from the actual signals to satisfy the single-responsibility rule
        // (as the signals themselves are responsible for translating the vector to position enter request)
        public abstract class InputVectorExtractor
        {
            public abstract TSignalContext CurrentInputVector { get; }
        }

        // the base class for the AI advisor is simply bound to the vector extractor
        // see the enter signal implementations to see what is the idea behind this one
        public abstract class AbstractEnterAI : PositionEnterSignal
        {
            private InputVectorExtractor inputVectorExtractor;

            protected AbstractEnterAI(TSelf strategy, InputVectorExtractor inputVectorExtractor)
                : base(strategy)
            {
                this.inputVectorExtractor = inputVectorExtractor;
            }

            public override OpenRequest CurrentOpenRequest
            {
                get
                {
                    var inputVector = this.inputVectorExtractor.CurrentInputVector;
                    if (inputVector == null)
                    {
                        return new OpenRequest(
                            Cbi.MarketPosition.Flat,
                            this.Strategy.EmptySignalContext,
                            this.Strategy.EmptySignal);
                    }

                    return this.TranslateToOpenRequest(inputVector);
                }
            }

            protected abstract OpenRequest TranslateToOpenRequest(TSignalContext inputSet);
        }

        protected override PositionEnterSignal CreateEnterSignal()
        {
            if (this.LearnModeEnabled)
            {
                return this.CreateLearnModeEnterSignal();
            }


            return this.CreatePlayerEnterSignal();
        }

        protected abstract AbstractEnterAILearnMode CreateLearnModeEnterSignal();

        protected abstract AbstractEnterAIPlayer CreatePlayerEnterSignal();
    }
}
