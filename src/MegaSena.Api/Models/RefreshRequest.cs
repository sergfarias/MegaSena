using System.ComponentModel.DataAnnotations;

namespace MegaSena.Api.Models;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
