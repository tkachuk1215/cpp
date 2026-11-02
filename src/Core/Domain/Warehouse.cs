using Core.Dto;

namespace Core.Domain;

// Друга сутність (WarehouseDto з лаб. 3 -> Warehouse).
public sealed class Warehouse
{
    public string Id { get; }
    public string Name { get; }
    public string Address { get; private set; }

    private Warehouse(string id, string name, string address)
    {
        Id = id;
        Name = name;
        Address = address;
    }

    public static Warehouse Create(string id, string name, string address)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор складу обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва складу не може бути порожньою", nameof(name));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Адреса складу не може бути порожньою", nameof(address));

        return new Warehouse(id.Trim(), name.Trim(), address.Trim());
    }

    public void ChangeAddress(string newAddress)
    {
        if (string.IsNullOrWhiteSpace(newAddress))
            throw new ArgumentException("Адреса складу не може бути порожньою", nameof(newAddress));

        Address = newAddress.Trim();
    }

    public WarehouseDto ToDto() => new(Id, Name, Address);

    public static Warehouse FromDto(WarehouseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return Create(dto.Id, dto.Name, dto.Address);
    }

    public override string ToString() => $"{Id} {Name} ({Address})";
}
