using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AlephionMath;

namespace Differentiation.Benchmarking
{
    public static class AlephionBenchmarkRunnerExtended
    {
        public static BenchmarkSummary RunWithReports(
            string datasetJsonPath,
            string outputSummaryJsonPath,
            string outputCsvPath,
            string outputLatexPath,
            IAlephionOps<Alephion> ops,
            int minReportedOrder = 1)
        {
            if (ops == null)
                throw new ArgumentNullException(nameof(ops));
            if (!File.Exists(datasetJsonPath))
                throw new FileNotFoundException("Dataset JSON not found.", datasetJsonPath);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            TfDataset dataset;
            using (var stream = File.OpenRead(datasetJsonPath))
            {
                dataset = JsonSerializer.Deserialize<TfDataset>(stream, jsonOptions)
                          ?? throw new InvalidOperationException("Failed to deserialize dataset.");
            }

            var globalAcc = new Dictionary<int, ErrorAccumulator>();
            for (int k = 0; k <= dataset.MaxOrder; k++)
                globalAcc[k] = new ErrorAccumulator();

            var summary = new BenchmarkSummary
            {
                DatasetPath = datasetJsonPath,
                MaxOrder = dataset.MaxOrder
            };

            var allRows = new List<ErrorRow>();
            var watchDiff = System.Diagnostics.Stopwatch.StartNew();
            foreach (var func in dataset.Functions)
            {
                watchDiff.Start();
                var differentiator = new AlephionDifferentiatorDouble(func.Dimension, dataset.MaxOrder);
                watchDiff.Stop();
                var functionAcc = new Dictionary<int, ErrorAccumulator>();
                for (int k = 0; k <= dataset.MaxOrder; k++)
                    functionAcc[k] = new ErrorAccumulator();

                for (int pointIndex = 0; pointIndex < func.Points.Count; pointIndex++)
                {
                    var point = func.Points[pointIndex];
                    watchDiff.Start();
                    Alephion evaluated = differentiator.Evaluate(
                        vars => BenchmarkFunctionsAlephion.Evaluate(
                            func.Name,
                            vars,
                            ops,
                            differentiator.Plan.RequiredLength
                        ),
                        point.X
                    );
                    watchDiff.Stop();
                    foreach (var alpha in func.MultiIndices)
                    {
                        int order = AlephionDerivativePlan.TotalOrder(alpha);
                        if (order < minReportedOrder)
                            continue;

                        string key = string.Join(",", alpha);
                        if (!point.Derivatives.TryGetValue(key, out double exact))
                            throw new InvalidOperationException($"Derivative key '{key}' not found in dataset.");
                        watchDiff.Start();
                        double predicted = differentiator.ExtractDerivative(evaluated, alpha);
                        watchDiff.Stop();
                        double absError = Math.Abs(predicted - exact);
                        double sqError = (predicted - exact) * (predicted - exact);
                        var encodedDegree = differentiator.Plan.Encode(alpha);

                        functionAcc[order].Add(predicted - exact);
                        globalAcc[order].Add(predicted - exact);

                        allRows.Add(new ErrorRow
                        {
                            FunctionName = func.Name,
                            FunctionType = func.Type,
                            Dimension = func.Dimension,
                            PointIndex = pointIndex,
                            Point = FormatPoint(point.X),
                            MultiIndex = key,
                            Order = order,
                            EncodedDegree = encodedDegree,
                            ExactValue = exact,
                            PredictedValue = predicted,
                            AbsError = absError,
                            SquaredError = sqError
                        });
                    }
                }

                var byOrder = new Dictionary<int, OrderRmse>();
                for (int k = minReportedOrder; k <= dataset.MaxOrder; k++)
                    byOrder[k] = functionAcc[k].ToResult(k);

                summary.ByFunction.Add(new FunctionRmse
                {
                    Name = func.Name,
                    Type = func.Type,
                    Dimension = func.Dimension,
                    ByOrder = byOrder
                });
            }
            Console.WriteLine($"Elapsed time in ms: {watchDiff.ElapsedMilliseconds}");
            Console.WriteLine($"Elapsed time in sec: {watchDiff.ElapsedMilliseconds/1000}");
            for (int k = minReportedOrder; k <= dataset.MaxOrder; k++)
                summary.GlobalByOrder[k] = globalAcc[k].ToResult(k);

            File.WriteAllText(
                outputSummaryJsonPath,
                JsonSerializer.Serialize(summary, jsonOptions)
            );

            WriteErrorsCsv(outputCsvPath, allRows);
            WriteLatexRmseTable(outputLatexPath, summary, minReportedOrder);

            return summary;
        }

