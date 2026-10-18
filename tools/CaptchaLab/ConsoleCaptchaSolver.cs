using RastreamentoCorreios.Captcha;

/// <summary>Solver manual, só para desenvolvimento: salva a imagem e pede que uma pessoa digite o texto.</summary>
internal sealed class ConsoleCaptchaSolver : ICaptchaSolver
{
    public CaptchaLido ResolverCaptcha(byte[] imagemCaptcha)
    {
        var caminho = Path.GetFullPath("captcha-manual.png");
        File.WriteAllBytes(caminho, imagemCaptcha);

        Console.Write($"Abra {caminho} e digite o texto: ");
        var texto = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

        return new CaptchaLido(texto, texto.Length > 0);
    }
}
