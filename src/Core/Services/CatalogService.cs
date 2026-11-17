using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Notifications;

namespace Core.Services;

public sealed class CatalogService(ICatalogStore store, IEnumerable<INotificationSink>? sinks = null)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly List<INotificationSink> _sinks = [.. sinks ?? []];

    public event EventHandler<CatalogChangedEventArgs>? Changed;

    private void OnChanged(Product p, CatalogChangeKind kind, int delta)
    {
        var args = new CatalogChangedEventArgs(p.Id, kind, delta, p.Quantity);
        Changed?.Invoke(this, args);
        foreach (var sink in _sinks) sink.Notify(args);
    }

    public Product Add(string sku, string name, string unit, int quantity)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        _store.Add(product);
        OnChanged(product, CatalogChangeKind.Added, quantity);
        return product;
    }

    public void Receive(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");
        product.RegisterArrival(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    public void Issue(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");
        product.Issue(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Issued, -quantity);
    }

    public bool Remove(string id)
    {
        var product = _store.GetById(id);
        if (product is null || !_store.Remove(id)) return false;
        OnChanged(product, CatalogChangeKind.Removed, -product.Quantity);
        return true;
    }

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Product> FindAll(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    public int ImportLines(IEnumerable<string> lines, Action<string> onSkipped)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(onSkipped);

        int imported = 0;
        foreach (var line in lines)
        {
            var parts = line.Split(';', StringSplitOptions.TrimEntries);
            if (parts.Length != 4 || !int.TryParse(parts[3], out var qty))
            {
                onSkipped($"Пропущено: '{line}'");
                continue;
            }

            try
            {
                Add(parts[0], parts[1], parts[2], qty);
                imported++;
            }
            catch (ArgumentException ex)
            {
                onSkipped($"Відхилено доменом: '{line}' ({ex.Message.Split('\n')[0]})");
            }
        }
        return imported;
    }
}