using System.Text;
using RastreamentoCorreios;
using RastreamentoCorreios.Captcha;
using RastreamentoCorreios.Erros;
using RastreamentoCorreios.Saida;

Console.OutputEncoding = Encoding.UTF8;

// Se o programa for chamado com argumentos, assume que é para rastrear um código de rastreio; caso contrário, assume que é para rodar o laboratório de CAPTCHA.
if (args.Length == 0)
{
    Console.Error.WriteLine("Uso: RastreamentoCorreios <codigo-de-rastreamento>");
    return 1;
}

using var solver = new TesseractCaptchaSolver(Path.Combine(AppContext.BaseDirectory, "tessdata"));
var rastreador = new Rastreador(solver, new OpcoesRastreamento(), Console.Error);

try
{
    var rastreamento = await rastreador.RastrearAsync(args[0]);
    Console.WriteLine(JsonSaida.Serializar(rastreamento));
    return 0;
}
catch (RastreamentoException ex)
{
    Console.Error.WriteLine($"Erro: {ex.Message}");
    return 2;
}
