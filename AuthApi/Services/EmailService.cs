using ApiGeneral.AuthApi.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace ApiGeneral.AuthApi.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ── Public Methods ────────────────────────────────────────────────────────

    public async Task SendTicketEmailAsync(
        string toEmail,
        string toName,
        TicketEmailData data
    )
    {
        var message = new MimeMessage();
        message.From.Add(
            new MailboxAddress("Tickets", _config["Email:Username"] ?? "udeaismael@gmail.com")
        );
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"🎟️ Tu entrada para {data.EventName} - #{data.TicketId}";

        var builder = new BodyBuilder();

        string qrCid = $"qr_{data.TicketId}@tickets";
        bool hasInlineQr = false;

        if (!string.IsNullOrWhiteSpace(data.QrCodeBase64))
        {
            var qrBytes = Convert.FromBase64String(data.QrCodeBase64);
            var linkedResource = builder.LinkedResources.Add(
                "qr.png",
                qrBytes,
                new ContentType("image", "png")
            );
            linkedResource.ContentId = qrCid;
            linkedResource.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
            hasInlineQr = true;
        }

        builder.HtmlBody = BuildTicketHtml(toName, data, qrCid, hasInlineQr);
        message.Body = builder.ToMessageBody();

        await SendAsync(message);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string toName)
    {
        var message = new MimeMessage();
        message.From.Add(
            new MailboxAddress("Tickets", _config["Email:Username"] ?? "udeaismael@gmail.com")
        );
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "¡Bienvenido a Tickets - app! 🎉";

        message.Body = new TextPart(TextFormat.Html)
        {
            Text = BuildWelcomeHtml(toName)
        };

        await SendAsync(message);
    }
    
    // ── Forgot Password ───────────────────────────────────────────────────────

    public async Task SendForgotPasswordEmailAsync(
      string toEmail, string toName, string resetToken
    )
    {
      var message = new MimeMessage();
      message.From.Add(new MailboxAddress("Tickets - app", _config["Email:Username"] ?? "udeaismael@gmail.com"));
      message.To.Add(new MailboxAddress(toName, toEmail));
      message.Subject = "Recuperación de contraseña 🔐";
      message.Body   = new TextPart(TextFormat.Html)
      {
        Text = BuildForgotPasswordHtml(toName, toEmail, resetToken)
      };
      await SendAsync(message);
    }

    public async Task SendAssistedRegistrationEmailAsync(
        string toEmail, string toName, string tempPassword)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Tickets", _config["Email:Username"] ?? "udeaismael@gmail.com"));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "🎟️ Tu cuenta ha sido creada en el mostrador";
        message.Body = new TextPart(TextFormat.Html)
        {
            Text = BuildAssistedRegistrationHtml(toName, toEmail, tempPassword)
        };
        await SendAsync(message);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    public async Task SendPasswordResetConfirmationEmailAsync(
        string toEmail, string toName, string confirmUrl
    )
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Ticket", _config["Email:Username"] ?? "udeaismael@gmail.com"));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "¿Solicitaste restablecer tu contraseña? 🔐";
        message.Body = new TextPart(TextFormat.Html)
        {
            Text = BuildPasswordResetConfirmationHtml(toName, confirmUrl)
        };
        await SendAsync(message);
    }

    public async Task SendNewPasswordEmailAsync(
        string toEmail, string toName, string newPassword
    )
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Tickets - app", _config["Email:Username"] ?? "udeaismael@gmail.com"));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "Tu nueva contraseña está lista 🔑";
        message.Body = new TextPart(TextFormat.Html)
        {
            Text = BuildNewPasswordHtml(toName, toEmail, newPassword)
        };
        await SendAsync(message);
    }

    private async Task SendAsync(MimeMessage message)
    {
        var host     = _config["Email:Host"]     ?? throw new InvalidOperationException("Email:Host no configurado");
        var port     = int.Parse(_config["Email:Port"] ?? "587");
        var username = _config["Email:Username"] ?? throw new InvalidOperationException("Email:Username no configurado");
        var password = _config["Email:Password"] ?? throw new InvalidOperationException("Email:Password no configurado");

        _logger.LogInformation("Enviando correo a {To} via {Host}:{Port}", message.To, host, port);

        using var client = new SmtpClient();

        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Correo enviado exitosamente a {To}", message.To);
    }
    
    // ── HTML Builders ─────────────────────────────────────────────────────────
    // Paleta Calcite: #DDDCDB | #FD7B41 | #EDBF9B | #3C4044

    private static string BuildForgotPasswordHtml(
        string toName, string email, string resetToken
    ) => $@"
