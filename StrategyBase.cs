using NinjaTrader.Cbi;
using NinjaTrader.Custom.Extensions.Bars;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace NinjaTrader.Custom.Strategies
{
    // -------------------------------------------------------------------------------------------------------------------------------------------------------
    // an instance of a strategy in ninja is our 'God Object' (unfortunately);
    // this particular class aims to minimize the requirement of keeping all the implementation details of our strategy
    // in single place and introduces a concept of 'command & conquer'; while an instance of strategy that is derived from
    // here will still be sort-of a 'Good Object' from ninja perspective, its main responsibility will be to configure & delegate
    // the actual responsibilities to other classes; SPECIAL NOTE: you may wonder why all components are inner classes - this is 
    // because they want (or even - they need) to have access to strategy protected methods - the only reasonable way to split
    // the underlying algorithm into pieces was to make sure that they are isolated from each other - but still have the strategy context
    // -------------------------------------------------------------------------------------------------------------------------------------------------------
    // quick note about the generic parameters:
    //  - TSelf:
    //     - a derived class should put its own definition in here
    //     - this is simply a syntax sugar, that makes sure that all the inner classes can capture the strongly-typed instance of enclosing class
    //  - TSignalContext:
    //     - something like a primary key for our signal
    //     - the purpose of this context is to give a hint to strategy of what this signal is : so it does not react on the same signals
    //     - .ToString() representation of this context will be printed in the Trades / Orders table in ninja
    //     - ********** BEWARE **************************************************************************************************************
    //     - ********** MAKE THIS ONE UNIQUE PER TRADE; NINJA HAS IN-BUILD MECHANISM THAT MIGHT LINK PREVIOUSLY SET ORDERS IF YOU WON'T 
    //     - ********** BEWARE **************************************************************************************************************
    //     - for example: if the open signal is generated in context of London open, then the date for which this is happening might be this context;
    //                    in such case: the implementation of the open signal might not care whether a position open has already been made, since the
    //                    enclosing strategy will make sure that there will be only one open in this context
    //  - TSignal:
    //     - this is the signal for opening a trade - simply put, a collection of all the data we gather from the market that is being used in decision making
    //     - it can be literally anything; if the strategy is to go for the trend - it might be the current trend; if we are to play on particular event
    //       (like macro-report): it might be the details about the report itself; anyway - this is very specific to the strategy itself
    // -------------------------------------------------------------------------------------------------------------------------------------------------------
    // IMPORTANT NOTES: read before using this class:
    //  - NO INCREASE: with this implementation it is assumed that no position increase is implemented within a strategy
    //      - simply put: once the position is open it can only be closed (partially or fully), never increased
    //  - MARKET OPEN: with this implementation the only open position strategy is with the MKT order
    //      - please note that it is possible to implement something more fancy, but it requires a slight re-factoring, and will still keep the NO INCREASE limit
    //  - NO PARTIAL FILLS:
    //      - if partial fills are configured then this class will have issues; BIG ISSUES; do not configure partial fills
    // -------------------------------------------------------------------------------------------------------------------------------------------------------
    public abstract partial class StrategyBase<TSelf, TSignalContext, TSignal> : Strategy
        where TSelf : StrategyBase<TSelf, TSignalContext, TSignal>
        where TSignalContext : IEquatable<TSignalContext>
    {
        // internal stuff; being used to track what bar periods we have already registered
        private readonly IDictionary<BarPeriod, BarsCollection> registeredBarPeriods = new Dictionary<BarPeriod, BarsCollection>();

        private bool isPositionOpen = false;
        private TSignalContext lastEnterSignalContext;
        private TSignal lastEnterSignal;
        private DateTime lastPositionOpenTime;

        private PositionEnterSignal positionEnterSignal;
        private PositionQuantityAdvisor positionQuantityAdvisor;
        private CompositeExitSignal positionExitSignal;
        private BarsCollection defaultBarsCollection;

        // NOTE: most likely you won't need this prop, unless you'll be adding / extending some of the abstract classes
        [XmlIgnore]
        [Browsable(false)]
        protected TSelf Self
        {
            get
            {
                var self = this as TSelf;
                if (self == null)
                {
                    const string errorMessage = "The 'TSelf' generic argument is supposed to be the self-class "
                        + "(the whole thing is just an syntax sugar that allows to re-use the strategy method from within other classes..). "
                        + "If you are seeing this message it is very likely that you have not read the code before using it ;)";
                    throw new ArgumentException(errorMessage, "TSelf");
                }

                return self;
            }
        }

        // -----------------------------------------------
        // ------------ Actually useful stuff ------------
        // -----------------------------------------------

        // quick & easy access to the current time
        [XmlIgnore]
        [Browsable(false)]
        public DateTime CurrentTime
        {
            get { return this.Time[0]; }
        }

        // quick & easy access to the last position open time
        protected DateTime LastPositionOpenTime
        {
            get { return this.lastPositionOpenTime; }
        }

        // default bars that are being used by the strategy
        // NOTE: it is very unlikely that you will ever use this one, unless you will stay away from multi-time-frame & leave all config to ninja
        protected BarsCollection DefaultBarsCollection
        {
            get { return this.defaultBarsCollection; }
        }

        // quick access to enter signal that is being used by the strategy
        protected PositionEnterSignal EnterSignal
        {
            get { return this.positionEnterSignal; }
        }

        // quick access to exit signal that is being used by the strategy
        // NOTE: exit signal is always a composite signal:
        //   - composite signal never generates any close signal on its own
        //   - composite signal is a "plugin manager": so each close strategy might be implemented in isolation
        //   - see the composite signal for more information of how the position close process is being managed
        protected CompositeExitSignal ExitSignal
        {
            get { return this.positionExitSignal; }
        }

        // quick access to last trade (closed one)
        // might be null if no trade was made yet
        protected Trade LastClosedTrade
        {
            get
            {
                if (this.SystemPerformance.AllTrades.Count == 0)
                {
                    return null;
                }

                return this.SystemPerformance.AllTrades[this.SystemPerformance.AllTrades.Count - 1];
            }
        }

        // a convinient way to requesting bars of a specific type to be available from within strategy
        // NOTE: 
        //   - you can't just request a new bars collection at any time - it must be requested during initialization
        //   - because of the above: this method is to be used either:
        //      - in override of a OnStateChange, for an appropriate strategy state
        //      - in override of any method that creates any signal used by the strategy
        // SUPER-SPECIAL NOTE: if you are using this method then:
        //   - please DO NOT ADD DATA SERIES MANUALLY
        //   - if, for any reason, you are adding data series manually, then DO NOT USE THIS METHOD
        //   - the reason for the above is that the method itself is stateful and keeps the track of
        //     period-to-data-series index; anything added manually will abuse the state, and bar
        //     collections returned from here will have wrong bar periods linked
        protected BarsCollection GetBarsBrowser(BarPeriod barPeriod)
        {
            BarsCollection barsBrowser;
            if (this.registeredBarPeriods.TryGetValue(barPeriod, out barsBrowser))
            {
                return barsBrowser;
            }

            if (this.State != NinjaScript.State.Configure)
            {
                var errorMessage = "Seems that there is an attempt to register bars from the outside of the configure event.";
                throw new InvalidOperationException(errorMessage);
            }

            this.AddDataSeries(barPeriod.ToChartDataSeries());
            barsBrowser = new BarsCollection(this, this.registeredBarPeriods.Count + 1);
            this.registeredBarPeriods.Add(barPeriod, barsBrowser);
            return barsBrowser;
        }

        // on state change is the way to configure the strategy, and it is the ONLY WAY to setup available bars & stuff like this
        // the strategy class will always execute the code for setting up all the signals (both for enter / exit / quantity) in here
        // NOTE: this is more of an implementation detail - you may not care, unless you need to extend the initialization process with something fancy
        protected override void OnStateChange()
        {
            base.OnStateChange();

            switch (this.State)
            {
                case NinjaScript.State.Configure:
                    this.defaultBarsCollection = new BarsCollection(this, 0);
                    this.positionEnterSignal = this.CreateEnterSignal();
                    this.positionExitSignal = new CompositeExitSignal(this.Self, this.CreateExitSignals());
                    this.positionQuantityAdvisor = this.CreateQuanityAdvisor();
                    break;
                case NinjaScript.State.DataLoaded:
                    foreach (var indicator in this.positionEnterSignal.Indicators)
                    {
                        this.AddChartIndicator(indicator);
                    }

                    foreach (var indicator in this.positionExitSignal.Indicators)
                    {
                        this.AddChartIndicator(indicator);
                    }

                    break;
            }
        }

        // the first critical part of the strategy: implementing reaction for the incoming bars
        // unless you are about to implement something really fancy you will probably not need to touch this method at all
        // it is still worth to read what it does in order to understand the generic strategy flow
        protected override void OnBarUpdate()
        {
            base.OnBarUpdate();

            // this particular setting is for making the strategy NOT TRADE for a particular number of bars
            // SPECIAL NOTE: I don't fully understand an importance of this param from the ninja perspective.. but let's follow up a pattern
            if (this.CurrentBar < this.BarsRequiredToTrade)
            {
                return;
            }

            // at the base strategy level I assume that there will be no position increase during its lifetime (for simplicity)
            // this (of course) can be changed, but will require a little bit of re-writing this particular class (or throwing it away)
            if (this.Position.Quantity == 0)
            {
                // VERY IMPORTANT REMARK: strategy will receive bar update event for EACH REGISTERED BAR PERIOD
                // as you might notice, it does not care which one is it when asking for the open request
                // in order to make sure that you're not wasting your CPU cycles make sure that you react on the proper ones only
                // EVEN MORE IMPORTANT REMARK: strategy does not care about the context when asking for an opening advice
                // because of the above - it might become the performance problem if the position enter signal will recklesly run
                // the expensive computations each time it's being asked for advice (and this might be very frequent)
                var openRequest = this.positionEnterSignal.CurrentOpenRequest;
                if (openRequest == null)
                {
                    // enter signal says nothing - so we don't really do anything
                    return;
                }

                if (Object.Equals(openRequest.SignalContext, this.lastEnterSignalContext))
                {
                    // we've been already playing with this particular signal, nothing to do here
                    return;
                }

                // caching these, so we can pass the snapshot of what the enter singnal just seen to the other signals (when the position has been open)
                this.lastEnterSignal = openRequest.Signal;
                this.lastEnterSignalContext = openRequest.SignalContext;

                // finally, the actual position opening; please note that: 
                //   - we do react only on Long / Short advice (as the signal might say Flat: which is a hint not to open position)
                //   - the actual position size is delegated to the quantity advisor (so you might customize the risk management in separation from the actual open)
                //     - you might be tempted to extract this "copy-paste" to separate variable - don't do, since these are CPU cycles you don't need to waste if the advice says 'Flat' :)
                //   - open is MTK order; yeah, I was lazy - if you need something fancy go ahead an implement it on your own ;)
                // SPECIAL NOTE:
                //    - this.lastEnterSignalContext.ToString() being the last param is the position tag
                //    - the above one will allow to easily identify the particular trades in Trades / Transations views in ninja
                switch (openRequest.MarketPosition)
                {
                    case MarketPosition.Long:
                        this.EnterLong(
                            this.positionQuantityAdvisor.GetQuantity(openRequest),
                            this.lastEnterSignalContext.ToString());
                        break;
                    case MarketPosition.Short:
                        this.EnterShort(
                            this.positionQuantityAdvisor.GetQuantity(openRequest),
                            this.lastEnterSignalContext.ToString());
                        break;
                }
            }
            else
            {
                // if the position is already open - then for each bar we do receive we ask for the closing orders
                this.ProcessCloseOrders();
            }
        }

        private readonly Dictionary<string, Order> lastOrders = new Dictionary<string, Order>();

        // position update is the place where we are being told about the position changes (so: after some transaction was executed)
        // most likely you won't need to change this code, unless you're going to go for some really fancy algo
        // still, I recommend to read & undestand what's happening here, in order to have a good overview on the strategy flow
        protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
        {
            base.OnPositionUpdate(position, averagePrice, quantity, marketPosition);

            if (this.Position.Quantity == 0)
            {
                // position was closed: so all the advisors have their chance to do the post-close actions
                this.positionExitSignal.Cleanup();
                this.isPositionOpen = false;
                this.lastOrders.Clear();
                return;
            }

            // TODO: there is a big problem with partial fills in here, which is preventing from implementing anything
            // reasonable at the generic level, and the conceptual existence of partial fill concept would have to be
            // embedded into the strategy logic itself (which is something really horrible); in order to avoid writing
            // w full-ninja-facade I simply assume that no partial fill is ever happening, nor the position increase in
            // anything that is about to re-use this pretty class; so, if you need:
            //  - partial fill: then StrategyBase is no good for you; however, you will never ever need a partial fill
            //     - a very notable exception is when you would like to make a 'real' strategy, not the 'academic' one - but that's a separate story
            //  - position up / down manipulation: quite likely StrategyBase is no good;
            //     - it might be possible to adjust it with making the position increase possible, but I fear that it might be lengthy process
            if (!this.isPositionOpen)
            {
                // first fill is always an open (no partial fills)
                this.isPositionOpen = true;
                this.lastPositionOpenTime = this.CurrentTime;
                // first fill will always have the full size, so it is safe to initialize the close signals (no partial fills)
                this.positionExitSignal.Initialize(this.lastEnterSignalContext, this.lastEnterSignal, this.Position.Quantity);
            }

            // after the position update - we always go for placing the close orders (no need to wait for the better days)
            this.ProcessCloseOrders();
        }

        // close orders are fully managed by the strategy itself;
        // unless you don't want the advices semantic, or you're about to do something fancy - most likely you don't need to modify this method
        private void ProcessCloseOrders()
        {
            // all the advices from here are already 'normalized' with the composite signal (see the implementation!!)
            // PLEASE NOTE:
            //   - the last param is always the last signal context string representation
            //   - this will allow ninja to bind the order to this context (so it knows which position is being closed)
            //   - BE AWARE THAT: if the context is non-unique for all trades, then previously placed orders will magically come back - you don't want this, so:
            //       - ALWAYS KEEP YOUR TSignalContext UNIQUE per each trade
            // PLEASE ALSO NOTE:
            //   - the before-last param is the advice.Source; this allows to bind the particular signal names to the Trades / Orders windows as well as to Charts
            //   - see the signal implementation samples for more details
            foreach (var advice in this.positionExitSignal.CloseAdvices)
            {
                switch (this.Position.MarketPosition)
                {
                    case MarketPosition.Long:
                        if (advice.Price.HasValue)
                        {
                            switch (advice.AdviceType)
                            {
                                case AdviceType.StopLoss:
                                    this.ExitLongStopMarket(advice.Quantity, advice.Price.Value, advice.Source, this.lastEnterSignalContext.ToString());
                                    break;
                                case AdviceType.TakeProfit:
                                    this.ExitLongLimit(advice.Quantity, advice.Price.Value, advice.Source, this.lastEnterSignalContext.ToString());
                                    break;
                            }
                        }
                        else
                        {
                            this.ExitLong(advice.Quantity, advice.Source, this.lastEnterSignalContext.ToString());
                        }

                        break;
                    case MarketPosition.Short:
                        if (advice.Price.HasValue)
                        {
                            switch (advice.AdviceType)
                            {
                                case AdviceType.StopLoss:
                                    this.ExitShortStopMarket(advice.Quantity, advice.Price.Value, advice.Source, this.lastEnterSignalContext.ToString());
                                    break;
                                case AdviceType.TakeProfit:
                                    this.ExitShortLimit(advice.Quantity, advice.Price.Value, advice.Source, this.lastEnterSignalContext.ToString());
                                    break;
                            }
                        }
                        else
                        {
                            this.ExitShort(advice.Quantity, advice.Source, this.lastEnterSignalContext.ToString());
                        }

                        break;
                }
            }
        }

        // the first key element for the strategy: the enter signal used for the position open
        // see the StrategyBase.Signal.Enter to learn more about the enter signals
        // also see the sample strategy to learn more about the enter signals
        protected abstract PositionEnterSignal CreateEnterSignal();

        // the second key element for the strategy: the quantity advisor, which allows to apply
        // various risk strategies; see the Signal.Enter.Quantity to learn more about the quantity advisors
        // also see the sample strategy to learn more about the risk advisors
        protected abstract PositionQuantityAdvisor CreateQuanityAdvisor();

        // the third, and last, key element for the strategy: the exit signals; it is possible to have multiple
        // exit signals, which are then managed by the composite signal;
        // see the StrategyBase.Signal.Exit to see what are the exit signals
        // see the StrategyBase.Signal.Exit.Composite to see how the strategy signals are being managed
        // also see the sample strategy to learn more about the risk advisors
        protected abstract IEnumerable<PositionExitSignal> CreateExitSignals();
    }
}
