namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IEmailService
{
    Task SendTicketEmailAsync(string toEmail, string toName, TicketEmailData data);
    Task SendWelcomeEmailAsync(string toEmail, string toName);
    Task SendForgotPasswordEmailAsync(string toEmail, string toName, string resetToken);

    /// <summary>
    /// Notifica al cliente registrado en mostrador su contraseña temporal.
    /// </summary>
    Task SendAssistedRegistrationEmailAsync(string toEmail, string toName, string tempPassword);

    /// <summary>
    /// Envía un correo pidiendo al usuario confirmar que él solicitó el reset.
    /// Al hacer click se aplica la nueva contraseña automáticamente.
    /// </summary>
    Task SendPasswordResetConfirmationEmailAsync(string toEmail, string toName, string confirmUrl);

    /// <summary>
    /// Envía la nueva contraseña ya aplicada al correo del usuario.
    /// </summary>
    Task SendNewPasswordEmailAsync(string toEmail, string toName, string newPassword);
}

public class TicketEmailData
{
    public int      TicketId      { get; set; }
    public string   EventName     { get; set; } = string.Empty;
    public string   VenueName     { get; set; } = string.Empty;
    public string   VenueAddress  { get; set; } = string.Empty;
    public string   VenueCity     { get; set; } = string.Empty;
    public DateTime ShowtimeStart { get; set; }
    public string   SeatLabel     { get; set; } = string.Empty;
    public decimal  PricePaid     { get; set; }
    public string   TransactionId { get; set; } = string.Empty;
    public string   QrCodeBase64  { get; set; } = string.Empty; // PNG en base64 para adjuntar inline
    public string?  QrImageUrl    { get; set; }                  // URL pública en MinIO
}