<!DOCTYPE html><html lang=""es""><head><meta charset=""UTF-8""/></head>
<body style=""margin:0;padding:0;background:#f4f4f4;font-family:Arial,Helvetica,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:32px 0;"">
    <tr><td align=""center"">
      <table width=""580"" cellpadding=""0"" cellspacing=""0""
        style=""background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);"">
        <tr><td style=""background:#FD7B41;padding:32px 40px;text-align:center;"">
          <h1 style=""margin:0;color:#fff;font-size:26px;"">🎟️ Tickets - app</h1>
          <p style=""margin:8px 0 0;color:#fff;opacity:.9;font-size:14px;"">Recuperación de contraseña</p>
        </td></tr>
        <tr><td style=""padding:36px 40px;"">
          <h2 style=""margin:0 0 16px;color:#3C4044;font-size:20px;"">Hola, {System.Net.WebUtility.HtmlEncode(toName)} 👋</h2>
          <p style=""margin:0 0 20px;color:#3C4044;font-size:15px;line-height:1.7;"">
            Recibimos una solicitud para restablecer la contraseña de tu cuenta.<br/>
            Usa el token a continuación para crear una nueva contraseña.
          </p>
          <div style=""background:#DDDCDB;border-radius:8px;padding:20px;text-align:center;margin-bottom:20px;"">
            <p style=""margin:0 0 8px;color:#3C4044;font-size:12px;font-weight:bold;text-transform:uppercase;letter-spacing:1px;"">Token de recuperación</p>
            <p style=""margin:0;color:#FD7B41;font-size:13px;word-break:break-all;font-family:monospace;"">{System.Net.WebUtility.HtmlEncode(resetToken)}</p>
          </div>
          <div style=""background:#EDBF9B;border-radius:8px;padding:16px 20px;"">
            <p style=""margin:0;color:#3C4044;font-size:13px;"">
              <strong>¿Cómo usar el token?</strong><br/>
              Envía una solicitud a <code>POST /api/auth/reset-password</code> con tu email,
              el token y tu nueva contraseña.<br/><br/>
              Este token expira en <strong>2 horas</strong>.
            </p>
          </div>
          <p style=""margin:20px 0 0;color:#3C4044;font-size:13px;opacity:0.7;"">
            Si no solicitaste esto, puedes ignorar este correo de forma segura.
          </p>
        </td></tr>
        <tr><td style=""background:#3C4044;padding:20px 40px;text-align:center;"">
          <p style=""margin:0;color:#DDDCDB;font-size:12px;"">© {DateTime.UtcNow.Year} Tickets - app</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body></html>";

    private static string BuildTicketHtml(
        string toName,
        TicketEmailData data,
        string qrCid,
        bool hasInlineQr
    )
    {
        var qrSection = hasInlineQr
            ? $@"<img src=""cid:{qrCid}"" alt=""QR Ticket #{data.TicketId}"" style=""width:200px;height:200px;display:block;margin:0 auto;border:4px solid #FD7B41;border-radius:8px;"" />"
            : (data.QrImageUrl != null
                ? $@"<img src=""{data.QrImageUrl}"" alt=""QR Ticket #{data.TicketId}"" style=""width:200px;height:200px;display:block;margin:0 auto;border:4px solid #FD7B41;border-radius:8px;"" />"
                : "<p style=\"color:#3C4044;text-align:center;\">QR disponible en tu cuenta</p>");

        return $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
  <title>Tu Ticket - Ticket-app</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f4;font-family:Arial,Helvetica,sans-serif;"">

  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f4;padding:32px 0;"">
    <tr>
      <td align=""center"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);"">

          <!-- Header -->
          <tr>
            <td style=""background-color:#FD7B41;padding:32px 40px;text-align:center;"">
              <h1 style=""margin:0;color:#ffffff;font-size:28px;font-weight:bold;letter-spacing:1px;"">🎟️ Tickets</h1>
              <p style=""margin:8px 0 0;color:#ffffff;font-size:15px;opacity:0.9;"">Tu entrada está lista</p>
            </td>
          </tr>

          <!-- Greeting -->
          <tr>
            <td style=""padding:32px 40px 16px;"">
              <p style=""margin:0;color:#3C4044;font-size:18px;"">Hola, <strong>{System.Net.WebUtility.HtmlEncode(toName)}</strong> 👋</p>
              <p style=""margin:12px 0 0;color:#3C4044;font-size:15px;line-height:1.6;"">
                Tu pago fue procesado con éxito. Aquí tienes tu entrada para el evento.
                Presenta el código QR en la puerta.
              </p>
            </td>
          </tr>

          <!-- Event Card -->
          <tr>
            <td style=""padding:16px 40px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0""
                style=""background-color:#DDDCDB;border-radius:10px;overflow:hidden;"">
                <tr>
                  <td colspan=""2"" style=""background-color:#3C4044;padding:14px 20px;"">
                    <p style=""margin:0;color:#FD7B41;font-size:11px;font-weight:bold;text-transform:uppercase;letter-spacing:1px;"">Evento</p>
                    <p style=""margin:4px 0 0;color:#ffffff;font-size:20px;font-weight:bold;"">{System.Net.WebUtility.HtmlEncode(data.EventName)}</p>
                  </td>
                </tr>
                <tr>
                  <td style=""padding:20px;"" width=""50%"" valign=""top"">
                    {DetailRow("📍 Lugar",  System.Net.WebUtility.HtmlEncode(data.VenueName))}
                    {DetailRow("🏙️ Ciudad", System.Net.WebUtility.HtmlEncode(data.VenueCity))}
                    {DetailRow("📅 Fecha",  data.ShowtimeStart.ToString("dddd, dd MMM yyyy", new System.Globalization.CultureInfo("es-ES")))}
                    {DetailRow("⏰ Hora",   data.ShowtimeStart.ToString("hh:mm tt"))}
                  </td>
                  <td style=""padding:20px;"" width=""50%"" valign=""top"">
                    {DetailRow("💺 Asiento",     System.Net.WebUtility.HtmlEncode(data.SeatLabel))}
                    {DetailRow("🎫 Ticket #",    data.TicketId.ToString())}
                    {DetailRow("💰 Precio",      $"${data.PricePaid:F2}")}
                    {DetailRow("🔖 Transacción", System.Net.WebUtility.HtmlEncode(data.TransactionId))}
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- QR Section -->
          <tr>
            <td style=""padding:24px 40px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0""
                style=""border:2px dashed #EDBF9B;border-radius:10px;padding:24px;"">
                <tr>
                  <td align=""center"">
                    <p style=""margin:0 0 16px;color:#3C4044;font-size:14px;font-weight:bold;text-transform:uppercase;letter-spacing:1px;"">
                      Código QR de acceso
                    </p>
                    {qrSection}
                    <p style=""margin:16px 0 0;color:#3C4044;font-size:12px;opacity:0.7;"">
                      Ticket #{data.TicketId} · Válido para una sola entrada
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Address -->
          <tr>
            <td style=""padding:0 40px 24px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0""
                style=""background-color:#EDBF9B;border-radius:8px;padding:14px 20px;"">
                <tr>
                  <td>
                    <p style=""margin:0;color:#3C4044;font-size:13px;"">
                      📍 <strong>Dirección del evento:</strong> {System.Net.WebUtility.HtmlEncode(data.VenueAddress)}, {System.Net.WebUtility.HtmlEncode(data.VenueCity)}
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background-color:#3C4044;padding:24px 40px;text-align:center;"">
              <p style=""margin:0;color:#DDDCDB;font-size:13px;line-height:1.6;"">
                Este correo fue enviado automáticamente por <strong style=""color:#FD7B41;"">Tickets - app</strong>.<br/>
                Si tienes dudas, responde este correo o visita nuestro sitio web.
              </p>
              <p style=""margin:12px 0 0;color:#DDDCDB;font-size:11px;opacity:0.6;"">© {DateTime.UtcNow.Year} Tickets · Todos los derechos reservados</p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>

</body>
</html>";
    }

    private static string DetailRow(string label, string value) =>
        $@"<p style=""margin:0 0 10px;"">
             <span style=""color:#FD7B41;font-size:12px;font-weight:bold;"">{label}</span><br/>
             <span style=""color:#3C4044;font-size:15px;"">{value}</span>
           </p>";

    private static string BuildWelcomeHtml(string toName) => $@"
