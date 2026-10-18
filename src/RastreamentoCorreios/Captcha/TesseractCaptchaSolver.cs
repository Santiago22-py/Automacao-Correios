using Tesseract;

namespace RastreamentoCorreios.Captcha
{
    public sealed class TesseractCaptchaSolver: ICaptchaSolver, IDisposable
    {
        // Variáveis privadas para o Tesseract
        private readonly TesseractEngine _engine;
        private readonly string _caracteresPermtidos;
        private readonly int _tamanhMaximo;
        private readonly int _tamanhoMinimo;

        /// <summary>
        /// Construtor da classe TesseractCaptchaSolver.
        /// </summary>
        /// <param name="pathTessdata">Caminho para os dados de treinamento do Tesseract.</param>
        /// <param name="caracteresPermitidos">Caracteres permitidos no CAPTCHA.</param>
        /// <param name="tamanhoMaximo">Tamanho máximo do CAPTCHA.</param>
        /// <param name="tamanhoMinimo">Tamanho mínimo do CAPTCHA.</param>
        public TesseractCaptchaSolver(string pathTessdata, string caracteresPermitidos = "abcdefghklmnprstuvwyz23456789", int tamanhoMaximo = 6, int tamanhoMinimo = 4)
        {
            _caracteresPermtidos = caracteresPermitidos;
            _tamanhMaximo = tamanhoMaximo;
            _tamanhoMinimo = tamanhoMinimo;

            // Inicializa o Tesseract com o caminho para os dados de treinamento
            _engine = new TesseractEngine(pathTessdata, "eng", EngineMode.Default);
            _engine.SetVariable("tessedit_char_whitelist", _caracteresPermtidos);
        }

        /// <summary>
        /// Resolve o CAPTCHA usando o Tesseract.
        /// </summary>
        /// <param name="imagemCaptcha">Imagem do CAPTCHA a ser resolvido.</param>
        /// <returns>O texto do CAPTCHA.</returns>
        public CaptchaLido ResolverCaptcha(byte[] imagemCaptcha)
        {
            return ResolverCaptcha(imagemCaptcha, pastaDebug: null);
        }

        public CaptchaLido ResolverCaptcha(byte[] imagemCaptcha, string? pastaDebug)
        {   
            // Carrega a imagem do CAPTCHA na memória e processa com o Tesseract
            using var original = Pix.LoadFromMemory(imagemCaptcha);
            using var imagemProcessada = PreProcessar(imagemCaptcha, pastaDebug);
            using var pagina = _engine.Process(imagemProcessada, PageSegMode.SingleLine);

            var texto = new string(pagina.GetText().Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();

            return CaptchaLido.Criar(texto, _caracteresPermtidos, _tamanhoMinimo, _tamanhMaximo);
        }

        /// <summary>
        /// Pré-processa a imagem do CAPTCHA para melhorar a precisão do OCR.
        /// </summary>
        /// <param name="imagemPng">A imagem original do CAPTCHA.</param>
        /// <param name="pastaDebug">O caminho da pasta para salvar as imagens de depuração.</param>
        private static Pix PreProcessar(byte[] imagemPng, string? pastaDebug)
        {
            var tinta = FiltrosImagem.Limpar(imagemPng, pastaDebug);
            tinta = FiltrosImagem.RecortarComMargem(tinta, margem: 15);
            FiltrosImagem.SalvarDebug(tinta, pastaDebug, "5-recortada");
            return FiltrosImagem.ParaPix(tinta);
        }

        public void Dispose()
        {
            _engine.Dispose();
        }
    }
}

