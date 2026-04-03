using AlephionMath;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Differentiation.Benchmarking
{
    public sealed class AlephionDerivativePlan
    {
        public int VariableCount { get; }
        public int MaxOrder { get; }
        public int BaseValue => MaxOrder + 1;
        public decimal[] Weights { get; }

        public int MaxEncodedDegree { get; }
        public int RequiredLength => 60;

        public AlephionDerivativePlan(int variableCount, int maxOrder)
        {
            if (variableCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(variableCount));
            if (maxOrder < 0)
                throw new ArgumentOutOfRangeException(nameof(maxOrder));

            VariableCount = variableCount;
            MaxOrder = maxOrder;
            var dlt = 0.1m;
            Weights = new decimal[variableCount];
            checked
            {
                for (int i = 0; i < variableCount; i++)
                {
                    Weights[i] = 1 + dlt;
                    dlt /= 10.0m;
                }

            }

        }

        public decimal Encode(IReadOnlyList<int> alpha)
        {
            int order = 0;
            decimal code = 0;

            checked
            {
                for (int i = 0; i < VariableCount; i++)
                {
                    order += alpha[i];
                    code += alpha[i] * Weights[i];
                }
            }

            if (order > MaxOrder)
                throw new ArgumentException("Multiindex order exceeds MaxOrder.", nameof(alpha));

            return code;
        }

        public static int TotalOrder(IReadOnlyList<int> alpha)
        {
            int s = 0;
            for (int i = 0; i < alpha.Count; i++)
                s += alpha[i];
            return s;
        }
    }

    public sealed class AlephionDifferentiatorDouble
    {
        public AlephionDerivativePlan Plan { get; }

        public AlephionDifferentiatorDouble(int variableCount, int maxOrder)
        {
            Plan = new AlephionDerivativePlan(variableCount, maxOrder);
        }

        public Alephion[] CreateShiftedVariables(IReadOnlyList<double> point)
        {
            if (point == null)
                throw new ArgumentNullException(nameof(point));
            if (point.Count != Plan.VariableCount)
                throw new ArgumentException("Invalid point dimension.", nameof(point));

            var vars = new Alephion[Plan.VariableCount];

            Alephion eps = new Alephion(Alephion.CreateInfinitesimal(), Plan.RequiredLength);

            for (int i = 0; i < Plan.VariableCount; i++)
            {
                var atom = new AlephAtom(1, Plan.Weights[i]);
                var shift = Alephion.CreateFromAtom(atom);
                vars[i] = new Alephion(point[i], Plan.RequiredLength) + shift;
            }

            return vars;
        }

        public Alephion Evaluate(Func<Alephion[], Alephion> function, IReadOnlyList<double> point)
        {
            var vars = CreateShiftedVariables(point);
            return function(vars);
        }

        public double ExtractDerivative(Alephion evaluatedValue, IReadOnlyList<int> multiIndex)
        {
            decimal degree = Plan.Encode(multiIndex);
            double coeff = Convert.ToDouble(
                evaluatedValue.GetCoefByExp(degree),
                CultureInfo.InvariantCulture
            );

            return MultiFactorialAsDouble(multiIndex) * coeff;
        }

        private static double MultiFactorialAsDouble(IReadOnlyList<int> alpha)
        {
            double result = 1.0;
            for (int i = 0; i < alpha.Count; i++)
                result *= Factorial(alpha[i]);
            return result;
        }

        private static double Factorial(int n)
        {
            double r = 1.0;
            for (int i = 2; i <= n; i++)
                r *= i;
            return r;
        }
    }

    public sealed class TfDataset
    {
        [JsonPropertyName("generator")]
        public string Generator { get; set; } = "";

        [JsonPropertyName("seed")]
        public int Seed { get; set; }

        [JsonPropertyName("dtype")]
        public string DType { get; set; } = "";

        [JsonPropertyName("max_order")]
        public int MaxOrder { get; set; }

        [JsonPropertyName("points_per_function")]
        public int PointsPerFunction { get; set; }

        [JsonPropertyName("functions")]
        public List<TfFunctionRecord> Functions { get; set; } = new();
    }

    public sealed class TfFunctionRecord
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("dimension")]
        public int Dimension { get; set; }

        [JsonPropertyName("expression")]
        public string Expression { get; set; } = "";

        [JsonPropertyName("multiindices")]
        public List<List<int>> MultiIndices { get; set; } = new();

        [JsonPropertyName("points")]
        public List<TfPointRecord> Points { get; set; } = new();
    }

    public sealed class TfPointRecord
    {
        [JsonPropertyName("x")]
        public List<double> X { get; set; } = new();

        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("derivatives")]
        public Dictionary<string, double> Derivatives { get; set; } = new();
    }

    public sealed class OrderRmse
    {
        public int Order { get; set; }
        public int Count { get; set; }
        public double Rmse { get; set; }
        public double MaxAbsError { get; set; }
    }

    public sealed class FunctionRmse
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int Dimension { get; set; }
        public Dictionary<int, OrderRmse> ByOrder { get; set; } = new();
    }

    public sealed class BenchmarkSummary
    {
        public string DatasetPath { get; set; } = "";
        public int MaxOrder { get; set; }
        public Dictionary<int, OrderRmse> GlobalByOrder { get; set; } = new();
        public List<FunctionRmse> ByFunction { get; set; } = new();
    }

    internal sealed class ErrorAccumulator
    {
        public int Count { get; private set; }
        public double SumSq { get; private set; }
        public double MaxAbs { get; private set; }

        public void Add(double error)
        {
            Count++;
            SumSq += error * error;
            double abs = Math.Abs(error);
            if (abs > MaxAbs)
                MaxAbs = abs;
        }

        public OrderRmse ToResult(int order)
        {
            return new OrderRmse
            {
                Order = order,
                Count = Count,
                Rmse = Count == 0 ? 0.0 : Math.Sqrt(SumSq / Count),
                MaxAbsError = MaxAbs
            };
        }
    }

    public static class AlephionBenchmarkRunner
    {
        public static BenchmarkSummary Run(
            string datasetJsonPath,
            string outputSummaryJsonPath,
            IAlephionOps<Alephion> ops,
            int minReportedOrder = 1)
        {
            if (ops == null)
                throw new ArgumentNullException(nameof(ops));
            if (!File.Exists(datasetJsonPath))
                throw new FileNotFoundException("Dataset JSON not found.", datasetJsonPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            TfDataset dataset;
            using (var stream = File.OpenRead(datasetJsonPath))
            {
                dataset = JsonSerializer.Deserialize<TfDataset>(stream, options)
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

            foreach (var func in dataset.Functions)
            {
                var differentiator = new AlephionDifferentiatorDouble(func.Dimension, dataset.MaxOrder);

                var functionAcc = new Dictionary<int, ErrorAccumulator>();
                for (int k = 0; k <= dataset.MaxOrder; k++)
                    functionAcc[k] = new ErrorAccumulator();

                foreach (var point in func.Points)
                {
                    Alephion evaluated = differentiator.Evaluate(
                        vars => BenchmarkFunctionsAlephion.Evaluate(
                            func.Name,
                            vars,
                            ops,
                            differentiator.Plan.RequiredLength
                        ),
                        point.X
                    );

                    foreach (var alpha in func.MultiIndices)
                    {
                        int order = AlephionDerivativePlan.TotalOrder(alpha);
                        if (order < minReportedOrder)
                            continue;

                        string key = string.Join(",", alpha);
                        if (!point.Derivatives.TryGetValue(key, out double exact))
                            throw new InvalidOperationException($"Derivative key '{key}' not found in dataset.");

                        double predicted = differentiator.ExtractDerivative(evaluated, alpha);
                        double error = predicted - exact;

                        functionAcc[order].Add(error);
                        globalAcc[order].Add(error);
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

            for (int k = minReportedOrder; k <= dataset.MaxOrder; k++)
                summary.GlobalByOrder[k] = globalAcc[k].ToResult(k);

            File.WriteAllText(
                outputSummaryJsonPath,
                JsonSerializer.Serialize(summary, options)
            );

            return summary;
        }
    }
}