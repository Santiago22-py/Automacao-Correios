using System.Text.Encodings.Web;
using System.Text.Json;

namespace RastreamentoCorreios.Saida;

/// <summary>
/// Serialização de saída em JSON, com indentação e sem escapar caracteres especiais.
/// </summary>
public static class JsonSaida
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        // Mantém caracteres especiais legiveis
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Serializar<T>(T valor) => JsonSerializer.Serialize(valor, Opcoes);
}
