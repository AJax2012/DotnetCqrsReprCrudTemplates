// See https://aka.ms/new-console-template for more information

using Cocona;
using DotnetCqrsClassTemplatesUtility.Console.Commands;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SpectreConsole;
using ILogger = Serilog.ILogger;

var builder = CoconaApp.CreateBuilder();

builder.Logging.AddFilter("System.Net.Http", LogLevel.Warning);

ILogger logger = new LoggerConfiguration()
	.Enrich.WithDemystifiedStackTraces()
	.WriteTo.SpectreConsole(minLevel: LogEventLevel.Information)
	.CreateLogger();

builder.Host.UseSerilog(logger);

var app = builder.Build();

app.AddCommands<CreateFilesCommand>();

app.Run();