using System.Text;
using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace RastreamentoCorreios.Captcha;

/// <summary>
/// Solver com o modelo de CAPTCHA do projeto ddddocr, executado pela ONNX Runtime.
/// O modelo é uma rede neural com saída CTC: para cada fatia vertical da imagem, uma pontuação por caractere.
/// </summary>
public sealed class OnnxCaptchaSolver : ICaptchaSolver, IDisposable
{
    private const int AlturaEntrada = 64; // altura das imagens com que o modelo foi treinado
    private const int IndiceBranco = 0;   // "branco" do CTC: nenhuma letra nesta fatia

    private readonly InferenceSession _sessao;
    private readonly string _nomeEntrada;
    private readonly string[] _caracteres;
    private readonly int[] _indicesPermitidos;
    private readonly string _caracteresPermitidos;
    private readonly int _tamanhoMinimo;
    private readonly int _tamanhoMaximo;

    public OnnxCaptchaSolver(
        string caminhoModelo,
        string caminhoCaracteres,
        string caracteresPermitidos = "abcdefghklmnprstuvwyz23456789",
        int tamanhoMinimo = 4,
        int tamanhoMaximo = 6)
    {
        _caracteresPermitidos = caracteresPermitidos;
        _tamanhoMinimo = tamanhoMinimo;
        _tamanhoMaximo = tamanhoMaximo;

        _caracteres = JsonSerializer.Deserialize<string[]>(File.ReadAllText(caminhoCaracteres))
                      ?? throw new InvalidDataException($"Lista de caracteres vazia: {caminhoCaracteres}");

        // O modelo conhece ~8 mil caracteres (inclusive chineses); só deixamos escolher o branco e os permitidos.
        _indicesPermitidos = Enumerable.Range(0, _caracteres.Length)
            .Where(i => i == IndiceBranco
                        || (_caracteres[i].Length == 1 && caracteresPermitidos.Contains(char.ToLowerInvariant(_caracteres[i][0]))))
            .ToArray();

        // O modelo declara a saída como [1, seqlen] mas devolve [seqlen, 1, classes]; o aviso disso é só ruído.
        using var opcoes = new SessionOptions { LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR };
        _sessao = new InferenceSession(caminhoModelo, opcoes);
        _nomeEntrada = _sessao.InputMetadata.Keys.First();
    }

    public CaptchaLido ResolverCaptcha(byte[] imagemCaptcha) => ResolverCaptcha(imagemCaptcha, pastaDebug: null);

    public CaptchaLido ResolverCaptcha(byte[] imagemCaptcha, string? pastaDebug)
    {
        var tinta = FiltrosImagem.Limpar(imagemCaptcha, pastaDebug);

        using var resultados = _sessao.Run([NamedOnnxValue.CreateFromTensor(_nomeEntrada, MontarEntrada(tinta))]);
        var texto = Decodificar(resultados.First().AsTensor<float>());

        return CaptchaLido.Criar(texto, _caracteresPermitidos, _tamanhoMinimo, _tamanhoMaximo);
    }

    /// <summary>
    /// Reduz a máscara para 64 px de altura, mantendo a proporção, por média de área:
    /// cada pixel de saída é a fração de fundo do bloco que ele cobre (0 = tinta, 1 = fundo).
    /// </summary>
    private static DenseTensor<float> MontarEntrada(bool[,] tinta)
    {
        int altura = tinta.GetLength(0), largura = tinta.GetLength(1);
        var larguraEntrada = largura * AlturaEntrada / altura;
        var entrada = new DenseTensor<float>([1, 1, AlturaEntrada, larguraEntrada]);

        for (var y = 0; y < AlturaEntrada; y++)
        for (var x = 0; x < larguraEntrada; x++)
        {
            int y0 = y * altura / AlturaEntrada, y1 = Math.Max(y0 + 1, (y + 1) * altura / AlturaEntrada);
            int x0 = x * largura / larguraEntrada, x1 = Math.Max(x0 + 1, (x + 1) * largura / larguraEntrada);

            int fundo = 0, total = 0;
            for (var yy = y0; yy < y1; yy++)
            for (var xx = x0; xx < x1; xx++)
            {
                total++;
                if (!tinta[yy, xx]) fundo++;
            }

            entrada[0, 0, y, x] = (float)fundo / total;
        }

        return entrada;
    }

    /// <summary>
    /// Decodificação CTC gulosa: em cada fatia, o caractere permitido mais provável;
    /// repetições consecutivas viram um só e o branco é descartado.
    /// </summary>
    private string Decodificar(Tensor<float> saida)
    {
        var texto = new StringBuilder();
        var anterior = IndiceBranco;

        for (var fatia = 0; fatia < saida.Dimensions[0]; fatia++)
        {
            var melhor = IndiceBranco;
            foreach (var indice in _indicesPermitidos)
                if (saida[fatia, 0, indice] > saida[fatia, 0, melhor]) melhor = indice;

            if (melhor != anterior && melhor != IndiceBranco)
                texto.Append(char.ToLowerInvariant(_caracteres[melhor][0]));

            anterior = melhor;
        }

        return texto.ToString();
    }

    public void Dispose() => _sessao.Dispose();
}
