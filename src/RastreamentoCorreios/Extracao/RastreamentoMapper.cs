using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using RastreamentoCorreios.Erros;
using RastreamentoCorreios.Modelos;
using RastreamentoCorreios.Modelos.Portal;

namespace RastreamentoCorreios.Extracao;

public static class RastreamentoMapper
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Devolve a mensagem de erro do portal ou null se a resposta é um objeto rastreado.
    /// O portal manda "erro" às vezes como booleano, às vezes como texto "true".
    /// </summary>
    public static string? LerErro(string json)
    {
        using var documento = JsonDocument.Parse(json);
        var raiz = documento.RootElement;
        if (!raiz.TryGetProperty("erro", out var erro)) return null;

        var temErro = erro.ValueKind == JsonValueKind.True
                      || (erro.ValueKind == JsonValueKind.String && erro.GetString() == "true");
        if (!temErro) return null;

        return raiz.TryGetProperty("mensagem", out var mensagem) ? mensagem.GetString() ?? "" : "";
    }

    public static Rastreamento Mapear(string json)
    {
        try
        {
            var objeto = JsonSerializer.Deserialize<ObjetoPortal>(json, Opcoes);
            if (objeto?.CodObjeto is null || objeto.Eventos is null)
                throw new RespostaInesperadaException("Resposta do portal sem 'codObjeto' ou 'eventos'.");

            return new Rastreamento(
                objeto.CodObjeto,
                Texto(objeto.TipoPostal?.Descricao),
                LerData(objeto.DtPrevista),
                objeto.Eventos.Select(MapearEvento).ToList());
        }
        catch (Exception ex) when (ex is JsonException or FormatException or TimeZoneNotFoundException)
        {
            throw new RespostaInesperadaException("Formato da resposta do portal diferente do esperado.", ex);
        }
    }

    private static Evento MapearEvento(EventoPortal evento)
    {
        // Eventos de transferência trazem unidade de destino: aí a unidade principal é a origem.
        var emTransferencia = evento.UnidadeDestino is not null;

        return new Evento(
            Codigo: Texto(evento.Codigo),
            Tipo: Texto(evento.Tipo),
            Status: Texto(evento.Descricao),
            Descricao: Texto(evento.Detalhe),
            DataHora: LerDataHora(evento.DtHrCriado),
            Local: emTransferencia ? null : MapearUnidade(evento.Unidade),
            Origem: emTransferencia ? MapearUnidade(evento.Unidade) : null,
            Destino: emTransferencia ? MapearUnidade(evento.UnidadeDestino) : null);
    }

    private static Unidade? MapearUnidade(UnidadePortal? unidade) =>
        unidade is null
            ? null
            : new Unidade(Texto(unidade.Tipo), Texto(unidade.Nome), Texto(unidade.Endereco?.Cidade), Texto(unidade.Endereco?.Uf));

    /// <summary>
    /// O portal usa "" para ausência e às vezes espaço não separável; na saída, vazio vira null.
    /// </summary>
    private static string? Texto(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : Regex.Replace(valor, @"\s+", " ").Trim();

    /// <summary>
    /// Converte uma data e hora do portal para um DateTimeOffset.
    /// exemplo: "2026-08-31 16:04:26.000000" + "America/Sao_Paulo" -> 2026-08-31T16:04:26-03:00
    /// </summary>
    private static DateTimeOffset? LerDataHora(DataHoraPortal? dataHora)
    {
        if (dataHora?.Date is null) return null;

        var local = DateTime.ParseExact(dataHora.Date, "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture);
        var fuso = TimeZoneInfo.FindSystemTimeZoneById(dataHora.Timezone ?? "America/Sao_Paulo");
        return new DateTimeOffset(local, fuso.GetUtcOffset(local));
    }

    private static DateOnly? LerData(string? valor) =>
        DateOnly.TryParseExact(valor, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? data
            : null;
}