<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""UTF-8""/><title>Bienvenido</title></head>
<body style=""margin:0;padding:0;background:#f4f4f4;font-family:Arial,Helvetica,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:32px 0;"">
    <tr>
      <td align=""center"">
        <table width=""580"" cellpadding=""0"" cellspacing=""0""
          style=""background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);"">
          <tr>
            <td style=""background:#FD7B41;padding:32px 40px;text-align:center;"">
              <h1 style=""margin:0;color:#fff;font-size:26px;"">🎟️ Tickets</h1>
            </td>
          </tr>
          <tr>
            <td style=""padding:36px 40px;"">
              <h2 style=""margin:0 0 16px;color:#3C4044;font-size:22px;"">¡Bienvenido, {System.Net.WebUtility.HtmlEncode(toName)}! 🎉</h2>
              <p style=""margin:0;color:#3C4044;font-size:15px;line-height:1.7;"">
                Tu cuenta ha sido creada exitosamente en <strong>Tickets</strong>.<br/>
                Ya puedes explorar eventos, reservar asientos y obtener tus entradas digitales.
              </p>
              <table style=""margin:28px 0 0;"" cellpadding=""0"" cellspacing=""0"">
                <tr>
  <td align=""center"">
    <a href=""https://kepler.andrescortes.dev/""
       target=""_blank""
       style=""display:inline-block;
              background:#FD7B41;
              border-radius:6px;
              padding:12px 28px;
              color:#ffffff;
              font-size:15px;
              font-weight:bold;
              text-decoration:none;"">
      ¡Explorar eventos!
    </a>
  </td>
