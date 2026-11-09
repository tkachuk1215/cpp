using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingCatalogStore(ICatalogStore inner) : ICatalogStore
{
    private readonly ICatalogStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Product>? _list;

    public IReadOnlyList<Product> List() => _list ??= _inner.List();

    public override string ToString() => $"CachingCatalogStore({_inner.GetType().Name})";

    public Product? GetById(string id)
    {
        if (_cache.TryGetValue(id, out var cached)) return cached;
        var found = _inner.GetById(id);
        if (found is not null) _cache[found.Id] = found;
        return found;
    }

    public void Add(Product item)
    {
        _inner.Add(item);
        _cache[item.Id] = item;
        _list = null;
    }

    public void Update(Product item)
    {
        _inner.Update(item);
        _cache[item.Id] = item;
        _list = null;
    }

    public bool Remove(string id)
    {
        var removed = _inner.Remove(id);
        if (removed) { _cache.Remove(id); _list = null; }
        return removed;
    }
}
