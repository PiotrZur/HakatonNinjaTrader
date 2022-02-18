using NinjaTrader.NinjaScript.Indicators;
using System.Collections.Generic;

namespace NinjaTrader.Custom.Strategies
{
    // this class defines the interface of the closing signals, and the signals themselves
    public abstract partial class StrategyBase<TSelf, TSignalContext, TSignal>
    {
        // each signal might be SL (we loose) or TP (we win) order
        // apart from pointing out the intention of the signal, each
        // of the signal types are separately managed (see the implementation)
        public enum AdviceType
        {
            StopLoss,
            TakeProfit
        }

        // expresses the intention of what should be done with the open position
        public sealed class CloseAdvice
        {
            // quantity to be closed, in relation to the initial quantity!
            //  - if the advisor is intending to close the full position, it should put the initial postion size, always !
            //  - see the examples for find out more
            public int Quantity { get; set; }

            // name of the source of an order
            //   - automatically initialized from the signal name
            //   - is displayed in trades window in strategy analyzer
            //   - is displayed in the orders window in strategy analyzer
            //   - is displayed on chart next to closing order
            public string Source { get; set; }

            // indicates whether the advice is for the stop loss or take profit; 
            public AdviceType AdviceType { get; set; }

            // if the price is set:
            //   - then it will be either stop order price or limit order price (depending whether we've got a SL or TP)
            // if the price is not set:
            //   - then it is assumed to be a MKT order advice (to be executed at any price, immediately)
            public double? Price { get; set; }
        }

        // an interface for all exit signals (abstract class for convinience)
        public abstract class PositionExitSignal
        {
            private readonly string signalName;
            private readonly AdviceType adviceType;

            public PositionExitSignal(TSelf strategy, string signalName, AdviceType adviceType)
            {
                this.Strategy = strategy;
                this.signalName = signalName;
                this.adviceType = adviceType;
            }

            public string SignalName
            {
                get { return this.signalName; }
            }

            public virtual IEnumerable<Indicator> Indicators
            {
                get { yield break; }
            }

            // the key point for the exit signal: a collection of closing advices
            //  - a single signal might suggest few advices, for instance: if it does intend to do partial closes
            //  - also: a composite signal is a multi-advice implementation that wraps everyone together
            public abstract IEnumerable<CloseAdvice> CloseAdvices { get; }

            protected TSelf Strategy { get; private set; }

            // signal context which was used when requesting open of the current position
            protected TSignalContext SignalContext { get; private set; }

            // the signal which was used when requesting open of the current position
            protected TSignal Signal { get; private set; }

            // initial position size - the position size that was requested to be open
            // NOTE: this might be different from the current position size if we're doing partial closes
            protected int InitialPositionSize { get; private set; }

            // this method is executed each time when the new position is open - if the exit signal
            // is stateful, it has an opportunity to initialize its state
            public virtual void Initialize(TSignalContext signalContext, TSignal signal, int initialPositionSize)
            {
                this.SignalContext = signalContext;
                this.Signal = signal;
                this.InitialPositionSize = initialPositionSize;
            }

            // this method is executed each time when the position is fully closed - if the exit signal
            // is stateful, it has an opportunity to clean up its state 
            public virtual void Cleanup()
            {
                this.SignalContext = default(TSignalContext);
                this.Signal = default(TSignal);
            }

            // helper method for creating a MKT order advice for a given quantity
            protected CloseAdvice CloseAdvice(int quantity)
            {
                return new CloseAdvice
                {
                    Source = this.signalName,
                    AdviceType = this.adviceType,
                    Quantity = quantity
                };
            }

            // helper method for creation SL/TP irder advice for a given quantity
            protected CloseAdvice CloseAdvice(int quantity, double price)
            {
                var advice = this.CloseAdvice(quantity);
                advice.Price = price;
                advice.Quantity = quantity;
                return advice;
            }
        }
    }
}