</tr>
              </table>
            </td>
          </tr>
          <tr>
            <td style=""background:#3C4044;padding:20px 40px;text-align:center;"">
              <p style=""margin:0;color:#DDDCDB;font-size:12px;"">© {DateTime.UtcNow.Year} Tickets</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

    private static string BuildAssistedRegistrationHtml(
        string toName, string email, string tempPassword
    ) => $@"
<!DOCTYPE html><html lang=""es""><head><meta charset=""UTF-8""/></head>
<body style=""margin:0;padding:0;background:#f4f4f4;font-family:Arial,Helvetica,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:32px 0;"">
    <tr><td align=""center"">
      <table width=""580"" cellpadding=""0"" cellspacing=""0""
        style=""background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);"">
        <tr><td style=""background:#FD7B41;padding:32px 40px;text-align:center;"">
          <h1 style=""margin:0;color:#fff;font-size:26px;"">🎟️ Tickets - app</h1>
          <p style=""margin:8px 0 0;color:#fff;opacity:.9;font-size:14px;"">Tu cuenta ha sido creada</p>
        </td></tr>
        <tr><td style=""padding:36px 40px;"">
          <h2 style=""margin:0 0 16px;color:#3C4044;font-size:20px;"">¡Bienvenido, {System.Net.WebUtility.HtmlEncode(toName)}! 👋</h2>
          <p style=""margin:0 0 20px;color:#3C4044;font-size:15px;line-height:1.7;"">
            Un recepcionista ha creado tu cuenta en <strong>Tickets - app</strong>
            para que puedas acceder a tus tickets digitales.
            Tus credenciales de acceso son:
          </p>
          <table width=""100%"" cellpadding=""0"" cellspacing=""0""
            style=""background:#DDDCDB;border-radius:8px;overflow:hidden;margin-bottom:20px;"">
            <tr><td style=""background:#3C4044;padding:12px 20px;"">
              <span style=""color:#FD7B41;font-weight:bold;font-size:13px;"">Credenciales de acceso</span>
            </td></tr>
            <tr><td style=""padding:16px 20px;"">
              <p style=""margin:0 0 10px;"">
                <span style=""color:#FD7B41;font-size:12px;font-weight:bold;"">CORREO</span><br/>
                <span style=""color:#3C4044;font-size:15px;"">{System.Net.WebUtility.HtmlEncode(email)}</span>
              </p>
              <p style=""margin:0;"">
                <span style=""color:#FD7B41;font-size:12px;font-weight:bold;"">CONTRASEÑA TEMPORAL</span><br/>
                <span style=""color:#3C4044;font-size:15px;font-family:monospace;letter-spacing:2px;"">{System.Net.WebUtility.HtmlEncode(tempPassword)}</span>
              </p>
            </td></tr>
          </table>
          <div style=""background:#EDBF9B;border-radius:8px;padding:14px 20px;"">
            <p style=""margin:0;color:#3C4044;font-size:13px;"">
              <strong>⚠️ Por tu seguridad</strong>, te recomendamos cambiar tu contraseña la primera vez
              que inicies sesión.
            </p>
          </div>
        </td></tr>
        <tr><td style=""background:#3C4044;padding:20px 40px;text-align:center;"">
          <p style=""margin:0;color:#DDDCDB;font-size:12px;"">© {DateTime.UtcNow.Year} Tickets - app</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body></html>";

    private static string BuildPasswordResetConfirmationHtml(string toName, string confirmUrl) => $@"
