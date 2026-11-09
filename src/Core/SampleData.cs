using Core.Domain;

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
    ];
}
