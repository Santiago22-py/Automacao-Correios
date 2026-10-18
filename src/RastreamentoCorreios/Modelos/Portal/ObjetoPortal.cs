namespace RastreamentoCorreios.Modelos.Portal;

//Classes representam os dados retornados pelo portal dos correios, e são usadas para desserializar o JSON retornado pela API do portal.
public sealed record ObjetoPortal(string? CodObjeto, TipoPostalPortal? TipoPostal, string? DtPrevista, List<EventoPortal>? Eventos);
public sealed record TipoPostalPortal(string? Descricao);
public sealed record EventoPortal(string? Codigo, string? Tipo, DataHoraPortal? DtHrCriado, string? Descricao, string? DescricaoDetalhada, string? Detalhe, UnidadePortal? Unidade, UnidadePortal? UnidadeDestino);
public sealed record DataHoraPortal(string? Date, string? Timezone);
public sealed record UnidadePortal(string? Nome, string? Tipo, EnderecoPortal? Endereco);
public sealed record EnderecoPortal(string? Cidade, string? Uf);
