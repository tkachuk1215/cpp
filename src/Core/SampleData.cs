using Core.Domain;
using Core.Reports;

namespace Core;

public static class SampleData
{
    public static IEnumerable<Product> Products() =>
    [
        Product.Create("s0000001", "SKU-001", "Мишка USB", "шт", 40),
        Product.Create("s0000002", "SKU-002", "Клавіатура механічна", "шт", 25),
        Product.Create("s0000003", "SKU-003", "Монітор 24\"", "шт", 12),
        Product.Create("s0000004", "SKU-004", "Кабель HDMI 2м", "шт", 80),
        Product.Create("s0000005", "SKU-005", "Цемент М400 25кг", "шт", 100),
        Product.Create("s0000006", "SKU-006", "Пісок будівельний", "т", 30),
        Product.Create("s0000007", "SKU-007", "Щебінь фракція 5-20", "т", 45),
        Product.Create("s0000008", "SKU-008", "Цегла червона", "шт", 5000),
        Product.Create("s0000009", "SKU-009", "Профіль CD 60", "м", 600),
        Product.Create("s0000010", "SKU-010", "Гіпсокартон 12.5мм", "шт", 150),
        Product.Create("s0000011", "SKU-011", "Фарба біла 10л", "шт", 35),
        Product.Create("s0000012", "SKU-012", "Шпаклівка 25кг", "шт", 60),
        Product.Create("s0000013", "SKU-013", "Дриль акумуляторний", "шт", 8),
        Product.Create("s0000014", "SKU-014", "Старий роутер", "шт", 3, "Знято з виробництва",
            ProductStatus.Discontinued),
        Product.Create("s0000015", "SKU-015", "Застарілий кабель VGA", "шт", 0, null,
            ProductStatus.Archived),
        Product.Create("s0000016", "SKU-016", "Рукавиці робочі", "пар", 200),
        Product.Create("s0000017", "SKU-017", "Кабель ВВГ 3x1.5", "м", 250),
        Product.Create("s0000018", "SKU-018", "Шнур живлення", "м", 9),
        Product.Create("s0000019", "SKU-019", "Саморізи 4x40", "кг", 45),
        Product.Create("s0000020", "SKU-020", "Цвяхи 100мм", "кг", 7),
    ];

    // Переміщення для Join/GroupJoin. Останній запис сирота: товару s0000099 немає.
    public static IReadOnlyList<StockMovement> Movements() =>
    [
        new("s0000001", 40), new("s0000001", -5),
        new("s0000004", 80), new("s0000004", -20), new("s0000004", -10),
        new("s0000008", 5000), new("s0000008", -300),
        new("s0000013", 10), new("s0000013", -2),
        new("s0000016", 200),
        new("s0000099", 1),
    ];

    // Детермінований набір для вимірювання часу (Random з фіксованим seed)
    public static IReadOnlyList<Product> Generate(int count)
    {
        string[] units = ["шт", "м", "кг", "т", "пар"];
        var rnd = new Random(42);
        var list = new List<Product>(count);
        for (int i = 0; i < count; i++)
            list.Add(Product.Create($"g{i:D7}", $"GEN-{i:D7}", $"Товар {i}",
                units[i % units.Length], rnd.Next(0, 1000)));
        return list;
    }
}