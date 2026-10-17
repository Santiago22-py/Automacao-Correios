namespace RastreamentoCorreios.Captcha
{
    /// <summary>
    /// Representa o resultado da leitura de um captcha, contendo o texto lido e um indicador de sucesso.
    /// Selada para garantir que não possa ser herdada
    /// </summary>
    /// <param name="Texto"> O texto lido do captcha. </param>
    /// <param name="Sucesso"> Indicador de sucesso na leitura do captcha. </param>
    public sealed record CaptchaLido(string Texto, bool Sucesso);

    public interface ICaptchaSolver
    {
        CaptchaLido ResolverCaptcha(byte[] imagemCaptcha);
    }
}
