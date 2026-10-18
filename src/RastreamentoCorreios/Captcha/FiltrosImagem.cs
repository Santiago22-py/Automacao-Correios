using Tesseract;

namespace RastreamentoCorreios.Captcha;

/// <summary>
/// Filtros de pré-processamento do CAPTCHA. Trabalham sobre uma máscara binária
/// em que true é tinta (pixel escuro) e false< é fundo.
/// </summary>
internal static class FiltrosImagem
{
    /// <summary>
    /// Decodifica o PNG, converte para cinza (0 = preto, 255 = branco) e copia os pixels para uma matriz.
    /// </summary>
    public static unsafe byte[,] CarregarEmCinza(byte[] png)
    {
        using var original = Pix.LoadFromMemory(png);
        using var cinza = original.ConvertRGBToGray();
        var dados = cinza.GetData();
        var resultado = new byte[cinza.Height, cinza.Width];

        for (var y = 0; y < cinza.Height; y++)
        {
            var linha = (uint*)dados.Data + y * dados.WordsPerLine;
            for (var x = 0; x < cinza.Width; x++)
                resultado[y, x] = (byte)PixData.GetDataByte(linha, x);
        }

        return resultado;
    }

    /// <summary>
    /// Método de Otsu: testa todos os limiares possíveis e escolhe o que melhor separa
    /// os pixels em dois grupos (escuros e claros), maximizando a variância entre eles.
    /// </summary>
    public static int LimiarOtsu(byte[,] cinza)
    {
        var histograma = new int[256];
        foreach (var tom in cinza) histograma[tom]++;

        var total = cinza.Length;
        double somaTotal = 0;
        for (var tom = 0; tom < 256; tom++) somaTotal += tom * (double)histograma[tom];

        double somaEscuros = 0, melhorVariancia = -1;
        int qtdEscuros = 0, melhorLimiar = 0;

        for (var limiar = 0; limiar < 256; limiar++)
        {
            qtdEscuros += histograma[limiar];
            if (qtdEscuros == 0) continue;

            var qtdClaros = total - qtdEscuros;
            if (qtdClaros == 0) break;

            somaEscuros += limiar * (double)histograma[limiar];
            var mediaEscuros = somaEscuros / qtdEscuros;
            var mediaClaros = (somaTotal - somaEscuros) / qtdClaros;

            var variancia = (double)qtdEscuros * qtdClaros * Math.Pow(mediaEscuros - mediaClaros, 2);
            if (variancia > melhorVariancia)
            {
                melhorVariancia = variancia;
                melhorLimiar = limiar;
            }
        }

        return melhorLimiar;
    }

    public static bool[,] Binarizar(byte[,] cinza, int limiar)
    {
        var tinta = new bool[cinza.GetLength(0), cinza.GetLength(1)];
        for (var y = 0; y < cinza.GetLength(0); y++)
            for (var x = 0; x < cinza.GetLength(1); x++)
                tinta[y, x] = cinza[y, x] <= limiar;
        return tinta;
    }

    /// <summary>
    /// Agrupa pixels de tinta que se tocam (inclusive na diagonal) e apaga os grupos
    /// com menos de <paramref name="tamanhoMinimo"/> pixels: os "+" e fragmentos soltos.
    /// </summary>
    public static bool[,] RemoverComponentesPequenos(bool[,] tinta, int tamanhoMinimo)
    {
        int altura = tinta.GetLength(0), largura = tinta.GetLength(1);
        var visitado = new bool[altura, largura];
        var resultado = new bool[altura, largura];

        for (var y = 0; y < altura; y++)
        for (var x = 0; x < largura; x++)
        {
            if (!tinta[y, x] || visitado[y, x]) continue;

            // Busca em largura: coleta todos os pixels de tinta conectados a (x, y).
            var componente = new List<(int Y, int X)>();
            var fila = new Queue<(int Y, int X)>();
            fila.Enqueue((y, x));
            visitado[y, x] = true;

            while (fila.Count > 0)
            {
                var (cy, cx) = fila.Dequeue();
                componente.Add((cy, cx));

                for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    int ny = cy + dy, nx = cx + dx;
                    if (ny < 0 || ny >= altura || nx < 0 || nx >= largura) continue;
                    if (!tinta[ny, nx] || visitado[ny, nx]) continue;

                    visitado[ny, nx] = true;
                    fila.Enqueue((ny, nx));
                }
            }

            if (componente.Count < tamanhoMinimo) continue;
            foreach (var (py, px) in componente) resultado[py, px] = true;
        }

