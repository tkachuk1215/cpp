using Core.Dto;
using Core.Import;

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.Error.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

// Гетерогенний файл (додаткове завдання 2)
if (Path.GetFileName(path).Equals("heterogeneous.csv", StringComparison.OrdinalIgnoreCase))
{
    var (products, warehouses, errors) = ProductCsvImporter.LoadHeterogeneous(path);

    if (products.Count > 0)
    {
        Console.WriteLine($"Завантажено товарів: {products.Count}");
        foreach (var p in products)
            Console.WriteLine($"  [P] {p.Id,-6} {p.Sku,-10} {p.Name,-22} {p.Quantity,5} {p.Unit}");
    }

    if (warehouses.Count > 0)
    {
        Console.WriteLine($"Завантажено складів: {warehouses.Count}");
        foreach (var w in warehouses)
            Console.WriteLine($"  [W] {w.Id,-6} {w.Name,-22} {w.Address}");
    }

    if (errors.Count > 0)
    {
        Console.WriteLine($"Пропущено записів: {errors.Count}");
        foreach (var e in errors)
            Console.WriteLine($" ! {e}");
    }

    var mixedStats = new ImportStats(products.Count + warehouses.Count, errors.Count);

    Console.WriteLine();
    Console.WriteLine(
        $"[Статистика] {mixedStats} (Товарів: {products.Count}, Складів: {warehouses.Count})");

    return 0;
}

// Звичайний CSV або JSON (додаткове завдання 1)
string extension = Path.GetExtension(path).ToLowerInvariant();

if (extension is not ".csv" and not ".json")
{
    Console.Error.WriteLine($"Непідтримуваний формат файлу: {extension}");
    Console.Error.WriteLine("Підтримувані формати: .csv, .json");
    return 1;
}

ImportResult<ProductDto> result = extension switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
    _ => throw new InvalidOperationException("Невідомий формат")
};

Console.WriteLine($"Завантажено записів: {result.Items.Count}");

foreach (ProductDto p in result.Items.Take(5))
{
    Console.WriteLine(
        $" {p.Id,-6} {p.Sku,-10} {p.Name,-26} {p.Quantity,5} {p.Unit}");
}

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено записів: {result.Errors.Count}");

    foreach (string error in result.Errors)
        Console.WriteLine($" ! {error}");
}

var stats = new ImportStats(result.Items.Count, result.Errors.Count);

Console.WriteLine();
Console.WriteLine($"[Статистика] {stats}");

return 0;