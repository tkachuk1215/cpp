using Core.Events;
using Core.Notifications;

namespace Cli;

internal sealed class ConsoleSink : INotificationSink
{
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public void Notify(CatalogChangedEventArgs e) =>
        _lines.Add($"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} → залишок {e.QuantityAfter}");
}