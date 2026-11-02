using Core.Dto;

namespace Core.Domain;

public sealed class Product
{
    private int _quantity;

    public string Id { get; }
    public string Sku { get; }
    public string Name { get; }
    public string Unit { get; }
    public string? Note { get; }
    public int Quantity => _quantity;
    public ProductStatus Status { get; private set; }

    // Приватний: створити товар можна лише через Create. Перевірок тут немає.
    private Product(string id, string sku, string name, string unit, int quantity,
        string? note, ProductStatus status)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Unit = unit;
        _quantity = quantity;
        Note = note;
        Status = status;
    }

    // Єдиний спосіб створити товар: усі перевірки тут, до new.
    public static Product Create(string id, string sku, string name, string unit, int quantity,
        string? note = null, ProductStatus status = ProductStatus.Active)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU не може бути порожнім", nameof(sku));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва не може бути порожньою", nameof(name));
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Одиниця виміру не може бути порожньою", nameof(unit));
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity,
                "Початковий залишок не може бути від'ємним");
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status), status,
                "Невідомий статус товару");

        string normalizedSku = sku.Trim().ToUpperInvariant();

        if (status == ProductStatus.Archived && quantity != 0)
            throw new ArgumentException(
                $"Архівний товар {normalizedSku} не може мати залишок: {quantity}", nameof(quantity));

        return new Product(id.Trim(), normalizedSku, name.Trim(), unit.Trim(), quantity,
            string.IsNullOrWhiteSpace(note) ? null : note.Trim(), status);
    }

    public void RegisterArrival(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Кількість приходу має бути більшою за нуль");
        if (Status != ProductStatus.Active)
            throw new InvalidOperationException(
                $"Не можна оприбуткувати {amount}: товар {Sku} має статус {Status}");

        _quantity += amount;
    }

    public void Issue(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Кількість видачі має бути більшою за нуль");
        if (Status == ProductStatus.Archived)
            throw new InvalidOperationException(
                $"Не можна видати {amount}: товар {Sku} заархівований");
        if (amount > _quantity)
            throw new InvalidOperationException(
                $"Не можна видати {amount}: залишок {Sku} = {_quantity}");

        _quantity -= amount;
    }

    // Зміна статусу: бізнес-назви, а допустимі переходи перевіряє IsTransitionAllowed.
    public void Discontinue()
    {
        EnsureCanMoveTo(ProductStatus.Discontinued);
        Status = ProductStatus.Discontinued;
    }

    public void Reactivate()
    {
        EnsureCanMoveTo(ProductStatus.Active);
        Status = ProductStatus.Active;
    }

    public void Archive()
    {
        EnsureCanMoveTo(ProductStatus.Archived);
        if (_quantity > 0)
            throw new InvalidOperationException(
                $"Не можна архівувати {Sku}: залишок {_quantity}, потрібно 0");
        Status = ProductStatus.Archived;
    }

    private void EnsureCanMoveTo(ProductStatus target)
    {
        if (!IsTransitionAllowed(Status, target))
            throw new InvalidOperationException(
                $"Недопустимий перехід статусу товару {Sku}: {Status} → {target}");
    }

    private static bool IsTransitionAllowed(ProductStatus from, ProductStatus to) => (from, to) switch
    {
        (ProductStatus.Active, ProductStatus.Discontinued) => true,
        (ProductStatus.Discontinued, ProductStatus.Active) => true,
        (ProductStatus.Discontinued, ProductStatus.Archived) => true,
        _ => false
    };

    // Мапінг у формат тижня 3 і назад — знадобиться сховищу тижня 5.
    public ProductDto ToDto() => new(Id, Sku, Name, Unit, Quantity, Note, Status.ToString());

    // Іде через Create, тому ті самі інваріанти, що й під час першого створення.
    public static Product FromDto(ProductDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return Create(dto.Id, dto.Sku, dto.Name, dto.Unit, dto.Quantity, dto.Note,
            ParseStatus(dto.Status));
    }

    private static ProductStatus ParseStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return ProductStatus.Active;

        if (Enum.TryParse<ProductStatus>(raw.Trim(), ignoreCase: true, out var status)
            && Enum.IsDefined(status))
            return status;

        throw new ArgumentException($"Невідомий статус товару '{raw}'", nameof(Status));
    }

    public override string ToString() => $"{Id} [{Sku}] {Name} — {Quantity} {Unit}";
    
}