using System.ComponentModel.DataAnnotations;

namespace EmailManager.Application.DTOs;

/// <summary>
/// E-posta gönderme isteğini taşıyan DTO.
/// Hem Razor Pages form submission'ında hem de Minimal API JSON body'sinde kullanılır.
/// </summary>
public class SendEmailRequest
{
    /// <summary>Alıcı e-posta adresi. Zorunlu, geçerli e-posta formatında olmalıdır.</summary>
    [Required(ErrorMessage = "Alıcı adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [MaxLength(256, ErrorMessage = "Alıcı adresi en fazla 256 karakter olabilir.")]
    public string To { get; set; } = string.Empty;

    /// <summary>E-posta konu satırı. Zorunlu.</summary>
    [Required(ErrorMessage = "Konu zorunludur.")]
    [MaxLength(512, ErrorMessage = "Konu en fazla 512 karakter olabilir.")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>E-posta gövdesi. Zorunlu.</summary>
    [Required(ErrorMessage = "Mesaj içeriği zorunludur.")]
    public string Body { get; set; } = string.Empty;

    /// <summary><c>true</c> ise gövde HTML olarak işlenir.</summary>
    public bool IsHtml { get; set; } = false;
}
