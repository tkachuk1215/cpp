using Core.Events;

namespace Core.Notifications;

public interface INotificationSink
{
    void Notify(CatalogChangedEventArgs e);
}
