namespace RastreamentoCorreios.Captcha
{
    /// <summary>
    /// Representa o resultado da leitura de um captcha, contendo o texto lido e um indicador de sucesso.
    /// Selada para garantir que não possa ser herdada
    /// </summary>
    /// <param name="Texto"> O texto lido do captcha. </param>
    /// <param name="Sucesso"> Indicador de sucesso na leitura do captcha. </param>
    public sealed record CaptchaLido(string Texto, bool Valido)
    {
        /// <summary>
        /// Marca como válido só o texto com tamanho dentro da faixa e caracteres permitidos.
        /// </summary>
        public static CaptchaLido Criar(string texto, string caracteresPermitidos, int tamanhoMinimo, int tamanhoMaximo) =>
            new(texto, texto.Length >= tamanhoMinimo && texto.Length <= tamanhoMaximo && texto.All(caracteresPermitidos.Contains));
    }

    public interface ICaptchaSolver
    {
        CaptchaLido ResolverCaptcha(byte[] imagemCaptcha);
    }



}
