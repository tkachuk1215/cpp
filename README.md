# CrossApp — складський облік (Крос-платформне програмування)

Рішення складається з двох проєктів:

- `src/Core` — доменна модель, DTO, абстракції, сховища, сервіси, події, звіти
- `src/Cli` — консольний застосунок (composition root), посилання Cli → Core

## Доменна модель (тиждень 4)

Стан сутностей інкапсульований: конструктори приватні, об'єкти створюються лише фабричними методами Create,
змінити стан можна лише методами з бізнес-назвами (`RegisterArrival`, `Issue`, `Discontinue`, `Reactivate`, `Archive`).

### Інваріанти

- Id, SKU, назва та одиниця виміру товару не порожні (`ArgumentException`, `Product.Create`)
- Початковий залишок ≥ 0 (`ArgumentOutOfRangeException`, `Product.Create`)
- Статус товару — відомий елемент ProductStatus (`ArgumentOutOfRangeException` / `ArgumentException`, `Product.Create`, `Product.FromDto`)
- Архівний товар не може мати залишок (`ArgumentException`, `Product.Create`)
- Кількість приходу > 0 (`ArgumentOutOfRangeException`, `Product.RegisterArrival`)
- Кількість видачі > 0 (`ArgumentOutOfRangeException`, `Product.Issue`)
- Не можна видати більше, ніж є на залишку (`InvalidOperationException`, `Product.Issue`)
- Прихід лише для активного товару, видача не для архівного (`InvalidOperationException`, `Product.RegisterArrival`, `Product.Issue`)
- Допустимі переходи статусу: Active ↔ Discontinued, Discontinued → Archived
  (`InvalidOperationException`, `Product.Discontinue` / `Reactivate` / `Archive`)
- Архівувати можна лише товар із нульовим залишком (`InvalidOperationException`, `Product.Archive`)
- Id, назва й адреса складу не порожні (`ArgumentException`, `Warehouse.Create`, `Warehouse.ChangeAddress`)
- На складі не більше 5 різних товарів, без дублів SKU і без архівних
  (`InvalidOperationException`, `WarehousePlacementPolicy.EnsureCanPlace`)
- `FromDto` не обходить перевірки: відновлює сутність через `Create` (`Product.FromDto`, `Warehouse.FromDto`)

### Додаткові завдання (тиждень 4)

- Імпорт у сутності: `ProductImportMapper.ToEntities` перетворює `ImportResult<ProductDto>` на сутності
  та перелік записів, які не пройшли інваріанти.
- Правило на дві сутності: `WarehousePlacementPolicy` (ліміт різних товарів на складі).
- Явний статус: `ProductStatus` (`Active`, `Discontinued`, `Archived`), допустимі переходи
  перевіряються через switch expression у `Product.IsTransitionAllowed`.

## Сервісний шар (тиждень 5)

Доменна логіка не залежить від способу зберігання даних: сервіс працює з абстракцією сховища,
а конкретна реалізація обирається вручну в точці входу (ручний DI, без контейнера).

### Компоненти

| Компонент | Файл | Призначення |
| :--- | :--- | :--- |
| `ICatalogStore` | `src/Core/Abstractions/ICatalogStore.cs` | Контракт сховища: `List`, `GetById`, `Add`, `Update`, `Remove` |
| `InMemoryCatalogStore` | `src/Core/Storage/InMemoryCatalogStore.cs` | Сховище в пам'яті (`Dictionary`), початкові дані через конструктор |
| `FileCatalogStore` | `src/Core/Storage/FileCatalogStore.cs` | Файлове сховище: кеш у пам'яті, ледаче завантаження, запис JSON після кожної зміни |
| `CachingCatalogStore` | `src/Core/Storage/CachingCatalogStore.cs` | Декоратор над будь-яким `ICatalogStore` (додаткове завдання) |
| `CatalogService` | `src/Core/Services/CatalogService.cs` | Бізнес-операції: `Add`, `Receive` (прихід), `Issue` (видача), `Remove`, `All`, `Find`, `FindAll`, подія `Changed` |
| `SampleData` | `src/Core/SampleData.cs` | Демонстраційні дані (20 записів, 5 одиниць виміру), переміщення для Join, генератор для бенчмарка |
| `StoreFactory` | `src/Cli/StoreFactory.cs` | Вибір реалізації за аргументами командного рядка |

### Схема залежностей

```
Cli (Program.cs, StoreFactory) → CatalogService → ICatalogStore ← InMemoryCatalogStore | FileCatalogStore
                                                                  ↑
                                                       CachingCatalogStore (обгортає інше сховище)
```

