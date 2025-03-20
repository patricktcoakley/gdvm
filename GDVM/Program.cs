using ConsoleAppFramework;
using GDVM.Command;
using GDVM.Environment;
using GDVM.Filter;
using GDVM.Godot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Utf8StringInterpolation;
using ZLogger;
using static ConsoleAppFramework.ConsoleApp;

namespace GDVM;

public class Program
{
    public static int Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddLogging(logger =>
        {
            logger.ClearProviders();
            logger.SetMinimumLevel(LogLevel.Information);
            logger.AddZLoggerFile(Paths.LogPath,
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


        services.AddSingleton<SystemInfo>();
        services.AddSingleton<PlatformStringProvider>();
        services.AddSingleton<IHostSystem, HostSystem>();
        services.AddSingleton<IDownloadClient, DownloadClient>();
        services.AddSingleton<IReleaseManager, ReleaseManager>();


        // ensure we have a writeable directory
        if (!Directory.Exists(Paths.BinPath))
        {
            Directory.CreateDirectory(Paths.BinPath);
        }

        // ensure we have a config file
        if (!File.Exists(Paths.ConfigPath))
        {
            File.Create(Paths.ConfigPath);
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddIniFile(Paths.ConfigPath, false, true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

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

        app.UseFilter<ExitCodeFilter>();

        app.Run(args);

        return 0;
    }
}
