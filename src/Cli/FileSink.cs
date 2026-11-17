using Core.Events;
using Core.Notifications;

namespace Cli;

internal sealed class FileSink(string path) : INotificationSink
{
    public void Notify(CatalogChangedEventArgs e)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path,
            $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} → залишок {e.QuantityAfter}{Environment.NewLine}");
    }
}
