using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NinjaTrader.Custom.Strategies
{
    // the player signal is where we integrate with the AI, and we actually start putting some position enter advices
    // player can work in 2 modes:
    //   - regressed mode, where it is assumed that all the answers from AI are provided via the input file
    //   - on-line mode, where each input vector will be automatically requested to AI (so it is actually integrated) 
    //      - with special note: this one has not been implemented - one day I will eventually do this
    // in either of these modes player has only one task:
    //   - translate the AI output into the actual open position advice
    partial class StrategyBaseAI<TSelf, TSignalContext, TSignal>
    {
        public abstract class AbstractEnterAIPlayer : AbstractEnterAI
        {
            private IDictionary<DateTime, string[]> regressedOutput;

            protected AbstractEnterAIPlayer(TSelf strategy, InputVectorExtractor inputVectorExtractor)
                : base(strategy, inputVectorExtractor)
            {
            }

            protected override OpenRequest TranslateToOpenRequest(TSignalContext inputVector)
            {
                var outputSignal = this.GetOutputSignal(inputVector);
                if (outputSignal == null)
                {
                    return new OpenRequest(
                        Cbi.MarketPosition.Flat,
                        this.Strategy.EmptySignalContext,
                        this.Strategy.EmptySignal);
                }

                return this.TranslateToOpenRequest(outputSignal);
            }

            private TSignal GetOutputSignal(TSignalContext inputVector)
            {
                if (this.Strategy.PlayerModeRegression)
                {
                    if (this.regressedOutput == null)
                    {
                        this.regressedOutput = this.LoadRegressedOutput();
                    }

                    string[] outputSignalCsv;
                    if (!this.regressedOutput.TryGetValue(inputVector.IntendedOpenTime, out outputSignalCsv))
                    {
                        return null;
                    }

                    return this.CsvLineToOutput(inputVector, outputSignalCsv);
                }

                throw new NotImplementedException("On-line mode has not been implemented.. feel free to implement one ;)");
            }

            private IDictionary<DateTime, string[]> LoadRegressedOutput()
            {
                return File
                    .ReadAllLines(this.Strategy.PlayerModeRegressionFilePath)
                    .Skip(1)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Split(','))
                    .Where(line => !Object.Equals(this.CsvLineToDateTime(line), default(DateTime)))
                    .ToDictionary(line => this.CsvLineToDateTime(line));
            }

            protected abstract DateTime CsvLineToDateTime(string[] line);

            protected abstract TSignal CsvLineToOutput(TSignalContext inputVector, string[] line);

            protected abstract OpenRequest TranslateToOpenRequest(TSignal signal);
        }
    }
}
