using Amazon.SQS;
using Core.Application.Handlers;
using Core.Application.Ports;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Context;
using Infrastructure.InMemory;
using Infrastructure.Queue;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API de Lançamentos",
        Version = "v1",
        Description = "Endpoints responsáveis por registrar lançamentos (crédito/débito)."
    });
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IRegistrarLancamentoHandler, RegistrarLancamentoHandler>();
builder.Services.AddScoped<ILancamentoRepository, LancamentoRepository>();
builder.Services.AddScoped<IEventQueue, MassTransitEventQueue>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LancamentoValidator>();

var awsOptions = builder.Configuration.GetSection("AwsSqs").Get<AwsSqsOptions>() ?? new AwsSqsOptions();
builder.Services.Configure<AwsSqsOptions>(builder.Configuration.GetSection("AwsSqs"));

builder.Services.AddMassTransit(x =>
{
    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(awsOptions.Host, h =>
        {
            h.AccessKey(awsOptions.AccessKey);
            h.SecretKey(awsOptions.SecretKey);

            h.Config(new AmazonSQSConfig
            {
                ServiceURL = awsOptions.ServiceURL,
                UseHttp = awsOptions.UseHttp,
                AuthenticationRegion = awsOptions.AuthenticationRegion
            });
        });
    });
});
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors();
app.MapControllers();


app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Lançamentos v1");
    options.DocumentTitle = "Documentação - API de Lançamentos";
});



app.Run();