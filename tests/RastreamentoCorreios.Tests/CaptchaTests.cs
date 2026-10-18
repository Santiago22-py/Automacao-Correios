using RastreamentoCorreios.Captcha;

namespace RastreamentoCorreios.Tests;

public class CaptchaTests
{
    private const string Permitidos = "abcdefghklmnprstuvwyz23456789";

    [Theory]
    [InlineData("bauy", true)]
    [InlineData("5efz7e", true)]
    [InlineData("bay", false)]      // curto demais
    [InlineData("t434gtr", false)]  // longo demais
    [InlineData("b0uy", false)]     // "0" não é usado pelo Securimage
    public void Criar_ValidaTamanhoECaracteres(string texto, bool valido) =>
        Assert.Equal(valido, CaptchaLido.Criar(texto, Permitidos, 4, 6).Valido);

    /// <summary>
    /// Teste de regressão ja que o pré-processamento e o ONNX acertavam 7 das 14 amostras rotuladas
    /// quando foi implementado. Se uma mudança nos filtros piorar isso, o teste falha.
    /// </summary>
    [Fact]
    public void OnnxSolver_AmostrasRotuladas_NaoRegrideDaTaxaMedida()
    {
        var onnx = Path.Combine(AppContext.BaseDirectory, "onnx");
        using var solver = new OnnxCaptchaSolver(
            Path.Combine(onnx, "common_old.onnx"), Path.Combine(onnx, "charset_old.json"));

        var amostras = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "amostras", "captcha"), "*__*.png");
        var acertos = amostras.Count(arquivo =>
            solver.ResolverCaptcha(File.ReadAllBytes(arquivo)).Texto
            == Path.GetFileNameWithoutExtension(arquivo).Split("__")[0]);

        Assert.True(acertos >= 7, $"Acertou {acertos} de {amostras.Length}; esperado pelo menos 7.");
    }
}