        private static string FormatPoint(IReadOnlyList<double> x)
        {
            return string.Join(
                ";",
                x.Select(v => v.ToString("G17", CultureInfo.InvariantCulture))
            );
        }

        private static void WriteErrorsCsv(string path, IReadOnlyList<ErrorRow> rows)
        {
            var sb = new StringBuilder();

            sb.AppendLine(
                "function_name,function_type,dimension,point_index,point,multiindex,order,encoded_degree,exact_value,predicted_value,abs_error,squared_error"
            );

            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",",
                    Csv(row.FunctionName),
                    Csv(row.FunctionType),
                    row.Dimension.ToString(CultureInfo.InvariantCulture),
                    row.PointIndex.ToString(CultureInfo.InvariantCulture),
                    Csv(row.Point),
                    Csv(row.MultiIndex),
                    row.Order.ToString(CultureInfo.InvariantCulture),
                    row.EncodedDegree.ToString(CultureInfo.InvariantCulture),
                    row.ExactValue.ToString("G17", CultureInfo.InvariantCulture),
                    row.PredictedValue.ToString("G17", CultureInfo.InvariantCulture),
                    row.AbsError.ToString("G17", CultureInfo.InvariantCulture),
                    row.SquaredError.ToString("G17", CultureInfo.InvariantCulture)
                ));
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string Csv(string s)
        {
            s ??= "";
            if (s.Contains('"'))
                s = s.Replace("\"", "\"\"");
            if (s.Contains(',') || s.Contains(';') || s.Contains('"') || s.Contains('\n'))
                return $"\"{s}\"";
            return s;
        }

        private static void WriteLatexRmseTable(string path, BenchmarkSummary summary, int minReportedOrder)
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"\begin{table}[ht]");
            sb.AppendLine(@"\centering");
            sb.AppendLine(@"\caption{RMSE of the derivatives}");
            sb.AppendLine(@"\label{tab:alephion_rmse_orders}");
            sb.AppendLine(@"\begin{tabular}{cccc}");
            sb.AppendLine(@"\hline");
            sb.AppendLine(@"Order & Number of comparisons & RMSE & MaxAbsError \\");
            sb.AppendLine(@"\hline");

            for (int order = minReportedOrder; order <= summary.MaxOrder; order++)
            {
                if (!summary.GlobalByOrder.TryGetValue(order, out var row))
                    continue;

                sb.AppendLine(
                    $"{order} & {row.Count} & {ToLatexSci(row.Rmse)} & {ToLatexSci(row.MaxAbsError)} \\\\"
                );
            }

            sb.AppendLine(@"\hline");
            sb.AppendLine(@"\end{tabular}");
            sb.AppendLine(@"\end{table}");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string ToLatexSci(double value)
        {
            if (value == 0.0)
                return "$0$";

            string s = value.ToString("0.0000000000E+00", CultureInfo.InvariantCulture);
            int ePos = s.IndexOf('E');
            string mantissa = s.Substring(0, ePos);
            string exponent = s.Substring(ePos + 1);

            int exp = int.Parse(exponent, CultureInfo.InvariantCulture);
            return $"${mantissa} \\times 10^{{{exp}}}$";
        }
    }
}