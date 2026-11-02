namespace Core.Domain;

// Додаткове завдання 2: правило, що охоплює ДВІ сутності (склад і товар) та інші товари складу.
// Сама сутність цього знати не може: їй потрібен список уже розміщених товарів, а його дає
// сховище. Тому правило винесене окремо; на тижні 5 його викликатиме CatalogService.
public static class WarehousePlacementPolicy
{
    public const int MaxDistinctProducts = 5;

    public static void EnsureCanPlace(
        Warehouse warehouse, Product product, IReadOnlyCollection<Product> alreadyPlaced)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(alreadyPlaced);

        if (product.Status == ProductStatus.Archived)
            throw new InvalidOperationException(
                $"Не можна розмістити {product.Sku} на складі {warehouse.Id}: товар заархівований");

        if (alreadyPlaced.Any(p => p.Sku == product.Sku))
            throw new InvalidOperationException(
                $"Товар {product.Sku} уже розміщений на складі {warehouse.Id}");

        if (alreadyPlaced.Count >= MaxDistinctProducts)
            throw new InvalidOperationException(
                $"Склад {warehouse.Id} уже містить {alreadyPlaced.Count} різних товарів " +
                $"(максимум {MaxDistinctProducts}), додати {product.Sku} не можна");
    }
}
