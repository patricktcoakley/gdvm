using ConsoleAppFramework;
using GDVM.Command;
using GDVM.Environment;
using GDVM.Error;
using GDVM.Filter;
using GDVM.Godot;
using GDVM.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Net.Http.Headers;
using Utf8StringInterpolation;
using ZLogger;
using static ConsoleAppFramework.ConsoleApp;

namespace GDVM;

public class Program
{
    public static int Main(string[] args)
    {
        var pathService = new PathService();

        var services = new ServiceCollection();
        services.AddLogging(logger =>
        {
            logger.ClearProviders();
            logger.SetMinimumLevel(LogLevel.Information);
            logger.AddZLoggerFile(pathService.LogPath,
                opts =>
                {
                    opts.UsePlainTextFormatter(formatter =>
                    {
                        formatter.SetPrefixFormatter($"{0}|{1}|",
                            (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel));

                        formatter.SetSuffixFormatter($"|({0})",
                            (in MessageTemplate template, in LogInfo info) => template.Format(info.Category));

                        formatter.SetExceptionFormatter((writer, ex) => Utf8String.Format(writer, $"{ex.Message}"));
                    });
                });
        });

        // Register services
        services.AddSingleton<IPathService>(pathService);
        services.AddSingleton<Messages>();
        services.AddSingleton<SystemInfo>();
        services.AddSingleton<PlatformStringProvider>();
        services.AddSingleton<IHostSystem, HostSystem>();

        // HTTP Clients - Named clients for different services
        services.AddHttpClient<IGitHubClient, GitHubClient>("github")
            .ConfigureHttpClient((_, client) =>
            {
                var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("gdvm", version));
            });

        services.AddHttpClient<ITuxFamilyClient, TuxFamilyClient>("tuxfamily")
            .ConfigureHttpClient((_, client) =>
            {
                var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("gdvm", version));
            });

        services.AddSingleton<IDownloadClient, DownloadClient>();
        services.AddSingleton<IReleaseManager, ReleaseManager>();
        services.AddSingleton<IInstallationService, InstallationService>();
        services.AddSingleton<IVersionManagementService, VersionManagementService>();
        services.AddSingleton<IGodotArgumentService, GodotArgumentService>();
        services.AddSingleton<IAnsiConsole>(_ => AnsiConsole.Console);

        // ensure we have a writeable directory
        if (!Directory.Exists(pathService.BinPath))
        {
            Directory.CreateDirectory(pathService.BinPath);
        }

        // ensure we have a config file
        if (!File.Exists(pathService.ConfigPath))
        {
            File.WriteAllText(pathService.ConfigPath, "# GDVM Configuration File\n");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddIniFile(pathService.ConfigPath, false, true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        Configuration.ValidateConfiguration(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        ConsoleApp.ServiceProvider = serviceProvider;

        var app = Create();

        app.Add<GodotCommand>();
        app.Add<SetCommand>();
        app.Add<WhichCommand>();
        app.Add<InstallCommand>();
        app.Add<ListCommand>();
        app.Add<RemoveCommand>();
        app.Add<LogsCommand>();
        app.Add<SearchCommand>();
        app.Add<LocalCommand>();

        app.UseFilter<ExitCodeFilter>();

        app.Run(args);

        return 0;
    }
}
