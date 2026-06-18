using Core.Application;
using Core.Application.Ports;
using Infrastructure.Context;
using Infrastructure.InMemory;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API de relatórios",
        Version = "v1",
        Description = "Responsável por obter o relatório consolidado do dia."
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

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<ILancamentoRepository, LancamentoRepository>();
builder.Services.AddScoped<ISaldoDiarioRepository, SaldoDiarioRepository>();
builder.Services.AddScoped<ObterRelatorioDiaHandler>();

var app = builder.Build();
app.UseCors();
app.MapControllers();


app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Relatórios v1");
    options.DocumentTitle = "Documentação - API de Relatórios";
});


app.Run();