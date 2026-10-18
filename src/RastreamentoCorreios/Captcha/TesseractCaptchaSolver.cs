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
        public TesseractCaptchaSolver(string pathTessdata, string caracteresPermitidos = "abcdefghijklmnopqrstuvwxyz0123456789", int tamanhoMaximo = 6, int tamanhoMinimo = 4)
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
            using var imagemProcessada = PreProcessar(original, pastaDebug);
            using var pagina = _engine.Process(imagemProcessada, PageSegMode.SingleWord);

            var texto = new string(pagina.GetText().Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();

            return new CaptchaLido(texto, isValido(texto));
        }

        /// <summary>
        /// Pré-processa a imagem do CAPTCHA para melhorar a precisão do OCR.
        /// </summary>
        /// <param name="original">A imagem original do CAPTCHA.</param>
        /// <param name="pastaDebug">O caminho da pasta para salvar as imagens de depuração.</param>
        private static Pix PreProcessar(Pix original, string? pastaDebug)
        {
            Salvar(original, pastaDebug, "0-original");

            // Converte a imagem para tons de cinza para melhorar a precisão do OCR
            using var imagemCinza = original.ConvertRGBToGray();
            Salvar(imagemCinza, pastaDebug, "1-cinza");

            // Remove pequenos ruídos da imagem binarizada para melhorar a precisão do OCR
            var imagemBinarizada = imagemCinza.BinarizeOtsuAdaptiveThreshold(imagemCinza.Width, imagemCinza.Height, 0, 0, 0f);
            Salvar(imagemBinarizada, pastaDebug, "2-binarizada");

            return imagemBinarizada;
        }

        // Retorna se o texto do CAPTCHA é válido com base nos critérios de tamanho mínimo e máximo e nos caracteres permitidos
        private bool isValido(string texto)
        {
            return texto.Length >= _tamanhoMinimo && texto.Length <= _tamanhMaximo && texto.All(_caracteresPermtidos.Contains);
        }
        
        // Salva a imagem em um arquivo PNG na pasta de depuração, se fornecida
        private static void Salvar(Pix imagem, string? pastaDebug, string etapa)
        {
            if (pastaDebug is null) return;

            Directory.CreateDirectory(pastaDebug);
            imagem.Save(Path.Combine(pastaDebug, $"{etapa}.png"), ImageFormat.Png);
        }

        public void Dispose()
        {
            _engine.Dispose();
        }


    }
}