        return resultado;
    }

    /// <summary>
    /// Abertura morfológica: erosão seguida de dilatação. Estruturas mais finas que a
    /// janela (2 * raio + 1 pixels) somem na erosão e não voltam; traços grossos voltam
    /// ao tamanho original na dilatação.
    /// </summary>
    public static bool[,] Abrir(bool[,] tinta, int raio) => Dilatar(Erodir(tinta, raio), raio);

    /// <summary>Um pixel continua tinta só se toda a janela ao redor dele for tinta.</summary>
    private static bool[,] Erodir(bool[,] tinta, int raio) => AplicarJanela(tinta, raio, exigeTodos: true);

    /// <summary>Um pixel vira tinta se qualquer pixel da janela ao redor for tinta.</summary>
    private static bool[,] Dilatar(bool[,] tinta, int raio) => AplicarJanela(tinta, raio, exigeTodos: false);

    private static bool[,] AplicarJanela(bool[,] tinta, int raio, bool exigeTodos)
    {
        int altura = tinta.GetLength(0), largura = tinta.GetLength(1);
        var resultado = new bool[altura, largura];

        for (var y = 0; y < altura; y++)
        for (var x = 0; x < largura; x++)
        {
            var valor = exigeTodos;
            for (var dy = -raio; dy <= raio && valor == exigeTodos; dy++)
            for (var dx = -raio; dx <= raio && valor == exigeTodos; dx++)
            {
                int ny = y + dy, nx = x + dx;
                var vizinho = ny >= 0 && ny < altura && nx >= 0 && nx < largura && tinta[ny, nx];
                if (vizinho != exigeTodos) valor = !exigeTodos;
            }
            resultado[y, x] = valor;
        }

        return resultado;
    }

    /// <summary>
    /// Recorta a menor caixa que contém toda a tinta e adiciona uma margem branca.
    /// </summary>
    public static bool[,] RecortarComMargem(bool[,] tinta, int margem)
    {
        int altura = tinta.GetLength(0), largura = tinta.GetLength(1);
        int minY = altura, maxY = -1, minX = largura, maxX = -1;

        for (var y = 0; y < altura; y++)
            for (var x = 0; x < largura; x++)
            {
                if (!tinta[y, x]) continue;
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
            }

        if (maxY < 0) return tinta; // nada sobrou: devolve como está, o OCR vai falhar e a validação descarta

        var resultado = new bool[maxY - minY + 1 + 2 * margem, maxX - minX + 1 + 2 * margem];
        for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
                resultado[y - minY + margem, x - minX + margem] = tinta[y, x];

        return resultado;
    }

    /// <summary>
    /// Converte a máscara numa imagem de 8 bits do Leptonica: tinta preta sobre fundo branco.
    /// </summary>
    public static unsafe Pix ParaPix(bool[,] tinta)
    {
        var pix = Pix.Create(tinta.GetLength(1), tinta.GetLength(0), 8);
        var dados = pix.GetData();

        for (var y = 0; y < pix.Height; y++)
        {
            var linha = (uint*)dados.Data + y * dados.WordsPerLine;
            for (var x = 0; x < pix.Width; x++)
                PixData.SetDataByte(linha, x, tinta[y, x] ? 0u : 255u);
        }

        return pix;
    }

    /// <summary>
    /// Limpeza comum aos solvers: binariza e remove pontos, linhas finas e fragmentos soltos.
    /// </summary>
    public static bool[,] Limpar(byte[] png, string? pastaDebug)
    {
        var cinza = CarregarEmCinza(png);

        var tinta = Binarizar(cinza, LimiarOtsu(cinza));
        SalvarDebug(tinta, pastaDebug, "1-binarizada");

        tinta = RemoverComponentesPequenos(tinta, tamanhoMinimo: 150);
        SalvarDebug(tinta, pastaDebug, "2-sem-pontos");

        tinta = Abrir(tinta, raio: 2);
        SalvarDebug(tinta, pastaDebug, "3-sem-linhas-finas");

        tinta = RemoverComponentesPequenos(tinta, tamanhoMinimo: 150);
        SalvarDebug(tinta, pastaDebug, "4-sem-fragmentos");

        return tinta;
    }

    // Salva a imagem de debug
    public static void SalvarDebug(bool[,] tinta, string? pastaDebug, string etapa)
    {
        if (pastaDebug is null) return;

        Directory.CreateDirectory(pastaDebug);
        using var pix = ParaPix(tinta);
        pix.Save(Path.Combine(pastaDebug, $"{etapa}.png"), ImageFormat.Png);
    }
}

