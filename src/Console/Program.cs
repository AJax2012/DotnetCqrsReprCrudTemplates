using Cocona;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SpectreConsole;

using DotnetCqrsClassTemplatesUtility.Console.Commands;

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