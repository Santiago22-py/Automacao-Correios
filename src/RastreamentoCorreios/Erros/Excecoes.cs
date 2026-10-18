namespace RastreamentoCorreios.Erros;

/// <summary>
/// Base das falhas previstas, para o Program poder tratá-las juntas.
/// </summary>
public class RastreamentoException(string mensagem, Exception? interna = null) : Exception(mensagem, interna);

public sealed class CodigoInvalidoException(string codigo)
    : RastreamentoException($"Código de rastreamento inválido: '{codigo}'.");

public sealed class ObjetoNaoEncontradoException(string codigo, string mensagemPortal)
    : RastreamentoException($"Objeto {codigo} não encontrado. Portal: {mensagemPortal}");

public sealed class PortalIndisponivelException(string mensagem, Exception? interna = null)
    : RastreamentoException(mensagem, interna);

public sealed class CaptchaEsgotadoException(int tentativas)
    : RastreamentoException($"CAPTCHA não resolvido após {tentativas} tentativas.");

/// <summary>
/// O portal respondeu, mas sem os campos esperados
/// </summary>
public sealed class RespostaInesperadaException(string mensagem, Exception? interna = null)
    : RastreamentoException(mensagem, interna);
