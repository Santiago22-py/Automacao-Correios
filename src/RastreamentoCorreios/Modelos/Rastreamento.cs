namespace RastreamentoCorreios.Modelos;

//Classes representam os dados retornados pelo portal dos correios, e são usadas para desserializar o JSON retornado pela API do portal.
public sealed record Rastreamento(string CodigoObjeto, string? TipoPostal, DateOnly? PrevisaoEntrega, IReadOnlyList<Evento> Eventos);
public sealed record Evento(string? Codigo, string? Tipo, string? Status, string? Descricao, DateTimeOffset? DataHora,Unidade? Local, Unidade? Origem, Unidade? Destino);
public sealed record Unidade(string? Tipo, string? Nome, string? Cidade, string? Uf);
