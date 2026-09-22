using System;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Cli;

class Program
{
    static void Main(string[] args)
    {
        bool isJson = args.Length > 0 && args[0] == "--json";

        var info = new
        {
            Student = "Ткачук Богдан, ФеІ-35с",
            OsDescription = RuntimeInformation.OSDescription,
            OsVersion = Environment.OSVersion.ToString(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            DotNetVersion = Environment.Version.ToString(),
            Runtime = RuntimeInformation.FrameworkDescription,
            AppDirectory = AppContext.BaseDirectory,
            CurrentDirectory = Environment.CurrentDirectory,
            Domain = "Склад (товари, партії, залишки, переміщення)"
        };

        if (isJson)
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            Console.WriteLine(JsonSerializer.Serialize(info, options));
        }
        else
        {
            Console.WriteLine("CrossApp — практикум з крос-платформного програмування");
            Console.WriteLine($"Студент: {info.Student}");
            Console.WriteLine(new string('-', 52));
            Console.WriteLine($"OC (OSDescription): {info.OsDescription}");
            Console.WriteLine($"OC (Environment) : {info.OsVersion}");
            Console.WriteLine($"Архітектура процесу: {info.ProcessArchitecture}");
            Console.WriteLine($"Версія .NET (CLR): {info.DotNetVersion}");
            Console.WriteLine($"Runtime : {info.Runtime}");
            Console.WriteLine($"Каталог застосунку: {info.AppDirectory}");
            Console.WriteLine($"Поточний каталог : {info.CurrentDirectory}");
            Console.WriteLine(new string('-', 52));
            Console.WriteLine($"Предметна область: {info.Domain}");
        }
    }
}