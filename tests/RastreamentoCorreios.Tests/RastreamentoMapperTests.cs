using RastreamentoCorreios.Erros;
using RastreamentoCorreios.Extracao;

namespace RastreamentoCorreios.Tests;

/// <summary>
/// Usa respostas reais do portal, salvas em amostras/.
/// </summary>
public class RastreamentoMapperTests
{
    private static string Amostra(string nome) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "amostras", nome));

    private static readonly string RespostaSucesso = Amostra("captcha-OK-resposta-exemplo.json");

    [Fact]
    public void Mapear_RespostaReal_PreservaTodosOsEventos()
    {
        var rastreamento = RastreamentoMapper.Mapear(RespostaSucesso);

        Assert.Equal("NN437873753BR", rastreamento.CodigoObjeto);
        Assert.Equal(12, rastreamento.Eventos.Count);
        Assert.Equal(new DateOnly(2026, 9, 3), rastreamento.PrevisaoEntrega);
    }

    [Fact]
    public void Mapear_EventoComum_PreencheLocalSemOrigemNemDestino()
    {
        var entregue = RastreamentoMapper.Mapear(RespostaSucesso).Eventos[0];

        Assert.Equal("Objeto entregue ao destinatário", entregue.Status);
        Assert.Equal("Sao Jose do Rio Preto", entregue.Local?.Cidade);
        Assert.Null(entregue.Origem);
        Assert.Null(entregue.Destino);
    }

    [Fact]
    public void Mapear_EventoDeTransferencia_PreencheOrigemEDestinoSemLocal()
    {
        var transferencia = RastreamentoMapper.Mapear(RespostaSucesso).Eventos[2];

        Assert.Equal("RO", transferencia.Codigo);
        Assert.Null(transferencia.Local);
        Assert.Equal("Unidade de Tratamento", transferencia.Origem?.Tipo);
        Assert.Equal("Unidade de Distribuição", transferencia.Destino?.Tipo);
    }

    [Fact]
    public void Mapear_DataDoPortal_ConverteComFusoDeSaoPaulo()
    {
        var dataHora = RastreamentoMapper.Mapear(RespostaSucesso).Eventos[0].DataHora;

        Assert.Equal(new DateTimeOffset(2026, 8, 31, 16, 4, 26, TimeSpan.FromHours(-3)), dataHora);
        Assert.Equal(TimeSpan.FromHours(-3), dataHora?.Offset);
    }

    [Fact]
    public void Mapear_TextoVazioDoPortal_ViraNull()
    {
        var local = RastreamentoMapper.Mapear(RespostaSucesso).Eventos[0].Local;

        Assert.Null(local?.Nome); // portal manda ""
        Assert.Null(local?.Uf);   // portal manda ""
    }

    [Fact]
    public void Mapear_EspacoNaoSeparavel_ViraEspacoComum()
    {
        var descricao = RastreamentoMapper.Mapear(RespostaSucesso).Eventos[0].Descricao;

        Assert.DoesNotContain('\u00a0', descricao?? string.Empty);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("<html>too many requests</html>")]
    public void Mapear_RespostaSemOFormatoEsperado_LancaRespostaInesperada(string json) =>
        Assert.Throws<RespostaInesperadaException>(() => RastreamentoMapper.Mapear(json));

    [Theory]
    [InlineData("captcha-inv-resposta-exemplo.json", "Captcha inválido")] // "erro": "true" (texto)
    [InlineData("codigo-inv-resposta-exemplo.json", "Período inválido")]  // "erro": true (booleano)
    public void LerErro_RespostaDeErro_DevolveMensagem(string arquivo, string mensagemEsperada) =>
        Assert.Equal(mensagemEsperada, RastreamentoMapper.LerErro(Amostra(arquivo)));

    [Fact]
    public void LerErro_RespostaDeSucesso_DevolveNull() =>
        Assert.Null(RastreamentoMapper.LerErro(RespostaSucesso));
}
