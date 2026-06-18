using System.Net;
using System.Net.Http.Json;
using Core.Application.Commands;
using Core.Application.Results;
using Core.Domain.Enums;
using FluentAssertions;
using Xunit;


public class LancamentosApiTests : IClassFixture<ApiApplicationFactory>
{
    private readonly HttpClient _client;

    public LancamentosApiTests()
    {
        var factory = new ApiApplicationFactory();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Deve_criar_lancamento_via_api()
    {
        var cmd = new RegistrarLancamentoCommand
        {
            Valor = 100,
            Descricao = "Venda teste",
            Data = new DateOnly(2025, 1, 1),
            Tipo = TipoLancamento.Credito
        };

        var response = await _client.PostAsJsonAsync("/api/lancamentos", cmd);           
        response.StatusCode.Should().Be(HttpStatusCode.Created);

    }
}
