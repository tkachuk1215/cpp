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
