using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace NinjaTrader.Custom.Strategies
{
    // the implementation of the learn-mode for the AI
    // simply put: here we'll be capturing the data in the same fashion as for the real strategy, 
    // but instead of passing them to AI we'll collect the 'correct' aswer that the AI should give
    // and generate the csv file, which is then intended to be an AI learning data
    partial class StrategyBaseAI<TSelf, TSignalContext, TSignal>
    {
        public abstract class AbstractEnterAILearnMode : AbstractEnterAI
        {
            #region Constructor and private variables

            private DateTime lastDateTime = DateTime.MinValue;
            private readonly LinkedList<TSignalContext> pendingInputs = new LinkedList<TSignalContext>();

            protected AbstractEnterAILearnMode(TSelf strategy, InputVectorExtractor inputVectorExtractor)
                : base(strategy, inputVectorExtractor)
            {
            }

            #endregion

            protected override OpenRequest TranslateToOpenRequest(TSignalContext inputVector)
            {
                if (inputVector.IntendedOpenTime > this.lastDateTime)
                {
                    this.lastDateTime = inputVector.IntendedOpenTime;
                    this.pendingInputs.AddLast(inputVector);
                }
                while (this.pendingInputs.Count != 0)
                {
                    var next = this.pendingInputs.First.Value;
                    if (next.IntendedOpenTime.Add(this.Strategy.LearnModeDataPeriod.TimeOfDay) > this.Strategy.CurrentTime)
                    {
                        break;
                    }
                    var output = this.TranslateToSignal(next);
                    if (output != null)
                    {
                        this.SaveOutput(output);
                    }
                    pendingInputs.RemoveFirst();
                }
                // we never play, we just generate & dump the input sets here
                return new OpenRequest(
                    Cbi.MarketPosition.Flat,
                    this.Strategy.EmptySignalContext,
                    this.Strategy.EmptySignal);
            }

            protected abstract TSignal TranslateToSignal(TSignalContext next);

            #region Generation of the CSV output

            private const string outputDir = "C:/temp";

            private string normalizedOutputFile;
            private string rawOutputFile;
            private StreamWriter normalizedOutputWriter;
            private StreamWriter rawOutputWriter;
            private volatile bool backgroundWrite = true;

            private ConcurrentQueue<TSignal> logQueue = new ConcurrentQueue<TSignal>();

            public void InitBackgroundLogger()
            {
                new Thread(this.BackgroundLogger).Start();
            }

            private void BackgroundLogger()
            {
                while(this.backgroundWrite)
                {
                    TSignal nextSignal;
                    if (this.logQueue.TryDequeue(out nextSignal))
                    {
                        this.WriteRecord(nextSignal);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }

                TSignal signal;
                while (this.logQueue.TryDequeue(out signal))
                {
                    this.WriteRecord(signal);
                }
            }

            public void FlushWriters()
            {
                this.backgroundWrite = false;

                while(this.logQueue.Count != 0)
                {
                    Thread.Sleep(100);
                }

                if (this.normalizedOutputWriter != null)
                {
                    this.normalizedOutputWriter.Flush();
                    this.normalizedOutputWriter = null;
                }

                if (this.rawOutputWriter != null)
                {
                    this.rawOutputWriter.Flush();
                    this.rawOutputWriter = null;
                }
            }

            private void SaveOutput(TSignal output)
            {
                if (this.normalizedOutputFile == null)
                {
                    var dateString = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss", CultureInfo.InvariantCulture);
                    this.normalizedOutputFile = Path.Combine(outputDir, "normalized_output_" + dateString + ".csv");
                    this.rawOutputFile = Path.Combine(outputDir, "raw_output_" + dateString + ".csv");
                    this.normalizedOutputWriter = File.AppendText(this.normalizedOutputFile);
                    this.rawOutputWriter = File.AppendText(this.rawOutputFile);
                    {
                        var inputHeader = output.SignalContext.NormalizedHeader;
                        for (var i = 0; i < inputHeader.Length; i++)
                        {
                            WriteValueNormalized(this.normalizedOutputWriter, inputHeader[i]);
                            this.normalizedOutputWriter.Write(',');
                        }

                        var outputHeader = output.NormalizedHeader;
                        for (var i = 0; i < outputHeader.Length; i++)
                        {
                            WriteValueNormalized(this.normalizedOutputWriter, outputHeader[i]);
                            if (i == outputHeader.Length - 1)
                            {
                                this.normalizedOutputWriter.WriteLine();
                            }
                            else
                            {
                                this.normalizedOutputWriter.Write(',');
                            }
                        }
                    }

                    {
                        var inputHeader = output.SignalContext.RawHeader;
                        for (var i = 0; i < inputHeader.Length; i++)
                        {
                            WriteValueRaw(this.rawOutputWriter, inputHeader[i]);
                            this.rawOutputWriter.Write(',');
                        }

                        var outputHeader = output.RawHeader;
                        for (var i = 0; i < outputHeader.Length; i++)
                        {
                            WriteValueRaw(this.rawOutputWriter, outputHeader[i]);
                            if (i == outputHeader.Length - 1)
                            {
                                this.rawOutputWriter.WriteLine();
                            }
                            else
                            {
                                this.rawOutputWriter.Write(',');
                            }
                        }
                    }
                }

                this.logQueue.Enqueue(output);
                // this.WriteRecord(output);
            }

            private void WriteRecord(TSignal output)
            {
                {
                    var inputValues = output.SignalContext.NormalizedInput;
                    for (int i = 0; i < inputValues.Length; i++)
                    {
                        WriteValueNormalized(this.normalizedOutputWriter, inputValues[i]);
                        this.normalizedOutputWriter.Write(',');
                    }

                    var outputValues = output.NormalizedOutput;
                    for (var i = 0; i < outputValues.Length; i++)
                    {
                        WriteValueNormalized(this.normalizedOutputWriter, outputValues[i]);
                        if (i == outputValues.Length - 1)
                        {
                            this.normalizedOutputWriter.WriteLine();
                        }
                        else
                        {
                            this.normalizedOutputWriter.Write(',');
                        }
                    }
                }

                {
                    var inputValues = output.SignalContext.RawInput;
                    for (int i = 0; i < inputValues.Length; i++)
                    {
                        WriteValueRaw(this.rawOutputWriter, inputValues[i]);
                        this.rawOutputWriter.Write(',');
                    }

                    var outputValues = output.RawOutput;
                    for (var i = 0; i < outputValues.Length; i++)
                    {
                        WriteValueRaw(this.rawOutputWriter, outputValues[i]);
                        if (i == outputValues.Length - 1)
                        {
                            this.rawOutputWriter.WriteLine();
                        }
                        else
                        {
                            this.rawOutputWriter.Write(',');
                        }
                    }
                }
            }

            private static void WriteValueNormalized(StreamWriter writer, object targetValue)
            {
                if (targetValue is double)
                {
                    var doubleValue = (double)targetValue;
                    if (doubleValue >= 0)
                    {
                        writer.Write(' ');
                    }

                    writer.Write(string.Format(CultureInfo.InvariantCulture, "{0:N6}", doubleValue));
                }
                else
                {

                    var formattable = targetValue as IFormattable;
                    var stringValue = formattable != null
                        ? formattable.ToString(null, CultureInfo.InvariantCulture)
                        : targetValue.ToString();
                    writer.Write(stringValue.PadLeft(9));
                }
            }

            private static void WriteValueRaw(StreamWriter writer, object targetValue)
            {
                if (targetValue == null)
                {
                    writer.Write("(null)");
                }
                else if (targetValue is DateTime)
                {
                    DateTime dateTime = (DateTime)targetValue;
                    writer.Write(dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture).PadLeft(20));
                }
                else if (targetValue is double)
                {
                    writer.Write(string.Format(CultureInfo.InvariantCulture, "{0:N6}", (double)targetValue).PadLeft(20));
                }
                else
                {
                    var formattable = targetValue as IFormattable;
                    var stringValue = formattable != null
                        ? formattable.ToString(null, CultureInfo.InvariantCulture)
                        : targetValue.ToString();
                    writer.Write(stringValue.PadLeft(20));
                }
            }

            #endregion
        }
    }
}
