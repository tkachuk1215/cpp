using System.Text;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        string json;

        try
        {
            json = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (IOException ex)
        {
            errors.Add($"Помилка читання JSON-файлу: {ex.Message}");
            return new ImportResult<ProductDto>(items, errors);
        }
        catch (UnauthorizedAccessException ex)
        {
            errors.Add($"Немає доступу до JSON-файлу: {ex.Message}");
            return new ImportResult<ProductDto>(items, errors);
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            errors.Add($"Некоректний формат JSON: {ex.Message}");
            return new ImportResult<ProductDto>(items, errors);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add("JSON повинен містити масив товарів.");
                return new ImportResult<ProductDto>(items, errors);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            int index = 0;

            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                index++;

                if (element.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"об'єкт JSON #{index}: очікується об'єкт товару");
                    continue;
                }

                try
                {
                    ProductDto? product = element.Deserialize<ProductDto>(options);

                    if (product is null)
                    {
                        errors.Add($"об'єкт JSON #{index}: не вдалося прочитати товар");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(product.Id))
                    {
                        errors.Add($"об'єкт JSON #{index}: ID не може бути порожнім");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(product.Sku))
                    {
                        errors.Add($"об'єкт JSON #{index}: SKU не може бути порожнім");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(product.Name))
                    {
                        errors.Add($"об'єкт JSON #{index}: назва не може бути порожньою");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(product.Unit))
                    {
                        errors.Add($"об'єкт JSON #{index}: одиниця виміру не може бути порожньою");
                        continue;
                    }

                    if (product.Quantity < 0)
                    {
                        errors.Add($"об'єкт JSON #{index}: кількість не може бути від'ємною");
                        continue;
                    }

                    items.Add(product);
                }
                catch (JsonException ex)
                {
                    errors.Add($"об'єкт JSON #{index}: помилка формату — {ex.Message}");
                }
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }
}