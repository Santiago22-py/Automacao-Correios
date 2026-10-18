using RastreamentoCorreios.Navegacao;
using RastreamentoCorreios.Captcha;
using RastreamentoCorreios.Saida;
using RastreamentoCorreios.Extracao;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var pasta = Path.Combine("amostras", "captcha");

switch (args.FirstOrDefault())
{
    case "coletar":
        await Coletor.ExecutarAsync(pasta, quantidade: 20);
        break;
    case "avaliar":
        Avaliador.Executar(pasta, Path.Combine("amostras", "debug"));
        break;
    case "consultar" when args.Length > 1:
        await ConsultarManualAsync(args[1]);
        break;
    case "mapear" when args.Length > 1:
        var json = await File.ReadAllTextAsync(args[1]);
        Console.WriteLine(RastreamentoMapper.LerErro(json) is { } erro
            ? $"Portal respondeu erro: {erro}"
            : JsonSaida.Serializar(RastreamentoMapper.Mapear(json)));
        break;
    default:
        Console.Error.WriteLine("Uso: CaptchaLab coletar | avaliar");
        return 1;
}

/// <summary>
/// Consulta manual, para gerar um arquivo de resposta JSON de exemplo.
/// </summary>
static async Task ConsultarManualAsync(string codigo)
{
    ICaptchaSolver solver = new ConsoleCaptchaSolver();

    await using var portal = new PortalCorreios(headless: true, timeoutMs: 30000);
    await portal.AbrirPaginaAsync();
    await portal.PreencherCodigoRastreioAsync(codigo);

    var lido = solver.ResolverCaptcha(await portal.CapturarCaptchaAsync());
    var json = await portal.ConsultarAsync(lido.Texto);

    Directory.CreateDirectory("amostras");
    await File.WriteAllTextAsync(Path.Combine("amostras", "resposta-exemplo.json"), json);
    Console.WriteLine(json);
}

return 0;
