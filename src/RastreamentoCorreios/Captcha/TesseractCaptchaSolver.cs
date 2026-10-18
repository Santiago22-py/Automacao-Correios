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

            return new CaptchaLido(texto, isValido(texto));
        }

        /// <summary>
        /// Pré-processa a imagem do CAPTCHA para melhorar a precisão do OCR.
        /// </summary>
        /// <param name="imagemPng">A imagem original do CAPTCHA.</param>
        /// <param name="pastaDebug">O caminho da pasta para salvar as imagens de depuração.</param>
        private static Pix PreProcessar(byte[] imagemPng, string? pastaDebug)
        {
            var cinza = FiltrosImagem.CarregarEmCinza(imagemPng);

            var tinta = FiltrosImagem.Binarizar(cinza, FiltrosImagem.LimiarOtsu(cinza));
            Salvar(tinta, pastaDebug, "1-binarizada");

            tinta = FiltrosImagem.RemoverComponentesPequenos(tinta, tamanhoMinimo: 150);
            Salvar(tinta, pastaDebug, "2-sem-pontos");

            tinta = FiltrosImagem.Abrir(tinta, raio: 2);
            Salvar(tinta, pastaDebug, "3-sem-linhas-finas");

            tinta = FiltrosImagem.RemoverComponentesPequenos(tinta, tamanhoMinimo: 150);
            Salvar(tinta, pastaDebug, "4-sem-fragmentos");

            tinta = FiltrosImagem.RecortarComMargem(tinta, margem: 15);
            Salvar(tinta, pastaDebug, "5-recortada");

            return FiltrosImagem.ParaPix(tinta);
        }

        // Retorna se o texto do CAPTCHA é válido com base nos critérios de tamanho mínimo e máximo e nos caracteres permitidos
        private bool isValido(string texto)
        {
            return texto.Length >= _tamanhoMinimo && texto.Length <= _tamanhMaximo && texto.All(_caracteresPermtidos.Contains);
        }
        
        // Salva a imagem em um arquivo PNG na pasta de depuração, se fornecida
        private static void Salvar(bool[,] tinta, string? pastaDebug, string etapa)
        {
            if (pastaDebug is null) return;

            Directory.CreateDirectory(pastaDebug);
            using var pix = FiltrosImagem.ParaPix(tinta);
            pix.Save(Path.Combine(pastaDebug, $"{etapa}.png"), ImageFormat.Png);
        }

        public void Dispose()
        {
            _engine.Dispose();
        }
    }
}

