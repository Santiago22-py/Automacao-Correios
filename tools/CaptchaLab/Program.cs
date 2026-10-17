using RastreamentoCorreios.Navegacao;

// Define a quantidade de captchas a serem capturados e o intervalo entre cada captura
const int Quantidade = 20;
var intervalo = TimeSpan.FromSeconds(5);
var pasta = Path.Combine("amostras", "captcha");
Directory.CreateDirectory(pasta);

// Cria uma instância da classe PortalCorreios e realiza a navegação para capturar os captchas
await using var portal = new PortalCorreios(headless: true, timeoutMs: 30000);
await portal.AbrirPaginaAsync();

// Captura os captchas em um loop, salvando cada imagem em um arquivo local
for (var i = 1; i <= Quantidade; i++)
{
    var imagem = await portal.CapturarCaptchaAsync();
    var caminho = Path.Combine(pasta, $"{i:D2}.png");
    await File.WriteAllBytesAsync(caminho, imagem);
    Console.WriteLine($"[{i}/{Quantidade}] {caminho}");

    // Aguarda o intervalo definido antes de capturar o próximo captcha recarregando a página para gerar um novo captcha
    if (i < Quantidade)
    {
        await Task.Delay(intervalo);
        await portal.RecarregarCaptchaAsync();
    }
}
