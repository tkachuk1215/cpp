using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    private static StockLine ToLine(Product p) => new(p.Sku, p.Name, p.Quantity, p.Unit);

    // 1) фільтр + сортування + проєкція
    public static IReadOnlyList<StockLine> LowStock(IEnumerable<Product> source, int threshold) =>
        source.Where(p => p.Quantity < threshold)
              .OrderBy(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal)
              .Select(ToLine)
              .ToList();

    // 2) топ-N (використовує TopBy: другий ключ усередині розширення)
    public static IReadOnlyList<StockLine> TopByQuantity(IEnumerable<Product> source, int n) =>
        source.TopBy(p => p.Quantity, n, p => p.Sku)
              .Select(ToLine)
              .ToList();

    // 3) групування з підрахунком і сумою (методний синтаксис)
    public static IReadOnlyList<GroupSummary> ByUnit(IEnumerable<Product> source) =>
        source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
              .OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.Key, StringComparer.Ordinal)
              .ToList();

    // 3б) той самий звіт у синтаксисі запитів (додаткове 3)
    public static IReadOnlyList<GroupSummary> ByUnitQuery(IEnumerable<Product> source)
    {
        var query =
            from product in source
            group product by product.Unit.ToLowerInvariant() into g
            orderby g.Sum(p => p.Quantity) descending, g.Key
            select new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity));
        return query.ToList();
    }

    // 4) агрегати з обробкою порожньої послідовності
    public static (int Items, int Total, double Average, int Max) Totals(IEnumerable<Product> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0, 0d, 0)
            : (list.Count, list.Sum(p => p.Quantity),
               list.Average(p => p.Quantity), list.Max(p => p.Quantity));
    }

    // 5) Join: лише товари, що мають переміщення (додаткове 1)
    public static IReadOnlyList<MovementLine> MovementLines(
        IEnumerable<Product> products, IEnumerable<StockMovement> movements) =>
        products.Join(movements,
                      product => product.Id,
                      movement => movement.ProductId,
                      (product, movement) => new MovementLine(product.Sku, movement.Delta))
                .OrderBy(l => l.Sku, StringComparer.Ordinal).ThenBy(l => l.Delta)
                .ToList();

    // 6) GroupJoin: усі товари, навіть без переміщень (додаткове 1)
    public static IReadOnlyList<MovementSummary> MovementSummaries(
        IEnumerable<Product> products, IEnumerable<StockMovement> movements) =>
        products.GroupJoin(movements,
                           product => product.Id,
                           movement => movement.ProductId,
                           (product, ms) => new MovementSummary(
                               product.Sku, product.Name, ms.Count(), ms.Sum(m => m.Delta)))
                .OrderBy(s => s.Sku, StringComparer.Ordinal)
                .ToList();

    // 7) топ за кількістю переміщень: другий звіт з TopBy (додаткове 2)
    public static IReadOnlyList<MovementSummary> TopMoved(
        IEnumerable<Product> products, IEnumerable<StockMovement> movements, int n) =>
        MovementSummaries(products, movements)
            .TopBy(s => s.Movements, n, s => s.Sku)
            .ToList();

    // Для порівняння швидкості (додаткове 4): ToList після кожного кроку
    public static IReadOnlyList<StockLine> LowStockMaterialized(
        IEnumerable<Product> source, int threshold)
    {
        var filtered = source.Where(p => p.Quantity < threshold).ToList();
        var sorted = filtered.OrderBy(p => p.Quantity)
                             .ThenBy(p => p.Sku, StringComparer.Ordinal).ToList();
        return sorted.Select(ToLine).ToList();
    }
}
