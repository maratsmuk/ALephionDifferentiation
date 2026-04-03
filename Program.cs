using AlephionMath;
using Differentiation;
using Differentiation.Benchmarking;

public static class Program
{
    public static void Main()
    {
        IAlephionOps<Alephion> ops = new AlephionOps(); // твоя реализация

        string datasetPath = "tf_autodiff_derivatives_dataset.json";
        string summaryPath = "exact_derivatives_summary_aligned.json";
        string csvPath = "alephion_full_errors.csv";
        string latexPath = "alephion_rmse_table.tex";
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var summary = AlephionBenchmarkRunnerExtended.RunWithReports(
            datasetJsonPath: datasetPath,
            outputSummaryJsonPath: summaryPath,
            outputCsvPath: csvPath,
            outputLatexPath: latexPath,
            ops: ops,
            minReportedOrder: 1
        );
        watch.Stop();
        var elapsedSeconds = watch.ElapsedMilliseconds;
        Console.WriteLine($"Elapsed: {elapsedSeconds} ms");
        Console.WriteLine("Global RMSE by order:");
        foreach (var kv in summary.GlobalByOrder.OrderBy(x => x.Key))
        {
            Console.WriteLine(
                $"order={kv.Key}, count={kv.Value.Count}, rmse={kv.Value.Rmse:E16}, maxAbs={kv.Value.MaxAbsError:E16}"
            );
        }

        Console.WriteLine($"Saved summary JSON : {summaryPath}");
        Console.WriteLine($"Saved full CSV     : {csvPath}");
        Console.WriteLine($"Saved LaTeX table  : {latexPath}");
    }
    public static void MainExample()
    {
        double x = 3.0;
        double y = 1.0;
        var func = new ExampleFunction();
        var exact_derivs = new List<double>() {
            func.F(x, y) ,
            func.Fx(x, y), func.Fy(x, y),
            func.Fxx(x, y), func.Fxy(x, y), func.Fyy(x, y),
            func.Fxxx(x, y), func.Fxxy(x, y), func.Fxyy(x, y), func.Fyyy(x, y)
        };

        var derivs = new AlephionDifferentiationSimple().Differentiate(func, x, y);
        var derivs_names = new List<string>()
        {
            $"f({x},{y})    = ",
            $"fx({x},{y})   = ",
            $"fy({x},{y})   = ",
            $"fxx({x},{y})  = ",
            $"fxy({x},{y})  = ",
            $"fyy({x},{y})  = ",
            $"fxxx({x},{y}) = ",
            $"fxxy({x},{y}) = ",
            $"fxyy({x},{y}) = ",
            $"fyyy({x},{y}) = ",
        };

        for (int i = 0; i < derivs.Count; i++)
        {
            Console.WriteLine($"$${derivs_names[i]} {derivs[i]},$$");
        }
    }

}