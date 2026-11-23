using System.Diagnostics;
using Core;
using Core.Reports;

namespace Cli;

internal static class Benchmark
{
    public static void Run(int count = 100_000)
    {
        var data = SampleData.Generate(count);
        Console.WriteLine($"Записів: {data.Count}, серія з 5 запусків, середній час:");

        Measure("LowStock: без проміжних ToList", () => ReportService.LowStock(data, 500));
        Measure("LowStock: ToList після Where і OrderBy", () => ReportService.LowStockMaterialized(data, 500));
        Measure("TopByQuantity (TopBy, n=5)", () => ReportService.TopByQuantity(data, 5));
        Measure("ByUnit (методний синтаксис)", () => ReportService.ByUnit(data));
        Measure("ByUnitQuery (синтаксис запитів)", () => ReportService.ByUnitQuery(data));
    }

    private static void Measure<T>(string title, Func<IReadOnlyList<T>> action)
    {
        action();                                   // прогрів JIT
        const int runs = 5;
        int rows = 0;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < runs; i++) rows = action().Count;
        sw.Stop();
        Console.WriteLine($"  {title,-42} {sw.Elapsed.TotalMilliseconds / runs,8:F1} мс (рядків: {rows})");
    }
}