- `CatalogService` приймає `ICatalogStore` через конструктор і не знає про конкретні реалізації.
- Конкретні класи створюються лише в проєкті Cli (`StoreFactory` + `Program.cs` — composition root).
- `Core` не містить `new FileCatalogStore(...)` та викликів `Console.`.
- `Microsoft.Extensions.DependencyInjection` **не підключено** (контейнер з'явиться разом з Api).

### Формат зберігання

- Файловий режим: `data/catalog.json` відносно папки виконання (`AppContext.BaseDirectory`),
  наприклад `src/Cli/bin/Debug/net8.0/data/catalog.json`.
- Формат: JSON з відступами. На диск записуються `ProductDto`, а не сутності
  (мапінг `Product.FromDto` / `product.ToDto`).

### Режими сховища

| Прапорець | Сховище | Поведінка |
| :--- | :--- | :--- |
| (немає) | `InMemoryCatalogStore` | Дані з `SampleData`, після виходу зникають |
| `--file` | `FileCatalogStore` | Створюється `catalog.json`, повторний запуск бачить попередні записи |
| `--cache` | `CachingCatalogStore` | Декоратор над in-memory сховищем |
| `--file --cache` | `CachingCatalogStore` → `FileCatalogStore` | Декоратор над файловим сховищем |

Щоб очистити файлові дані, видаліть `catalog.json`:

```bash
rm -f src/Cli/bin/Debug/net8.0/data/catalog.json
```

## Доменні події (тиждень 6)

`CatalogService` сповіщає зовнішній код про зміни стану через подію. `Core` лише піднімає подію
і нічого не знає про консоль чи файли: обробку визначає `Cli`.

### Компоненти

| Компонент | Файл | Призначення |
| :--- | :--- | :--- |
| `CatalogChangeKind` | `src/Core/Events/CatalogChangeKind.cs` | Вид зміни: `Added`, `Received`, `Issued`, `Removed` |
| `CatalogChangedEventArgs` | `src/Core/Events/CatalogChangedEventArgs.cs` | Дані події: `Id`, `Kind`, `Delta`, `QuantityAfter`, `At` |
| `CatalogService.Changed` | `src/Core/Services/CatalogService.cs` | `event EventHandler<CatalogChangedEventArgs>?`, піднімається в `OnChanged` після успішної зміни |
| `INotificationSink` | `src/Core/Notifications/INotificationSink.cs` | Контракт приймача сповіщень (додаткове завдання 1) |
| `ConsoleSink`, `FileSink` | `src/Cli/` | Реалізації приймачів, передаються сервісу через конструктор |

### Правила

- Подія піднімається лише після успішної зміни: якщо інваріант кинув виняток, повідомлення немає.
- Підписники в Cli: локальний метод `OnCatalogChanged` (консоль + `logs/app.log`) і лямбда-лічильник із замиканням.
- Після `-=` обробник мовчить, але дані змінюються.
- `FindAll(Func<Product, bool>)` повертає значення, тому тут делегат `Func`, а не подія.
- `ImportLines(lines, Action<string> onSkipped)`: Core повідомляє про пропущені рядки через колбек (додаткове завдання 2).
- `logs/app.log` містить події до відписки, `logs/sink.log` — усі події (sink не відписується).
- Демонстраційний сценарій подій лежить у тезі `lab06`: `git checkout lab06`.

## LINQ-звіти (тиждень 7)

Аналітика реалізована в `Core/Reports` як чисті статичні функції: вони приймають `IEnumerable<Product>`
і повертають `IReadOnlyList<DTO>`. `Program.cs` не містить `Where`/`GroupBy`, лише виклики звітів і вивід.

### Запити

| Метод | Питання | Оператори |
| :--- | :--- | :--- |
| `LowStock(source, threshold)` | Яких товарів бракує? | `Where`, `OrderBy`, `ThenBy`, `Select` |
| `TopByQuantity(source, n)` | Які позиції мають найбільший залишок? | `TopBy` (власне розширення: `OrderByDescending`, `ThenBy`, `Take`) |
| `ByUnit(source)` | Як розподілено залишок за одиницями? | `GroupBy`, `Count`, `Sum`, `OrderByDescending` |
| `ByUnitQuery(source)` | Те саме в синтаксисі запитів | `from ... group ... into ... orderby ... select` |
| `Totals(source)` | Скільки позицій, сума, середнє, максимум? | `Count`, `Sum`, `Average`, `Max` (порожнє джерело дає нулі) |
| `MovementLines(products, movements)` | Які переміщення має кожен товар? | `Join` |
| `MovementSummaries(products, movements)` | Зведення по всіх товарах, навіть без рухів | `GroupJoin` |
| `TopMoved(products, movements, n)` | Які товари рухались найчастіше? | `GroupJoin`, `TopBy` |

DTO: `StockLine`, `GroupSummary`, `MovementLine`, `MovementSummary` (record). Топ-N має другий ключ
сортування (`Sku`), тому порядок однаковий за кожного запуску.

### Підкоманди

```bash
dotnet run --project src/Cli -- report-top
dotnet run --project src/Cli -- report-low
dotnet run --project src/Cli -- report-groups
dotnet run --project src/Cli -- report-groups-q
dotnet run --project src/Cli -- report-sum
dotnet run --project src/Cli -- report-join
dotnet run --project src/Cli -- report-moved
dotnet run --project src/Cli -- demo-deferred
dotnet run --project src/Cli -c Release -- bench
```

Прапорці `--file`, `--cache` не сприймаються як назва команди; `--empty` підставляє порожнє джерело
(звіти не падають, друкують `(порожньо)` або нулі).

### Відкладене виконання

Запит `source.Where(p => p.Quantity < 10)` лише описує відбір і нічого не обчислює. Виконання
починається під час `Count()`, `ToList()` або `foreach`. У `demo-deferred` перший `query.Count()`
дає 5, бо перебирає список у момент виклику. Після `source.Add(...)` другий `query.Count()` дає 6:
запит виконався заново і побачив новий елемент. `ToList()` зафіксував результат, тому
`snapshot.Count` лишається 6 навіть після `source.Clear()`, а сам запит після очищення дає 0.
`query.Count()` — метод, що перебирає послідовність, `snapshot.Count` — властивість готового списку.

### Синтаксис запитів і методів

`ByUnit` (методний синтаксис) і `ByUnitQuery` (синтаксис запитів) дають однаковий результат.
Для групування запис `group ... by ... into` читається природніше, а `Where`/`Take` зручніші зліва направо
в методному синтаксисі. У синтаксисі запитів не можна передати `StringComparer`, тому ключ
нормалізується через `ToLowerInvariant()`, що й робить його повільнішим на 100 000 записів.

### Вимірювання часу (Stopwatch, 100 000 записів, Release, середнє з 5 запусків)

| Звіт | Час, мс |
| :--- | ---: |
| `LowStock` без проміжних `ToList` | 30.5 |
| `LowStock` з `ToList` після `Where` і `OrderBy` | 16.4 |
| `TopByQuantity` (`TopBy`, n=5) | 2.2 |
| `ByUnit` (методний синтаксис) | 15.1 |
| `ByUnitQuery` (синтаксис запитів) | 23.6 |

Проміжні `ToList` не обов'язково повільніші: вони додають виділення пам'яті, але можуть здешевити
подальше сортування. Вимірювання з 5 запусків має похибку, тому висновок орієнтовний.

## Запуск та збірка

```bash
dotnet build
dotnet run --project src/Cli -f net8.0 -- report-top
dotnet run --project src/Cli -f net8.0 -- report-sum --file
```

## Публікація (Publishing)

### Команди публікації:

- macOS (osx-arm64) Self-contained:
  `dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -o publish/sc`

- macOS (osx-arm64) Framework-dependent:
  `dotnet publish src/Cli -c Release -r osx-arm64 --self-contained false -o publish/fd`

- Linux (linux-x64) Self-contained:
  `dotnet publish src/Cli -c Release -r linux-x64 --self-contained true`

- Windows (win-x64) Self-contained:
  `dotnet publish src/Cli -c Release -r win-x64 --self-contained true`

### Порівняння режимів публікації (Publishing)

| RID | Режим | Потрібен runtime | Розмір publish | Примітка |
| :--- | :--- | :---: | :---: | :--- |
| osx-arm64 | Framework-dependent (`fd`) | Так (.NET 10) | ~0.2 МБ | Лише бінарники додатка та залежності |
| osx-arm64 | Self-contained (`sc`) | Ні | ~80 МБ | Містить повну копію .NET Runtime |
| osx-arm64 | Single-file (`single`) | Ні | ~80 МБ | Усі компоненти та Runtime упаковано в один файл |
| osx-arm64 | Trimmed (`trimmed`) | Ні | ~15 МБ | Видалено невикористаний код .NET Runtime (IL Trimming) |
