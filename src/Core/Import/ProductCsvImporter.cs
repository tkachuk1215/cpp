using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

public static class ProductCsvImporter
{
    // Роздільник — крапка з комою: не конфліктує з комою в назвах товарів.
    private const char Separator = ';';

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        string[] lines;

        try
        {
            lines = File.ReadAllLines(path, Encoding.UTF8);
        }
        catch (IOException ex)
        {
            return new ImportResult<ProductDto>(items, [$"Помилка читання CSV: {ex.Message}"]);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ImportResult<ProductDto>(items, [$"Немає доступу до CSV: {ex.Message}"]);
        }

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNumber = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            string[] headerParts = line.Split(Separator, StringSplitOptions.TrimEntries);

            if (headerParts.Length >= 2 &&
                headerParts[0].Equals("id", StringComparison.OrdinalIgnoreCase) &&
                headerParts[1].Equals("sku", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (ParseLine(line))
            {
                case ParseProductOk ok:
                    items.Add(ok.Value);
                    break;

                case ParseFailed failed:
                    errors.Add($"рядок {lineNumber}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    // Додаткове завдання 2: розпізнавання різнорідних рядків (P та W)
    public static (IReadOnlyList<ProductDto> Products, IReadOnlyList<WarehouseDto> Warehouses, IReadOnlyList<string> Errors) LoadHeterogeneous(string path)
    {
        var products = new List<ProductDto>();
        var warehouses = new List<WarehouseDto>();
        var errors = new List<string>();

        string[] lines;

        try
        {
            lines = File.ReadAllLines(path, Encoding.UTF8);
        }
        catch (IOException ex)
        {
            errors.Add($"Помилка читання гетерогенного файлу: {ex.Message}");
            return (products, warehouses, errors);
        }
        catch (UnauthorizedAccessException ex)
        {
            errors.Add($"Немає доступу до гетерогенного файлу: {ex.Message}");
            return (products, warehouses, errors);
        }

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNumber = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                continue;

            if (lineNumber == 1 && line.StartsWith("type", StringComparison.OrdinalIgnoreCase))
                continue;

            switch (ParseHeterogeneousLine(line))
            {
                case ParseProductOk pOk:
                    products.Add(pOk.Value);
                    break;
                case ParseWarehouseOk wOk:
                    warehouses.Add(wOk.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {lineNumber}: {failed.Reason}");
                    break;
            }
        }

        return (products, warehouses, errors);
    }

    // Звичайний CSV: рядок -> колонки -> спільна валідація товару
    private static ParseOutcome ParseLine(string line) =>
        ParseProduct(line.Split(Separator, StringSplitOptions.TrimEntries));

    // Єдине місце з правилами валідації товару.
    // Використовується і звичайним, і гетерогенним режимом.
    private static ParseOutcome ParseProduct(string[] parts) => parts switch
    {
        { Length: < 5 } => new ParseFailed($"очікую 5 колонок, отримав {parts.Length}"),
        { Length: > 5 } => new ParseFailed($"забагато колонок: {parts.Length}, очікую 5"),
        [var id, _, _, _, _] when string.IsNullOrWhiteSpace(id) => new ParseFailed("ID не може бути порожнім"),
        [_, var sku, _, _, _] when string.IsNullOrWhiteSpace(sku) => new ParseFailed("SKU не може бути порожнім"),
        [_, _, var name, _, _] when string.IsNullOrWhiteSpace(name) => new ParseFailed("Назва товару не може бути порожньою"),
        [_, _, _, var unit, _] when string.IsNullOrWhiteSpace(unit) => new ParseFailed("Одиниця виміру не може бути порожньою"),
        [_, _, _, _, var qty] when !int.TryParse(qty, NumberStyles.Integer, CultureInfo.InvariantCulture, out int quantity) || quantity < 0 =>
            new ParseFailed($"кількість '{qty}' має бути невід'ємним цілим числом"),
        [var id, var sku, var name, var unit, var qty] =>
            new ParseProductOk(new ProductDto(id, sku, name, unit, int.Parse(qty, CultureInfo.InvariantCulture))),
        _ => new ParseFailed("невідомий формат рядка")
    };

    // Гетерогенний CSV: перша колонка — префікс типу запису
    private static ParseOutcome ParseHeterogeneousLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            // P;id;sku;name;unit;qty -> відкидаємо префікс і віддаємо решту спільній валідації
            ["P", .. var rest] => ParseProduct(rest),

            // W;id;name;address
            ["W", var id, var name, var address]
                when string.IsNullOrWhiteSpace(id)
                  || string.IsNullOrWhiteSpace(name)
                  || string.IsNullOrWhiteSpace(address)
                => new ParseFailed("id, назва або адреса складу порожні"),
            ["W", var id, var name, var address]
                => new ParseWarehouseOk(new WarehouseDto(id, name, address)),
            ["W", ..]
                => new ParseFailed($"склад: очікую 4 колонки, отримав {parts.Length}"),

            _ => new ParseFailed($"невідомий префікс: '{parts[0]}'")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseProductOk(ProductDto Value) : ParseOutcome;
    private sealed record ParseWarehouseOk(WarehouseDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}