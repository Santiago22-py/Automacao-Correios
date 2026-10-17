using Microsoft.Playwright;

namespace RastreamentoCorreios.Navegacao
{   
    ///<summary>
    /// Classe responsável por navegar no portal dos correios e obter informações de rastreio.
    /// Selada para impedir herança e garantir encapsulamento.
    /// </summary>
    public sealed class PortalCorreios : IAsyncDisposable
    {
        private const string UrlCorreios = "https://rastreamento.correios.com.br/app/index.php";

        //Variáveis privadas
        private readonly bool _headless;
        private readonly float _timeoutMs;

        //Variaveis privadas para o Playwright
        private IPlaywright? _playwright;
        private IBrowser? _browser;  
        private IPage? _page;


        /// <summary>
        /// Construtor da classe PortalCorreios.
        /// </summary>
        /// <param name="headless"> Indica se o navegador deve ser executado em modo headless. </param>
        /// <param name="timeoutMs"> Tempo limite para a operação em milissegundos. </param>
        public PortalCorreios(bool headless = true, float timeoutMs = 30000)
        {
            _headless = headless;
            _timeoutMs = timeoutMs;
        }

        /// <summary>
        /// Abre a página do portal dos correios e aguarda o carregamento do captcha.
        /// </summary>
        public async Task AbrirPaginaAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = _headless });
            _page = await _browser.NewPageAsync();
            _page.SetDefaultTimeout(_timeoutMs);

            await _page.GotoAsync(UrlCorreios, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await AguardarCaptchaCarregadoAsync();
        }

        /// <summary>
        /// Preenche o campo de código de rastreio na página do portal dos correios.
        /// </summary>
        /// <param name="codigoRastreio"></param>
        public Task PreencherCodigoRastreioAsync(string codigoRastreio)
        {
            return Pagina.Locator("#objeto").FillAsync(codigoRastreio);
        }

        /// <summary>
        ///  Captura a imagem do captcha exibido na página do portal dos correios.
        ///  Aqui usei um screenshot ao inves de baixar a imagem diretamente
        ///  Pois tentar baixar diretamente gera um captcha novo e totalmente diferente
        /// </summary>
        public Task<byte[]> CapturarCaptchaAsync()
        {
            return Pagina.Locator("#captcha_image").ScreenshotAsync();
        }

        /// <summary>
        /// Aguarda até que o captcha seja carregado na página do portal dos correios.
        /// </summary>
        private Task AguardarCaptchaCarregadoAsync()
        {
            return Pagina.WaitForFunctionAsync(
                "() => { const img = document.querySelector('#captcha_image');" +
                " return img !== null && img.complete && img.naturalWidth > 0; }"
            );
        }

        /// <summary>
        /// Fecha o navegador e libera os recursos utilizados pelo Playwright.
        /// </summary>
        private IPage Pagina =>
        _page ?? throw new InvalidOperationException("Chame AbrirAsync antes de usar o portal.");

        /// <summary>
        /// Implementação do método DisposeAsync para liberar recursos de forma assíncrona.
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {

            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }

            if (_playwright != null)
            {
                _playwright.Dispose();
                _playwright = null;
            }

            if (_page != null)
            {
                await _page.CloseAsync();
                _page = null;
            }
        }
    }    
}