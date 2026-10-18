using RastreamentoCorreios.Navegacao;

var pasta = Path.Combine("amostras", "captcha");

switch (args.FirstOrDefault())
{
    case "coletar":
        await Coletor.ExecutarAsync(pasta, quantidade: 20);
        break;
    case "avaliar":
        Avaliador.Executar(pasta, Path.Combine("amostras", "debug"));
        break;
    default:
        Console.Error.WriteLine("Uso: CaptchaLab coletar | avaliar");
        return 1;
}

return 0;
