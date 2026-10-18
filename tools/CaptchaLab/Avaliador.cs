using RastreamentoCorreios.Captcha;

internal static class Avaliador
{
    // Arquivos rotulados seguem o padrão "texto__NN.png"; os não rotulados são ignorados.
    public static void Executar(string pastaAmostras, string pastaDebug, Func<byte[], string?, CaptchaLido> resolver)
    {
        var rotuladas = Directory.GetFiles(pastaAmostras, "*__*.png").Order().ToArray();
        if (rotuladas.Length == 0)
        {
            Console.WriteLine($"Nenhuma imagem rotulada em {pastaAmostras}.");
            return;
        }

        var acertos = 0;

        foreach (var arquivo in rotuladas)
        {
            var nome = Path.GetFileNameWithoutExtension(arquivo);
            var esperado = nome.Split("__")[0].ToLowerInvariant();

            var lido = resolver(File.ReadAllBytes(arquivo), Path.Combine(pastaDebug, nome));
            var acertou = lido.Valido && lido.Texto == esperado;
            if (acertou) acertos++;

            var situacao = acertou ? "OK  " : lido.Valido ? "ERRO" : "INV ";
            Console.WriteLine($"{situacao} esperado '{esperado}', lido '{lido.Texto}'");
        }

        Console.WriteLine($"\nAcertos: {acertos}/{rotuladas.Length} ({100.0 * acertos / rotuladas.Length:F1}%)");
    }
}
