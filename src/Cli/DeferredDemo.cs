using Core.Domain;

namespace Cli;

internal static class DeferredDemo
{
    public static void Run(IEnumerable<Product> all)
    {
        var source = all.ToList();
        var query = source.Where(p => p.Quantity < 10);   // запит побудовано, не виконано

        Console.WriteLine($"Дефіцитних до змін: {query.Count()}");
        source.Add(Product.Create("tmp00001", "SKU-999", "Скоба", "шт", 3));
        Console.WriteLine($"Дефіцитних після змін: {query.Count()}");

        var snapshot = query.ToList();                    // результат зафіксовано
        source.Clear();
        Console.WriteLine($"У знімку лишилось: {snapshot.Count}");
        Console.WriteLine($"А запит після Clear дає: {query.Count()}");
    }
}
