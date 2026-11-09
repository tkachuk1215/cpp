using Cli;
using Core.Abstractions;
using Core.Services;

// Composition root: фабрика створює конкретні класи
ICatalogStore store = StoreFactory.Create(args);
var service = new CatalogService(store);

Console.WriteLine($"Сховище: {store}");

var created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
service.Receive(created.Id, 25);
service.Issue(created.Id, 10);

var found = service.Find(created.Id);
Console.WriteLine($"Знайдено: {found?.Name}, залишок {found?.Quantity}");

foreach (var p in service.All())
    Console.WriteLine($" {p.Id} {p.Sku,-10} {p.Name,-22} {p.Quantity,5} {p.Unit}");

// Додаткове завдання 2: пошук за предикатом
Console.WriteLine();
Console.WriteLine("Товари із залишком менше 20:");
foreach (var p in service.Find(p => p.Quantity < 20))
    Console.WriteLine($"  {p.Sku,-10} {p.Name,-22} {p.Quantity,5}");

// Сценарії відмови
Console.WriteLine();
TryRun("Receive з неіснуючим id", () => service.Receive("no-such-id", 5));
TryRun("дубль id", () => store.Add(created));
TryRun("видача більша за залишок", () => service.Issue(created.Id, 1000));
TryRun("прихід від'ємної кількості", () => service.Receive(created.Id, -3));

return 0;

static void TryRun(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював!");
    }
    catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
    {
        Console.WriteLine($"  Помилка ({title}): {ex.Message.Split('\n')[0]}");
    }
}