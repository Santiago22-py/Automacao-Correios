using RastreamentoCorreios.Navegacao;

// Verifica se o código de rastreio foi fornecido como argumento
if (args.Length < 1)
{
    Console.WriteLine("Uso: RastreamentoCorreios <codigo-de-rastreamento>");
    return 1;
}

var codigoRastreio = args[0].Trim();

// Cria uma instância da classe PortalCorreios e realiza a navegação e preenchimento do código de rastreio
await using var portalCorreios = new PortalCorreios(headless: false, timeoutMs: 30000);
await portalCorreios.AbrirPaginaAsync();
await portalCorreios.PreencherCodigoRastreioAsync(codigoRastreio);

var imagem = await portalCorreios.CapturarCaptchaAsync(); 

// Salva a imagem do captcha em um arquivo local
Directory.CreateDirectory("captchas");
var caminhoArquivo = Path.Combine("captchas", $"{codigoRastreio}-{DateTime.Now:yyyyMMdd_HHmmss}.png");
await File.WriteAllBytesAsync(caminhoArquivo, imagem);

Console.WriteLine($"Salvando imagem do captcha em: {caminhoArquivo}");
return 0;
