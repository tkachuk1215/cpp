# CrossApp

Наскрізний проєкт з крос-платформного програмування.

## Середовище розробки
- .NET SDK: 10.0
- ОС: macOS (Apple Silicon / osx-arm64)
- IDE: Visual Studio Code

## Предметна область: Склад
- Product (товар) — номенклатура продукції;
- StockBatch (партія) — конкретна партія надходження з терміном придатності та ціною;
- Warehouse (склад) — фізичне місце зберігання;
- Movement (переміщення) — операція внутрішнього або зовнішнього переміщення товарів.

Призначення: Облік залишків товарів по партіях та контроль переміщень на складах.

## Структура проєкту

```
CrossApp/
├── CrossApp.slnx
├── .gitignore
├── data/
│   ├── sample.csv          # 10 коректних і 3 пошкоджені рядки
│   ├── sample.json         # 2 коректні і 2 пошкоджені об'єкти
│   └── heterogeneous.csv   # змішані записи: товари (P) і склади (W)
├── publish/
└── src/
    ├── Cli/
    │   ├── Cli.csproj
    │   └── Program.cs      # аргументи, імпорт, вивід, демонстрація доменної моделі
    └── Core/
        ├── Core.csproj
        ├── EnvironmentInfo.cs
        ├── Domain/                         # сутності з поведінкою та інваріантами (ЛР 4)
        │   ├── Product.cs
        │   ├── ProductStatus.cs
        │   ├── Warehouse.cs
        │   └── WarehousePlacementPolicy.cs
        ├── Dto/                            # формат даних (ЛР 3)
        │   ├── ImportResult.cs
        │   ├── ImportStats.cs
        │   └── ProductDto.cs
        └── Import/
            ├── ProductCsvImporter.cs
            ├── ProductImportMapper.cs      # DTO -> сутності (ЛР 4)
            └── ProductJsonImporter.cs
```

## Імпорт даних (ЛР 3)

### Формат CSV (`data/sample.csv`)

| Параметр | Значення |
| :--- | :--- |
| Роздільник | `;` (крапка з комою) |
| Заголовок | `id;sku;name;unit;quantity` — необов'язковий, якщо є, то пропускається |
| Кодування | UTF-8 |
| Коментарі | рядки, що починаються з `#`, пропускаються |
| Порожні рядки | пропускаються |
| Числа | розбираються з `CultureInfo.InvariantCulture` |

Приклад рядка: `P-001;SKU-001;Цемент М400 25кг;шт;120`

Правила валідації: рівно 5 колонок; `id`, `sku`, `name`, `unit` не порожні; `quantity` — невід'ємне ціле число.
Некоректний рядок не перериває імпорт: він потрапляє в список помилок із номером рядка.

### Навмисно пошкоджені рядки в `sample.csv`

Це тестові дані для перевірки обробки помилок:

| Рядок | Дефект | Повідомлення |
| :---: | :--- | :--- |
| 12 | мало колонок | `очікую 5 колонок, отримав 4` |
| 13 | нечислова кількість | `кількість 'багато' має бути невід'ємним цілим числом` |
| 14 | порожній SKU | `SKU не може бути порожнім` |

### Формат JSON (`data/sample.json`)

Масив об'єктів з полями `id`, `sku`, `name`, `unit`, `quantity` (регістр імен властивостей не важливий).
Необов'язкові поля: `note` (примітка) та `status` (`Active`, `Discontinued` або `Archived`; якщо не задано, товар активний).
Помилки повідомляються з номером об'єкта (`об'єкт JSON #3`).

### Гетерогенний CSV (`data/heterogeneous.csv`)

Додаткове завдання: перша колонка — тип запису.

- `P;id;sku;name;unit;quantity` — товар;
- `W;id;name;address` — склад.

Цей режим вмикається, якщо файл називається `heterogeneous.csv`.

### Запуск імпорту

