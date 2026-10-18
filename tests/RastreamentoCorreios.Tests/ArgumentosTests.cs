namespace RastreamentoCorreios.Tests;

public class ArgumentosTests
{
    [Fact]
    public void Ler_SoOCodigo_UsaOpcoesPadrao()
    {
        var argumentos = Argumentos.Ler(["NN437873753BR"]);

        Assert.Equal("NN437873753BR", argumentos.Codigo);
        Assert.Null(argumentos.ArquivoSaida);
        Assert.Equal(new OpcoesRastreamento(), argumentos.Opcoes);
    }

    [Fact]
    public void Ler_TodasAsOpcoes_AplicaCadaUma()
    {
        var argumentos = Argumentos.Ler(
            ["--tentativas", "5", "NN437873753BR", "--saida", "saida.json", "--intervalo", "2000", "--timeout", "15000", "--visivel"]);

        Assert.Equal("NN437873753BR", argumentos.Codigo);
        Assert.Equal("saida.json", argumentos.ArquivoSaida);
        Assert.Equal(new OpcoesRastreamento(5, 2000, 15000, Headless: false), argumentos.Opcoes);
    }

    [Theory]
    [InlineData()]                                   // sem código
    [InlineData("NN437873753BR", "AA000000005BR")]   // dois códigos
    [InlineData("NN437873753BR", "--tentativas")]    // opção sem valor
    [InlineData("NN437873753BR", "--tentativas", "0")]
    [InlineData("NN437873753BR", "--tentativas", "abc")]
    [InlineData("NN437873753BR", "--desconhecida")]
    public void Ler_ArgumentosInvalidos_LancaArgumentException(params string[] args) =>
        Assert.Throws<ArgumentException>(() => Argumentos.Ler(args));
}
