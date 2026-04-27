using Avryd.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(opts => opts.ServiceName = "Avryd Screen Reader")
    .ConfigureServices(services =>
    {
        services.AddHostedService<AvrydWorker>();
    })
    .Build();

await host.RunAsync();