```bash
dotnet run --project src/Cli                          # data/sample.csv за замовчуванням
dotnet run --project src/Cli data/sample.json         # JSON
dotnet run --project src/Cli data/heterogeneous.csv   # змішані записи
dotnet run --project src/Cli data/nofile.csv          # неіснуючий файл
echo $?
```

Cli виводить кількість завантажених записів, перші 5, перелік пропущених рядків з номерами та рядок статистики.

### Коди завершення

| Код | Значення |
| :---: | :--- |
| 0 | імпорт виконано (навіть якщо частину рядків пропущено) |
| 1 | файл не знайдено або непідтримуваний формат |

## Доменна модель (ЛР 4)

Сутності лежать у `src/Core/Domain`: `Product`, `Warehouse`, `ProductStatus`, а правило на дві сутності
— у `WarehousePlacementPolicy`. Records з `Core/Dto` залишилися DTO (формат даних),
зв'язок з сутностями — через `ToDto` / `FromDto`.

Стан сутностей інкапсульований: конструктори приватні, об'єкти створюються лише фабричними методами `Create`,
змінити стан можна лише методами з бізнес-назвами (`RegisterArrival`, `Issue`, `Discontinue`, `Reactivate`, `Archive`).

### Інваріанти

- Id, SKU, назва та одиниця виміру товару не порожні (`ArgumentException`, `Product.Create`)
- Початковий залишок ≥ 0 (`ArgumentOutOfRangeException`, `Product.Create`)
- Статус товару — відомий елемент `ProductStatus` (`ArgumentOutOfRangeException` / `ArgumentException`, `Product.Create`, `Product.FromDto`)
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

### Додаткові завдання

- **Імпорт у сутності:** `ProductImportMapper.ToEntities` перетворює `ImportResult<ProductDto>` на сутності
  та перелік записів, які не пройшли інваріанти.
- **Правило на дві сутності:** `WarehousePlacementPolicy` (ліміт різних товарів на складі).
  Воно винесене з сутностей, бо потребує списку розміщених товарів зі сховища (сервіс з'явиться на тижні 5).
- **Явний статус:** `ProductStatus` (`Active`, `Discontinued`, `Archived`), допустимі переходи
  перевіряються через `switch` expression у `Product.IsTransitionAllowed`.

### Демонстрація

`dotnet run --project src/Cli` виводить після імпорту блок «Імпорт -> сутності» та два сценарії:
успішний і той, що порушує інваріанти (виводиться лише `Message` винятків, без stack trace).
Для `heterogeneous.csv` демонстрація доменної моделі не виконується.

## Запуск та Збірка

### Збірка рішення:
dotnet build

### Запуск CLI-застосунку:
dotnet run --project src/Cli

## Публікація (Publishing)

### Команди публікації:

- macOS (osx-arm64) Self-contained:
  dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -o publish/sc

- macOS (osx-arm64) Framework-dependent:
  dotnet publish src/Cli -c Release -r osx-arm64 --self-contained false -o publish/fd

- Linux (linux-x64) Self-contained:
  dotnet publish src/Cli -c Release -r linux-x64 --self-contained true

- Windows (win-x64) Self-contained:
  dotnet publish src/Cli -c Release -r win-x64 --self-contained true

### Порівняння режимів публікації (Publishing)

| RID | Режим | Потрібен runtime | Розмір publish | Примітка |
| :--- | :--- | :---: | :---: | :--- |
| **osx-arm64** | Framework-dependent (`fd`) | Так (.NET 10) | ~0.2 МБ | Лише бінарники додатка та залежності |
| **osx-arm64** | Self-contained (`sc`) | Ні | ~80 МБ | Містить повну копію .NET Runtime |
| **osx-arm64** | Single-file (`single`) | Ні | ~80 МБ | Усі компоненти та Runtime упаковано в один файл |
| **osx-arm64** | Trimmed (`trimmed`) | Ні | ~15 МБ | Видалено невикористаний код .NET Runtime (IL Trimming) |