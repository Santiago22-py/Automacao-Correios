using RastreamentoCorreios.Erros;

namespace RastreamentoCorreios.Tests;

public class CodigoRastreamentoTests
{
    [Theory]
    [InlineData("NN437873753BR")]
    [InlineData("  nn437873753br ")]
    public void Validar_CodigoValido_DevolveEmMaiusculasSemEspacos(string codigo) =>
        Assert.Equal("NN437873753BR", CodigoRastreamento.Validar(codigo));

    [Fact]
    public void Validar_DigitoVerificadorCorreto_Aceita() =>
        Assert.Equal("AA000000005BR", CodigoRastreamento.Validar("AA000000005BR"));

    [Theory]
    [InlineData("")]
    [InlineData("NAA123456789BR")] // um caractere a mais
    [InlineData("NN43787375BR")]   // um dígito a menos
    [InlineData("NN437873750BR")]  // dígito verificador errado (o correto é 3)
    [InlineData("AA000000000BR")]  // dígito verificador errado (o correto é 5)
    [InlineData("1N437873753BR")]  // prefixo precisa ser letra
    public void Validar_CodigoInvalido_LancaCodigoInvalido(string codigo) =>
        Assert.Throws<CodigoInvalidoException>(() => CodigoRastreamento.Validar(codigo));
}
