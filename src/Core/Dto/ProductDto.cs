namespace Core.Dto;

// Лаб. 4: додано необов'язкове поле Status (рядок), щоб імпорт з лаб. 3 не зламався.
// DTO не залежить від Core.Domain, тому статус тут — текст, а перетворення на enum робить Product.FromDto.
public record ProductDto(
    string Id,
    string Sku,
    string Name,
    string Unit,
    int Quantity,
    string? Note = null,
    string? Status = null);

// Record для складу (додаткове завдання 2)
public record WarehouseDto(
    string Id,
    string Name,
    string Address);