<!DOCTYPE html><html lang=""es""><head><meta charset=""UTF-8""/></head>
<body style=""margin:0;padding:0;background:#f4f4f4;font-family:Arial,Helvetica,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:32px 0;"">
    <tr><td align=""center"">
      <table width=""580"" cellpadding=""0"" cellspacing=""0""
        style=""background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);"">
        <tr><td style=""background:#FD7B41;padding:32px 40px;text-align:center;"">
          <h1 style=""margin:0;color:#fff;font-size:26px;"">🎟️ Tickets - app</h1>
          <p style=""margin:8px 0 0;color:#fff;opacity:.9;font-size:14px;"">Verificación de identidad</p>
        </td></tr>
        <tr><td style=""padding:36px 40px;"">
          <h2 style=""margin:0 0 16px;color:#3C4044;font-size:20px;"">Hola, {System.Net.WebUtility.HtmlEncode(toName)} 👋</h2>
          <p style=""margin:0 0 20px;color:#3C4044;font-size:15px;line-height:1.7;"">
            Recibimos una solicitud para restablecer la contraseña de tu cuenta.<br/>
            Si fuiste tú, haz clic en el botón de abajo para confirmar y recibir tu nueva contraseña.
          </p>
          <p style=""margin:0 0 12px;color:#3C4044;font-size:14px;line-height:1.6;"">
            <strong>⏱️ Este enlace expira en 15 minutos.</strong>
          </p>
          <table cellpadding=""0"" cellspacing=""0"" style=""margin:28px 0;"">
            <tr>
              <td style=""background:#FD7B41;border-radius:8px;padding:14px 36px;"">
                <a href=""{System.Net.WebUtility.HtmlEncode(confirmUrl)}""
                   style=""color:#fff;font-size:16px;font-weight:bold;text-decoration:none;"">
                  ✅ Sí, soy yo — Restablecer contraseña
                </a>
              </td>
            </tr>
          </table>
          <div style=""background:#EDBF9B;border-radius:8px;padding:14px 20px;"">
            <p style=""margin:0;color:#3C4044;font-size:13px;"">
              <strong>⚠️ ¿No fuiste tú?</strong> Ignora este correo. Tu contraseña actual no cambiará.
            </p>
          </div>
        </td></tr>
        <tr><td style=""background:#3C4044;padding:20px 40px;text-align:center;"">
          <p style=""margin:0;color:#DDDCDB;font-size:12px;"">© {DateTime.UtcNow.Year} Tickets - app</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body></html>";

    private static string BuildNewPasswordHtml(string toName, string email, string newPassword) => $@"
