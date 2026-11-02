using Core.Domain;
using Core.Dto;

namespace Core.Import;

// Додаткове завдання 1: ImportResult<ProductDto> -> сутності + рядки, що не пройшли інваріанти.
// Окремий клас, бо метод однаково працює і для CSV, і для JSON.
public static class ProductImportMapper
{
    public static (IReadOnlyList<Product> Products, IReadOnlyList<string> Rejected) ToEntities(
        ImportResult<ProductDto> imported)
    {
        ArgumentNullException.ThrowIfNull(imported);

        var products = new List<Product>();
        var rejected = new List<string>();

        foreach (ProductDto dto in imported.Items)
        {
            try
            {
                products.Add(Product.FromDto(dto));
            }
            catch (ArgumentException ex)
            {
                rejected.Add($"{dto.Id}: {ex.Message.ReplaceLineEndings(" ")}");
            }
        }

        return (products, rejected);
    }
}
