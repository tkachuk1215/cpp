using Cli;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;

ICatalogStore store = StoreFactory.Create(args);

// Прапорці (--file, --cache, --empty) не є назвою команди
var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top";

var service = new CatalogService(store);
IReadOnlyList<Product> items = args.Contains("--empty") ? Array.Empty<Product>() : service.All();

switch (command)
{
    case "report-top":
        ReportPrinter.Print(ReportService.TopByQuantity(items, 5));
        break;
    case "report-low":
        ReportPrinter.Print(ReportService.LowStock(items, 10));
        break;
    case "report-groups":
        ReportPrinter.Print(ReportService.ByUnit(items));
        break;
    case "report-groups-q":
        ReportPrinter.Print(ReportService.ByUnitQuery(items));
        break;
    case "report-sum":
        ReportPrinter.Print(ReportService.Totals(items));
        break;
    case "report-join":
        ReportPrinter.Print(ReportService.MovementLines(items, SampleData.Movements()));
        Console.WriteLine();
        ReportPrinter.Print(ReportService.MovementSummaries(items, SampleData.Movements()));
        break;
    case "report-moved":
        ReportPrinter.Print(ReportService.TopMoved(items, SampleData.Movements(), 3));
        break;
    case "demo-deferred":
        DeferredDemo.Run(items);
        break;
    case "bench":
        Benchmark.Run();
        break;
    default:
        Console.WriteLine("report-top | report-low | report-groups | report-groups-q | report-sum");
        Console.WriteLine("report-join | report-moved | demo-deferred | bench");
        break;
}

return 0;