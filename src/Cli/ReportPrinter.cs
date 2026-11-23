using Core.Reports;

namespace Cli;

internal static class ReportPrinter
{
    public static void Print(IReadOnlyList<StockLine> lines)
    {
        if (lines.Count == 0) { Console.WriteLine("  (порожньо)"); return; }
        foreach (var l in lines)
            Console.WriteLine($" {l.Sku,-10} {l.Name,-24} {l.Quantity,6} {l.Unit}");
    }

    public static void Print(IReadOnlyList<GroupSummary> groups)
    {
        if (groups.Count == 0) { Console.WriteLine("  (порожньо)"); return; }
        foreach (var g in groups)
            Console.WriteLine($" {g.Key,-6} позицій: {g.Count,3} сумарно: {g.TotalQuantity,6}");
    }

    public static void Print((int Items, int Total, double Average, int Max) t) =>
        Console.WriteLine(
            $" позицій {t.Items}, всього {t.Total}, середнє {t.Average:F2}, макс {t.Max}");

    public static void Print(IReadOnlyList<MovementLine> lines)
    {
        if (lines.Count == 0) { Console.WriteLine("  (порожньо)"); return; }
        foreach (var l in lines)
            Console.WriteLine($" {l.Sku,-10} {l.Delta,6:+0;-0}");
    }

    public static void Print(IReadOnlyList<MovementSummary> rows)
    {
        if (rows.Count == 0) { Console.WriteLine("  (порожньо)"); return; }
        foreach (var s in rows)
            Console.WriteLine(
                $" {s.Sku,-10} {s.Name,-24} рухів: {s.Movements,2} разом: {s.NetDelta,6:+0;-0;0}");
    }
}
