# CrossApp — складський облік (Крос-платформне програмування)

Рішення складається з двох проєктів:

- `src/Core` — доменна модель, DTO, абстракції, сховища, сервіси
- `src/Cli` — консольний застосунок (composition root), посилання Cli → Core

> Розділи про імпорт CSV/JSON (тиждень 3) залиште тут без змін, якщо вони були на початку вашого README.

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
| `CatalogService` | `src/Core/Services/CatalogService.cs` | Бізнес-операції: `Add`, `Receive` (прихід), `Issue` (видача), `All`, `Find`, пошук за предикатом |
| `SampleData` | `src/Core/SampleData.cs` | Демонстраційні дані (15–20 записів) |
| `StoreFactory` | `src/Cli/StoreFactory.cs` | Вибір реалізації за аргументами командного рядка |
| `Program.cs` | `src/Cli/Program.cs` | Composition root і демонстраційний сценарій |

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
  наприклад `src/Cli/bin/Debug/net10.0/data/catalog.json`.
- Формат: JSON з відступами. На диск записуються `ProductDto`, а не сутності
  (мапінг `Product.FromDto` / `product.ToDto`).

### Режими запуску

| Команда | Сховище | Поведінка |
| :--- | :--- | :--- |
| `dotnet run --project src/Cli` | `InMemoryCatalogStore` | Дані з `SampleData`, після виходу зникають |
| `dotnet run --project src/Cli -- --file` | `FileCatalogStore` | Створюється `catalog.json`, повторний запуск бачить попередні записи |
| `dotnet run --project src/Cli -- --cache` | `CachingCatalogStore` | Декоратор над in-memory сховищем |
| `dotnet run --project src/Cli -- --file --cache` | `CachingCatalogStore` → `FileCatalogStore` | Декоратор над файловим сховищем |

Щоб очистити файлові дані, видаліть `catalog.json`:

```bash
rm -f src/Cli/bin/Debug/net10.0/data/catalog.json
```

### Демонстрація

Сценарій у `Program.cs`: додати товар, змінити (прихід), показати список, знайти за id.
Також демонструються помилки, які перехоплюються в `try/catch` і виводяться людським текстом:
дубль id, відсутній id, видача більша за залишок, некоректна кількість приходу.

## Запуск та збірка

### Збірка рішення:

```bash
dotnet build
```

### Запуск CLI-застосунку:

```bash
dotnet run --project src/Cli
dotnet run --project src/Cli -- --file
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
