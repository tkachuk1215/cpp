namespace Core.Dto;

public record ProductDto(
    string Id,
    string Sku,
    string Name,
    string Unit,
    int Quantity,
    string? Note = null);

// Record для складу (додаткове завдання 2)
public record WarehouseDto(
    string Id,
    string Name,
    string Address);