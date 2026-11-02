using Core.Domain;
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

// ===== Лабораторна 4: доменна модель =====
Console.WriteLine();
RunDomainDemo(result);

return 0;

static void RunDomainDemo(ImportResult<ProductDto> imported)
{
    // --- Доп. 1: DTO з імпорту -> сутності + відхилені доменом ---
    Console.WriteLine("=== Імпорт -> сутності (доп. 1) ===");

    // До справжніх DTO додаємо два, яких імпортер не ловить (наприклад, "status" з JSON).
    var extra = new ProductDto[]
    {
        new("P-900", "SKU-900", "Тестовий товар", "шт", 5, null, "Невідомий"),
        new("P-901", "SKU-901", "Архівний із залишком", "шт", 7, null, "Archived")
    };
    var batch = new ImportResult<ProductDto>([.. imported.Items, .. extra], imported.Errors);

    var (entities, rejected) = ProductImportMapper.ToEntities(batch);
    Console.WriteLine($"  Сутностей: {entities.Count}, відхилено доменом: {rejected.Count}");
    foreach (string reason in rejected)
        Console.WriteLine($"  ! {reason}");
    Console.WriteLine();

    // --- Сценарій 1: успіх ---
    Console.WriteLine("=== Сценарій 1: успіх ===");
    Product product = Product.Create("P-001", "sku-001", "Цемент М400 25кг", "шт", 100);
    Console.WriteLine(product);
    product.RegisterArrival(50);
    product.Issue(30);
    Console.WriteLine(product);

    Product legacy = Product.Create("P-050", "sku-050", "Старий профіль", "м", 0);
    legacy.Discontinue();
    legacy.Archive();
    Console.WriteLine($"{legacy}, статус: {legacy.Status}");

    Warehouse hub = Warehouse.Create("W-01", "Центральний склад", "м. Київ, вул. Полова 12");
    var placed = new List<Product>();
    for (int i = 1; i <= WarehousePlacementPolicy.MaxDistinctProducts; i++)
    {
        Product item = Product.Create($"P-10{i}", $"sku-10{i}", $"Товар {i}", "шт", 1);
        WarehousePlacementPolicy.EnsureCanPlace(hub, item, placed);
        placed.Add(item);
    }
    Console.WriteLine($"{hub}: розміщено {placed.Count} з {WarehousePlacementPolicy.MaxDistinctProducts}");
    Console.WriteLine();

    // --- Сценарій 2: порушення інваріантів ---
    Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
    TryDo("видача більша за залишок", () => product.Issue(1000));
    TryDo("видача нуля", () => product.Issue(0));
    TryDo("прихід від'ємний", () => product.RegisterArrival(-3));
    TryDo("порожній SKU", () => Product.Create("P-002", " ", "Пісок", "т", 10));
    TryDo("від'ємний залишок", () => Product.Create("P-003", "SKU-003", "Цегла", "шт", -5));
    TryDo("FromDto з некоректним DTO",
        () => Product.FromDto(new ProductDto("P-004", "SKU-004", "Щебінь", "т", -1)));
    TryDo("архівація з активного статусу", () => product.Archive());
    TryDo("прихід на архівний товар", () => legacy.RegisterArrival(5));
    TryDo("відновлення архівного товару", () => legacy.Reactivate());
    TryDo("порожня адреса складу", () => Warehouse.Create("W-02", "Хаб", " "));
    TryDo("шостий товар на складі", () => WarehousePlacementPolicy.EnsureCanPlace(
        hub, Product.Create("P-200", "SKU-200", "Зайвий", "шт", 1), placed));
    TryDo("архівний товар на склад", () => WarehousePlacementPolicy.EnsureCanPlace(
        hub, legacy, []));

    Console.WriteLine();
    Console.WriteLine($"Стан після усіх відмов: {product}"); // має лишитися 120
}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}