using Cli;
using Core.Abstractions;
using Core.Events;
using Core.Services;

ICatalogStore store = StoreFactory.Create(args);

var consoleSink = new ConsoleSink();
var sinkLog = Path.Combine(AppContext.BaseDirectory, "logs", "sink.log");
var service = new CatalogService(store, [consoleSink, new FileSink(sinkLog)]);

var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

void OnCatalogChanged(object? sender, CatalogChangedEventArgs e)
{
    var line = $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} → залишок {e.QuantityAfter}";
    Console.WriteLine("  " + line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}

int changes = 0;
service.Changed += OnCatalogChanged;      // підписник 1: консоль + файл
service.Changed += (_, _) => changes++;   // підписник 2: лічильник (замикання)

Console.WriteLine($"Сховище: {store}");

// ===== 1. Події =====
Console.WriteLine();
Console.WriteLine("=== 1. Події (підписник OnCatalogChanged) ===");
var created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
service.Receive(created.Id, 25);
service.Issue(created.Id, 10);
Console.WriteLine($"Подій отримано: {changes}");

// ===== 2. Відписка =====
Console.WriteLine();
Console.WriteLine("=== 2. Після відписки (-=) ===");
service.Changed -= OnCatalogChanged;
service.Receive(created.Id, 5);
Console.WriteLine("  (рядка події немає, дані змінено)");
Console.WriteLine($"Подій отримано: {changes}");
Console.WriteLine($"Залишок: {service.Find(created.Id)?.Quantity}");

// ===== 3. Відмови =====
Console.WriteLine();
Console.WriteLine("=== 3. Сценарій відмови (подій немає) ===");
TryRun("Receive з неіснуючим id", () => service.Receive("no-such-id", 5));
TryRun("прихід від'ємної кількості", () => service.Receive(created.Id, -3));
TryRun("видача більша за залишок", () => service.Issue(created.Id, 1000));
Console.WriteLine($"Подій отримано після відмов: {changes}");

// ===== 4. Func =====
Console.WriteLine();
Console.WriteLine("=== 4. FindAll(p => p.Quantity < 10) ===");
foreach (var p in service.FindAll(p => p.Quantity < 10))
    Console.WriteLine($"  {p.Sku,-10} {p.Name,-22} {p.Quantity,5}");

// ===== 5. Додаткове 2: Action<string> =====
Console.WriteLine();
Console.WriteLine("=== 5. Додаткове 2: ImportLines з Action<string> ===");
var skipped = new List<string>();
int n = service.ImportLines(
    ["SKU-201;Лампа;шт;30", "битий рядок", "SKU-202;Розетка;шт;abc", ";Без SKU;шт;5"],
    msg => { Console.WriteLine($"  ! {msg}"); skipped.Add(msg); });
Console.WriteLine($"Імпортовано: {n}, пропущено: {skipped.Count}");

// ===== 6. Додаткове 1: INotificationSink =====
Console.WriteLine();
Console.WriteLine("=== 6. Додаткове 1: ConsoleSink (усі події, без відписки) ===");
foreach (var line in consoleSink.Lines)
    Console.WriteLine("  " + line);
Console.WriteLine($"Усього через sink: {consoleSink.Lines.Count}");

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