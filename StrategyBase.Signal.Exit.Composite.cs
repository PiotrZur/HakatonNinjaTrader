using NinjaTrader.NinjaScript.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NinjaTrader.Custom.Strategies
{
    // a composite exit signal is a way that the strategy uses to manage all configured closing advices
    // this is the only exit signal that is explicitly created by the base strategy
    // it is not advised to create an instance of this class on your own
    // HOWEVER: it might be beneficial to slightly fine-tune this one, or create a variant that does slightly different decision making :)
    public abstract partial class StrategyBase<TSelf, TSignalContext, TSignal>
    {
        public class CompositeExitSignal : PositionExitSignal
        {
            private readonly List<PositionExitSignal> signals;

            public CompositeExitSignal(TSelf strategy, IEnumerable<PositionExitSignal> signals)
                // special note: the name / type does not matter, since this one does not generate its own advices
                : base(strategy, String.Empty, default(AdviceType)) 
            {
                this.signals = signals.ToList();
            }

            public override IEnumerable<Indicator> Indicators
            {
                get { return this.signals.SelectMany(nestedSignal => nestedSignal.Indicators).Distinct(); }
            }

            // composite advisor always presents all signals it wraps
            public IEnumerable<PositionExitSignal> AllSignals
            {
                get { return this.signals; }
            }

            // the main purpose of the composite signal is to manage all other signals configured for the strategy; it will:
            //   - first of all - it will prefer MKT orders (since they are executed immediately)
            //   - if the 100% of the position quantity was not covered by the MKT orders from any advisors then it will try to place SL / TP orders:
            //     - both SL & TP are processed separately
            //     - when the signal is to pick one from the multiple orders, it will always prefer one that would be executed sooner
            //     - if the best advice does not cover the remaining position quantity, then next one will be picked, etc.
            public override IEnumerable<CloseAdvice> CloseAdvices
            {
                get
                {
                    // the portion of the position we have already closed
                    var quantityClosed = this.InitialPositionSize - this.Strategy.Position.Quantity;

                    // all advices from each & every advisor that are about to close some of the position
                    var allAdvices = this.signals
                        .SelectMany(signal => signal.CloseAdvices)
                        // the below ones are partial closes which have already been taken care of
                        .Where(signal => signal.Quantity > quantityClosed)
                        .ToList();

                    // MKT orders will execute immediately anyway - so we simply pick up the best one (if any) so we can throw it into the MKT
                    var bestMarketOrder = allAdvices
                        .Where(advice => !advice.Price.HasValue)
                        .OrderByDescending(advice => advice.Quantity)
                        .FirstOrDefault();

                    // assuming that the above one is executed: the remainder is what we should be covering with SL / TP orders
                    var remainingQuantity = this.Strategy.Position.Quantity;
                    if (bestMarketOrder != null)
                    {
                        yield return bestMarketOrder;
                    }

                    if (bestMarketOrder != null)
                    {
                        remainingQuantity -= bestMarketOrder.Quantity;
                        if (remainingQuantity <= 0)
                        {
                            yield break;
                        }
                    }

                    // everything that has a price: will be used to place SL / TP orders
                    var orderAdvices = allAdvices.Where(advice => advice.Price.HasValue);

                    // NOTE: the way we're going to approach here is the fast-kill way:
                    // if an order would have been executed faster, then it will go faster

                    // another NOTE: we place all the remaining quantity orders for both SL & TP SEPARATELY !
                    // - this actually makes sense, since they're are (or - supposed to be - on the opposite sides of the market)

                    // VERY SPECIAL NOTE: not really sure how nasty ninja is when it comes to screwing up executions to emulate
                    // the real market insane volatility spikes; also it seems to have some in-build way of actually exiting positions
                    // when it was requested to do so, but I have never really had a chance to test that thing

                    // this particular flag will come in handy when sorting orders by the price 
                    // (because of the different direction, higher / lower prices will be more prefered)
                    var isLong = this.Strategy.Position.MarketPosition == Cbi.MarketPosition.Long;

                    var stopLossOrdersSorted = orderAdvices
                        .Where(advice => advice.AdviceType == AdviceType.StopLoss)
                        .OrderBy(advice => advice.Price.Value * (isLong ? -1 : 1));

                    var alreadyClosed = quantityClosed + (bestMarketOrder == null ? 0 : bestMarketOrder.Quantity);

                    var remainingStopQuantity = remainingQuantity;
                    foreach (var orderAdvice in stopLossOrdersSorted)
                    {
                        var advice = orderAdvice;
                        var effectiveQuantity = orderAdvice.Quantity - alreadyClosed;
                        if (effectiveQuantity <= 0)
                        {
                            continue;
                        }

                        advice.Quantity = Math.Min(remainingStopQuantity, effectiveQuantity);
                        yield return advice;
                        remainingStopQuantity -= advice.Quantity;
                        if (remainingStopQuantity == 0)
                        {
                            break;
                        }
                    }

                    // TODO: yeah, nasty copy-paste, fix it hurts your eyes :P
                    var takeProfitSorted = orderAdvices
                        .Where(advice => advice.AdviceType == AdviceType.TakeProfit)
                        .OrderBy(advice => advice.Price.Value * (isLong ? 1 : -1));

                    var remainingLimitQuantity = remainingQuantity;
                    foreach (var orderAdvice in takeProfitSorted)
                    {
                        var advice = orderAdvice;
                        var effectiveQuantity = orderAdvice.Quantity - alreadyClosed;
                        if (effectiveQuantity <= 0)
                        {
                            continue;
                        }

                        advice.Quantity = Math.Min(remainingLimitQuantity, effectiveQuantity);
                        yield return advice;
                        remainingLimitQuantity -= advice.Quantity;
                        if (remainingLimitQuantity == 0)
                        {
                            break;
                        }
                    }
                }
            }

            public override void Initialize(TSignalContext signalContext, TSignal signal, int initialPositionSize)
            {
                base.Initialize(signalContext, signal, initialPositionSize);
                this.signals.ForEach(nestedSignal => nestedSignal.Initialize(signalContext, signal, initialPositionSize));
            }

            public override void Cleanup()
            {
                base.Cleanup();
                this.signals.ForEach(nestedSignal => nestedSignal.Cleanup());
            }
        }
    }
}
