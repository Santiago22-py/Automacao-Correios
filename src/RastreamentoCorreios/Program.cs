using System.Text;
using RastreamentoCorreios;
using RastreamentoCorreios.Captcha;
using RastreamentoCorreios.Erros;
using RastreamentoCorreios.Saida;

Console.OutputEncoding = Encoding.UTF8;

Argumentos argumentos;
try // Tenta interpretar os argumentos, se falhar, mostra a mensagem de uso e sai com código 1.
{
    argumentos = Argumentos.Ler(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(Argumentos.Uso);
    return 1;
}

// O ONNX + pré-processamento é mais mais preciso que o Tesseract, então é o padrão.
var pastaOnnx = Path.Combine(AppContext.BaseDirectory, "onnx");
using var solver = new OnnxCaptchaSolver(
    Path.Combine(pastaOnnx, "common_old.onnx"), Path.Combine(pastaOnnx, "charset_old.json"));
var rastreador = new Rastreador(solver, argumentos.Opcoes, Console.Error);

// Tenta rastrear e gravar o resultado, captura erros de rastreamento e de gravação do arquivo de saída.
try
{
    var json = JsonSaida.Serializar(await rastreador.RastrearAsync(argumentos.Codigo));
    Console.WriteLine(json);

    if (argumentos.ArquivoSaida is not null)
    {
        await File.WriteAllTextAsync(argumentos.ArquivoSaida, json);
        Console.Error.WriteLine($"Resultado salvo em {Path.GetFullPath(argumentos.ArquivoSaida)}");
    }

    return 0;
}
catch (RastreamentoException ex)
{
    Console.Error.WriteLine($"Erro: {ex.Message}");
    return 2;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Erro ao gravar o arquivo de saída: {ex.Message}");
    return 3;
}
