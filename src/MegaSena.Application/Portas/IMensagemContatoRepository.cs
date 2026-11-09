using MegaSena.Application.Models;

namespace MegaSena.Application.Portas;

public interface IMensagemContatoRepository
{
    Task AdicionarAsync(MensagemContatoDto mensagem);
    Task<List<MensagemContatoDto>> ObterNaoLidasAsync();
    Task<List<MensagemContatoDto>> ObterTodasAsync();
    Task MarcarComoLidaAsync(Guid id);
}