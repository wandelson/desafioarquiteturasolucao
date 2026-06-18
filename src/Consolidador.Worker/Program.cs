using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Core.Application;
using Core.Application.Ports;
using Infrastructure.Context;
using Infrastructure.InMemory;
using Infrastructure.Queue;
using Infrastructure.Repository;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal class Program
{
    private static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");

        var awsSqsSettings = context.Configuration.GetSection("AwsSqs").Get<AwsSqsOptions>()
            ?? throw new InvalidOperationException("Configuration section 'AwsSqs' is missing or invalid.");

        services.AddSingleton(awsSqsSettings);

        services.AddDbContext<AppDbContext>(opt =>
          opt.UseNpgsql(connectionString));

        services.AddScoped<ILancamentoRepository, LancamentoRepository>();
        services.AddScoped<ISaldoDiarioRepository, SaldoDiarioRepository>();
        services.AddScoped<ConsolidarDiaHandler>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<LancamentoCriadoConsumer>();

            x.UsingAmazonSqs((context, cfg) =>
            {
                cfg.Host(new Uri(awsSqsSettings.Host), h =>
                {
                    h.AccessKey(awsSqsSettings.AccessKey);
                    h.SecretKey(awsSqsSettings.SecretKey);
                    h.Config(new AmazonSQSConfig
                    {
                        ServiceURL = awsSqsSettings.ServiceURL,
                        UseHttp = awsSqsSettings.UseHttp,
                        AuthenticationRegion = awsSqsSettings.AuthenticationRegion
                    });
                    h.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = awsSqsSettings.ServiceURL });
                });

                cfg.ReceiveEndpoint("lancamento-criado", e =>
                {
                    e.ConfigureConsumer<LancamentoCriadoConsumer>(context);
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                });
            });
        });

        services.AddHostedService<MassTransitHostedService>();
    })
    .Build();

        await host.RunAsync();
    }
}