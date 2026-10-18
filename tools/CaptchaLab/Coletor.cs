using RastreamentoCorreios.Navegacao;

internal static class Coletor
{
    public static async Task ExecutarAsync(string pasta, int quantidade)
    {
        // Define a quantidade de captchas a serem capturados e o intervalo entre cada captura
        var intervalo = TimeSpan.FromSeconds(5);
        Directory.CreateDirectory(pasta);

        // Cria uma instância da classe PortalCorreios e realiza a navegação para capturar os captchas
        await using var portal = new PortalCorreios(headless: true, timeoutMs: 30000);
        await portal.AbrirPaginaAsync();

        // Captura os captchas em um loop, salvando cada imagem em um arquivo local
        for (var i = 1; i <= quantidade; i++)
        {
            var imagem = await portal.CapturarCaptchaAsync();
            var caminho = Path.Combine(pasta, $"{i:D2}.png");
            await File.WriteAllBytesAsync(caminho, imagem);
            Console.WriteLine($"[{i}/{quantidade}] {caminho}");

            // Aguarda o intervalo definido antes de capturar o próximo captcha recarregando a página para gerar um novo captcha
            if (i < quantidade)
            {
                await Task.Delay(intervalo);
                await portal.RecarregarCaptchaAsync();
            }
        }
    }
}
