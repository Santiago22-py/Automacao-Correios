namespace RastreamentoCorreios;

/// <summary>
/// Argumentos da linha de comando já interpretados.
/// </summary>
public sealed record Argumentos(string Codigo, string? ArquivoSaida, OpcoesRastreamento Opcoes)
{
    public const string Uso = """
        Uso: RastreamentoCorreios <codigo> [opções]

          --saida <arquivo>    grava o JSON também no arquivo informado
          --tentativas <n>     máximo de tentativas de CAPTCHA (padrão: 14)
          --intervalo <ms>     intervalo entre tentativas, em ms (padrão: 5000)
          --timeout <ms>       tempo máximo de cada ação no navegador, em ms (padrão: 30000)
          --visivel            abre o navegador visível, para depuração
        """;

    /// <summary>
    /// Interpreta os argumentos; lança uma ArgumentException com uma mensagem para o usuário.
    /// </summary>
    public static Argumentos Ler(string[] args)
    {
        string? codigo = null, arquivoSaida = null;
        var opcoes = new OpcoesRastreamento();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--saida":
                    arquivoSaida = LerValor(args, ref i);
                    break;
                case "--tentativas":
                    opcoes = opcoes with { MaxTentativas = LerInteiroPositivo(args, ref i) };
                    break;
                case "--intervalo":
                    opcoes = opcoes with { IntervaloEntreTentativasMs = LerInteiroPositivo(args, ref i) };
                    break;
                case "--timeout":
                    opcoes = opcoes with { TimeoutMs = LerInteiroPositivo(args, ref i) };
                    break;
                case "--visivel":
                    opcoes = opcoes with { Headless = false };
                    break;
                case var opcao when opcao.StartsWith("--"):
                    throw new ArgumentException($"Opção desconhecida: {opcao}");
                default:
                    if (codigo is not null)
                        throw new ArgumentException("Informe apenas um código de rastreamento.");
                    codigo = args[i];
                    break;
            }
        }

        return new Argumentos(
            codigo ?? throw new ArgumentException("Informe o código de rastreamento."), arquivoSaida, opcoes);
    }

    private static string LerValor(string[] args, ref int i) =>
        ++i < args.Length ? args[i] : throw new ArgumentException($"A opção {args[i - 1]} precisa de um valor.");

    private static int LerInteiroPositivo(string[] args, ref int i)
    {
        var opcao = args[i];
        var texto = LerValor(args, ref i);
        return int.TryParse(texto, out var numero) && numero > 0
            ? numero
            : throw new ArgumentException($"A opção {opcao} precisa de um número inteiro positivo (recebido: '{texto}').");
    }
}
