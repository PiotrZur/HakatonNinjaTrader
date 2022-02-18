using System;
using System.Linq;

namespace NinjaTrader.Custom.Strategies
{
    // this class defines the interface of the quantity advisor (risk management)
    // it also provides some implementation of the typical risk advisors
    partial class StrategyBase<TSelf, TSignalContext, TSignal>
    {
        // the quantity advisor interface (for convinience an abstract class)
        public abstract class PositionQuantityAdvisor
        {
            public PositionQuantityAdvisor(TSelf strategy)
            {
                this.Strategy = strategy;
            }

            protected TSelf Strategy { get; private set; }

            // the one an only thing that our quantity advisor is supposed to do
            // is to say how big the position should be for a particular open request
            public abstract int GetQuantity(OpenRequest open);
        }

        // this is the simplest way of managing the position size that can be configured for strategy:
        //   - each position will have fixed size
        //   - this means that the open orders will always open the same amount
        //   - DO NOT BE FOOLED:
        //     - it does not mean that each trade is of equal value (or equal risk)
        //     - the above would be true only if the possible PnL was equal for each trade
        //        - so it actually MIGHT be true, buy only if you have a fixed-distance stop-loss order
        public class FixedQuantityAdvisor : PositionQuantityAdvisor
        {
            private readonly int quantity;

            public FixedQuantityAdvisor(TSelf strategy, int quantity)
                : base(strategy)
            {
                this.quantity = quantity;
            }

            public override int GetQuantity(OpenRequest open)
            {
                return this.quantity;
            }
        }

        // an abstract class that will adjust the position size to keep the risk at fixed level 
        // the idea here is to make sure that each of the trade will be of equal value in case it will be a LOSS
        // the class is abstract simply because both concepts of fixed risk & re-investment risks - see next classes
        // SPECIAL NOTE:
        //   - for the simplification it is assumed that the accounting currency of the underlying instrument is equal to account currency
        //   - in most cases this is a very bad assumption; also - running a multi-instrument portfolio will generate some nonsense on the final value
        //   - the simplification is done because ninja does not have a good solution for CCY rates and ad-hoc access to them when running backtest
        //   - still, running a single CCY pair in isolation will generate stable results
        public abstract class RiskBasedQuantityAdvisor : PositionQuantityAdvisor
        {
            public RiskBasedQuantityAdvisor(TSelf strategy)
                : base(strategy)
            {
            }

            // override this property to express how much money is to be LOST in case when the trade will hit the SL order
            protected abstract double MoneyToRisk { get; }

            // override this method so the closing advisor knows where the SL order will be placed for a given open position
            protected abstract double GetPotentialLossDistance(OpenRequest open);

            public override int GetQuantity(OpenRequest open)
            {
                // the formula below is correct for the FX market
                return Math.Max(1, (int)(this.MoneyToRisk / this.GetPotentialLossDistance(open)));
            }
        }

        // the quantity advisor that will make sure that each trade will risk the same amount on money
        //  - it does not mean that in case when we hit SL each trade will actually loose the same amount
        //  - the above is true because the position size is determined BEFORE we put the open order, which is MKT order
        //  - the real execution price might be different from the assumed one; also, SL is not guaranteed to execute at the given limit
        // NOTE: this class is still abstract, because it does not have a clue where the actual SL order will be put;
        //       the strategy-specific override is the only one who might know this
        public abstract class FixedRiskQuantityAdvisor : RiskBasedQuantityAdvisor
        {
            private readonly double moneyToRisk;

            // money to risk - is the actual money we want (or, agree to, because we never 'want') to loose for each SL hit
            public FixedRiskQuantityAdvisor(TSelf strategy, double moneyToRisk)
                : base(strategy)
            {
                this.moneyToRisk = moneyToRisk;
            }

            protected override double MoneyToRisk
            {
                get { return this.moneyToRisk; }
            }
        }

        // the quantity advisor that will make sure that each trade will risk the same % of money we own
        // this is the most basic concept of re-investment - portion of each profitable trade is immediately
        // re-invested, while each loss will decrease the investment size; 
        // the class is based on the same abstract class as the fixed risk: the only thing that changes 
        // here is the amount of money we put on the table before we trade, which is dynamically adjusted based on the wallet size
        public abstract class PercentageRiskQuantityAdvisor : RiskBasedQuantityAdvisor
        {
            private readonly double accountPercentageToRisk;

            // account percentage to risk - the portion of money we're ready to loose per each SL hit
            public PercentageRiskQuantityAdvisor(TSelf strategy, double accountPercentageToRisk)
                : base(strategy)
            {
                this.accountPercentageToRisk = accountPercentageToRisk;
            }

            protected override double MoneyToRisk
            {
                get
                {
                    // funny that this is not available via account while the strategy is back-tested
                    var realizedPnl = this.Strategy.SystemPerformance.AllTrades.Select(trade => trade.ProfitCurrency + trade.Commission).Sum();
                    // cash will be set to its initial value all the time while the backtest is running
                    var initialCash = this.Strategy.Account.Get(Cbi.AccountItem.CashValue, this.Strategy.Account.Denomination);
                    var totalMoney = initialCash + realizedPnl;
                    return totalMoney * this.accountPercentageToRisk;
                }
            }
        }
    }
}
