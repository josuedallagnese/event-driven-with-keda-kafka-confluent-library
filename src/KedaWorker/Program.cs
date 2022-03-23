using KedaWorker;
using MediatR;
using Shared;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        var env = hostingContext.HostingEnvironment;
        config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: false, reloadOnChange: true);

        config.AddEnvironmentVariables();
        config.AddCommandLine(args);
    })
    .ConfigureLogging((hostingContext, config) =>
    {
        config.AddConsole();
        config.AddConfiguration(hostingContext.Configuration);
    })
    .ConfigureServices((hostContext, services) =>
    {
        var eventHubConfiguration = new KafkaConfiguration(hostContext.Configuration);

        services.AddSingleton(eventHubConfiguration);

        services.AddMediatR(typeof(Program).Assembly);

        services.AddHostedService<Worker>();

    }).Build();

await host.RunAsync();