<!DOCTYPE html><html lang=""es""><head><meta charset=""UTF-8""/></head>
<body style=""margin:0;padding:0;background:#f4f4f4;font-family:Arial,Helvetica,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:32px 0;"">
    <tr><td align=""center"">
      <table width=""580"" cellpadding=""0"" cellspacing=""0""
        style=""background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);"">
        <tr><td style=""background:#FD7B41;padding:32px 40px;text-align:center;"">
          <h1 style=""margin:0;color:#fff;font-size:26px;"">🎟️ Tickets - app</h1>
          <p style=""margin:8px 0 0;color:#fff;opacity:.9;font-size:14px;"">Tu nueva contraseña</p>
        </td></tr>
        <tr><td style=""padding:36px 40px;"">
          <h2 style=""margin:0 0 16px;color:#3C4044;font-size:20px;"">¡Listo, {System.Net.WebUtility.HtmlEncode(toName)}! 🎉</h2>
          <p style=""margin:0 0 20px;color:#3C4044;font-size:15px;line-height:1.7;"">
            Tu contraseña ha sido restablecida exitosamente. Aquí están tus nuevas credenciales:
          </p>
          <table width=""100%"" cellpadding=""0"" cellspacing=""0""
            style=""background:#DDDCDB;border-radius:8px;overflow:hidden;margin-bottom:20px;"">
            <tr><td style=""background:#3C4044;padding:12px 20px;"">
              <span style=""color:#FD7B41;font-weight:bold;font-size:13px;"">TUS CREDENCIALES</span>
            </td></tr>
            <tr><td style=""padding:16px 20px;"">
              <p style=""margin:0 0 10px;"">
                <span style=""color:#FD7B41;font-size:12px;font-weight:bold;"">CORREO</span><br/>
                <span style=""color:#3C4044;font-size:15px;"">{System.Net.WebUtility.HtmlEncode(email)}</span>
              </p>
              <p style=""margin:0;"">
                <span style=""color:#FD7B41;font-size:12px;font-weight:bold;"">NUEVA CONTRASEÑA</span><br/>
                <span style=""color:#3C4044;font-size:18px;font-family:monospace;letter-spacing:3px;font-weight:bold;"">{System.Net.WebUtility.HtmlEncode(newPassword)}</span>
              </p>
            </td></tr>
          </table>
          <div style=""background:#EDBF9B;border-radius:8px;padding:14px 20px;"">
            <p style=""margin:0;color:#3C4044;font-size:13px;"">
              <strong>🔒 Por tu seguridad</strong>, te recomendamos cambiar esta contraseña
              después de iniciar sesión.
            </p>
          </div>
        </td></tr>
        <tr><td style=""background:#3C4044;padding:20px 40px;text-align:center;"">
          <p style=""margin:0;color:#DDDCDB;font-size:12px;"">© {DateTime.UtcNow.Year} Tickets - app</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body></html>";
}
