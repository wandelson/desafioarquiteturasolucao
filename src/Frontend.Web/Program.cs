using BlazorApp;
using FluentValidation;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddValidatorsFromAssemblyContaining<LancamentoValidator>();

// API de Lançamentos
builder.Services.AddHttpClient("LancamentosApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000"); // ajuste a porta da API.Lancamentos
});

// API de Relatórios
builder.Services.AddHttpClient("RelatoriosApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:5001"); // ajuste a porta da API.Relatorios
});

await builder.Build().RunAsync();