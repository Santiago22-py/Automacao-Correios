using Microsoft.Playwright;
using RastreamentoCorreios.Captcha;
using RastreamentoCorreios.Erros;
using RastreamentoCorreios.Extracao;
using RastreamentoCorreios.Modelos;
using RastreamentoCorreios.Navegacao;

namespace RastreamentoCorreios;

public sealed record OpcoesRastreamento(
    int MaxTentativas = 14,
    int IntervaloEntreTentativasMs = 5000,
    float TimeoutMs = 30000,
    bool Headless = true);

/// <summary>
/// Orquestra uma consulta: valida o código, resolve o CAPTCHA com retentativas,
/// consulta o portal e mapeia o resultado.
/// </summary>
public sealed class Rastreador(ICaptchaSolver solver, OpcoesRastreamento opcoes, TextWriter log)
{
    // Mesma string que o index.js do portal compara.
    private const string MensagemCaptchaInvalido = "Captcha inválido";

    public async Task<Rastreamento> RastrearAsync(string codigo)
    {
        var codigoValido = CodigoRastreamento.Validar(codigo);

        try
        {
            await using var portal = new PortalCorreios(opcoes.Headless, opcoes.TimeoutMs);
            await portal.AbrirPaginaAsync();
            await portal.PreencherCodigoRastreioAsync(codigoValido);

            return await ResolverEConsultarAsync(portal, codigoValido);
        }
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
        {
            throw new PortalIndisponivelException($"Falha de comunicação com o portal: {ex.Message}", ex);
        }
    }

    // Tenta resolver o CAPTCHA e consultar o portal, repetindo até o máximo de tentativas.
    private async Task<Rastreamento> ResolverEConsultarAsync(PortalCorreios portal, string codigo)
    {
        var enviadas = 0;

        for (var tentativa = 1; tentativa <= opcoes.MaxTentativas; tentativa++)
        {
            // Uso responsável: espaça as requisições ao portal.
            if (tentativa > 1) await Task.Delay(opcoes.IntervaloEntreTentativasMs);

            var lido = solver.ResolverCaptcha(await portal.CapturarCaptchaAsync());
            if (!lido.Valido)
            {
                Registrar(tentativa, $"OCR leu '{lido.Texto}', formato inválido; pedindo nova imagem sem consultar");
                await portal.RecarregarCaptchaAsync();
                continue;
            }

            enviadas++;
            var resposta = await portal.ConsultarAsync(lido.Texto);

            // Limite de requisições devolve uma página HTML, não JSON: insistir só piora.
            if (!resposta.TrimStart().StartsWith('{'))
                throw new PortalIndisponivelException(
                    "O portal recusou a consulta (provavelmentec limite de requisições). Tente novamente em alguns minutos.");

            var erro = RastreamentoMapper.LerErro(resposta);
            if (erro is null)
            {
                Registrar(tentativa, $"OCR leu '{lido.Texto}', aceito. Consultas enviadas: {enviadas}");
                return RastreamentoMapper.Mapear(resposta);
            }

            if (erro != MensagemCaptchaInvalido)
                throw new ObjetoNaoEncontradoException(codigo, erro);

            Registrar(tentativa, $"OCR leu '{lido.Texto}', portal respondeu '{erro}'");
            await portal.AguardarNovoCaptchaAsync();
        }

        throw new CaptchaEsgotadoException(opcoes.MaxTentativas);
    }

    private void Registrar(int tentativa, string mensagem) =>
        log.WriteLine($"[{DateTime.Now:HH:mm:ss}] tentativa {tentativa}/{opcoes.MaxTentativas}: {mensagem}");
}
