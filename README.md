# CrossApp

Наскрізний проєкт з крос-платформного програмування.

## Середовище розробки:
.NET SDK: 10.0
ОС: macOS
IDE: Visual Studio Code

## Предметна область: Склад
- **Product** (товар) — номенклатура продукції;
- **StockBatch** (партія) — конкретна партія надходження з терміном придатності та ціною;
- **Warehouse** (склад) — фізичне місце зберігання;
- **Movement** (переміщення) — операція внутрішнього або зовнішнього переміщення товарів.

**Призначення:** Облік залишків товарів по партіях та контроль переміщень на складах.

## Запуск

### Стандартний запуск:
```bash
dotnet build
dotnet run --project src/Cli

### Запуск у форматі json:
```bash
dotnet run --project src/Cli -- --json

• macOS (osx-arm64): dotnet publish src/Cli-c Release-r osx-arm64--self-contained true
• Linux (linux-x64): dotnet publish src/Cli-c Release-r linux-x64--self-contained true
• Windows (win-x64): dotnet publish src/Cli -c Release -r win-x64 --self-contained true
