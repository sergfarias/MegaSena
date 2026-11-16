namespace MegaSena.Application.Models;

public sealed record EmailMensagem(string Para, string Assunto, string CorpoHtml, string? ResponderPara = null);
