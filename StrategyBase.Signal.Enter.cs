using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.Indicators;
using System.Collections.Generic;

namespace NinjaTrader.Custom.Strategies
{
    // this class defines the interface of the position enter signal; read to learn more
    public abstract partial class StrategyBase<TSelf, TSignalContext, TSignal>
    {
        // the signal interface (for convinience it's actually an abstract class)
        //   - please note the generic strategy param: in your implementation the signal should set it to the strategy class itself
        //   - if you do the above, then the signal might be able to see what the strategy context sees, which is:
        //      - the current time, position open, all the closing advisors that were set up, etc
        //      - all the strategy configuration parameters (which are, unfortunately, limited to the strategy object only)
        public abstract class PositionEnterSignal
        {
            protected PositionEnterSignal(TSelf strategy)
            {
                this.Strategy = strategy;
            }

            protected TSelf Strategy { get; private set; }

            // this is the only thing that the open signal is required to do: to be able to 
            // say what is the opening advice when anyone asks it about it; you may already
            // see that having the strategy context handy is a must if it's about to figure it out
            public abstract OpenRequest CurrentOpenRequest { get; }

            // if the enter signal has a dependency on any indicators that you'd like to have 
            // plotted automatically, this is the place when you should be creating those
            public virtual IEnumerable<Indicator> Indicators
            { 
                get { yield break; }
            }
        }

        public class OpenRequest
        {
            public OpenRequest(MarketPosition marketPosition, TSignalContext signalContext, TSignal signal)
            {
                this.MarketPosition = marketPosition;
                this.SignalContext = signalContext;
                this.Signal = signal;
            }

            // market position says whether we play Long (buy), Short (sell), or don't play (Flat)
            public MarketPosition MarketPosition { get; private set; }

            // the unique key of the signal; the string representation will be used to:
            //  - tag the positions
            //  - name a trade in trade window
            //  - name the opening orders in orders window
            //  - will be displayed on chart in strategy analyzer
            public TSignalContext SignalContext { get; private set; }

            // the data that has been used to calculate the signal
            //  - may be anything; should be something that
            //      - explains why we're opening
            //      - some context that will be then used by closing signals
            public TSignal Signal { get; private set; }
        }
    }
}
