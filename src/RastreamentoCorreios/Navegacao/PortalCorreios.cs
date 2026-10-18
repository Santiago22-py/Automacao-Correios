using Microsoft.Playwright;

namespace RastreamentoCorreios.Navegacao
{   
    ///<summary>
    /// Classe responsável por navegar no portal dos correios e obter informações de rastreio.
    /// </summary>
    public sealed class PortalCorreios : IAsyncDisposable
    {
        private const string UrlCorreios = "https://rastreamento.correios.com.br/app/index.php";

        //Variáveis privadas
        private readonly bool _headless;
        private readonly float _timeoutMs;
        private string? _srcAntesDaConsulta;

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
            _page = await _browser.NewPageAsync(new() { DeviceScaleFactor = 1 });
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
        /// Pede um novo CAPTCHA pelo botão da página e aguarda a nova imagem carregar.
        /// </summary>
        public async Task RecarregarCaptchaAsync()
        {
            await Pagina.RunAndWaitForResponseAsync(
                () => Pagina.Locator("#captcha_refresh_btn").ClickAsync(),
                resposta => resposta.Url.Contains("securimage_show.php"));

            await AguardarCaptchaCarregadoAsync();
        }


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
        }

        /// <summary>
        /// Preenche o CAPTCHA, clica em Consultar e devolve o corpo (JSON) da resposta de resultado.php.
        /// </summary>
        public async Task<string> ConsultarAsync(string textoCaptcha)
        {   
            _srcAntesDaConsulta = await Pagina.Locator("#captcha_image").GetAttributeAsync("src");
            await Pagina.Locator("#captcha").FillAsync(textoCaptcha);

            var resposta = await Pagina.RunAndWaitForResponseAsync(
                () => Pagina.Locator("#b-pesquisar").ClickAsync(),
                r => r.Url.Contains("resultado.php"));

            return await resposta.TextAsync();
        }

        /// <summary>
    /// Depois de cada consulta, o script da página (refreshCaptcha) troca o src da imagem.
    /// Espera o src mudar e a nova imagem terminar de carregar.
    /// </summary>
    public Task AguardarNovoCaptchaAsync()
        {
            return Pagina.WaitForFunctionAsync(
                "anterior => { const img = document.querySelector('#captcha_image');" +
                " return img.getAttribute('src') !== anterior && img.complete && img.naturalWidth > 0; }",
                _srcAntesDaConsulta);
        }
        

    }    
}