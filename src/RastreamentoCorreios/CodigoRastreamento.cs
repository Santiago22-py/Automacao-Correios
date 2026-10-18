using System.Text.RegularExpressions;
using RastreamentoCorreios.Erros;

namespace RastreamentoCorreios;

/// <summary>
/// Validação local do código, antes de qualquer acesso ao portal.
/// Assim evitando enviar codigos invalidos
/// </summary>
public static class CodigoRastreamento
{
    // Regex para validar o formato do código de rastreio
    private static readonly Regex Formato = new("^[A-Z]{2}[0-9]{9}[A-Z]{2}$");
    private static readonly int[] Pesos = [8, 6, 4, 2, 3, 5, 9, 7];

    /// <summary>
    /// Devolve o código em maiúsculas, ou lança uma CodigoInvalidoException />.
    /// </summary>
    public static string Validar(string codigo)
    {   
        // Normaliza o código, removendo espaços e convertendo para maiúsculas
        var normalizado = codigo.Trim().ToUpperInvariant();
        if (!Formato.IsMatch(normalizado) || !DigitoVerificadorConfere(normalizado))
            throw new CodigoInvalidoException(codigo);

        return normalizado;
    }

    // Verifica se o dígito verificador do código de rastreio confere com o esperado.
    private static bool DigitoVerificadorConfere(string codigo)
    {
        var soma = 0;
        for (var i = 0; i < Pesos.Length; i++)
            soma += (codigo[2 + i] - '0') * Pesos[i];

        var resto = soma % 11;
        var esperado = resto switch { 0 => 5, 1 => 0, _ => 11 - resto };
        return codigo[10] - '0' == esperado;
    }
}
