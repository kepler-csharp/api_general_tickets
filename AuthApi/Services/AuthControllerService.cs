using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ApiGeneral.AuthApi.DTOs;
using ApiGeneral.AuthApi.DTOs.AuthDTOs;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using Org.BouncyCastle.Math.EC;
using StackExchange.Redis;

namespace ApiGeneral.AuthApi.Services;

public class AuthControllerService : IAuthControllerService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtService _jwtService;
    private readonly IConnectionMultiplexer _redis;
    private readonly IMinioClient _minio;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _email;

    public AuthControllerService(
        UserManager<ApplicationUser> userManager,
        JwtService jwtService,
        IConnectionMultiplexer redis,
        IMinioClient minio,
        IConfiguration configuration,
        IEmailService email
    )
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _redis = redis;
        _minio = minio;
        _configuration = configuration;
        _email = email;
    }

    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null)
        {
            return new UnauthorizedObjectResult(
                "Invalid credentials"
            );
        }

        var valid =
            await _userManager.CheckPasswordAsync(
                user,
                dto.Password
            );

        if (!valid)
        {
            return new UnauthorizedObjectResult(
                "Invalid credentials"
            );
        }

        var accessToken =
            await _jwtService.GenerateAccessToken(user);

        var refreshToken =
            await _jwtService.GenerateRefreshToken(user.Id);

        return new OkObjectResult(new
        {
            accessToken,
            refreshToken
        });
    }

    public async Task<IActionResult> Logout(string token)
    {
        var db = _redis.GetDatabase();

        var jwt =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(token);

        var expiration =
            jwt.ValidTo - DateTime.UtcNow;

        await db.StringSetAsync(
            $"blacklist:{token}",
            "revoked",
            expiration
        );

        return new OkObjectResult(
            "Logged out successfully"
        );
    }

    public async Task<IActionResult> RegisterAdmin(
        RegisterDto dto
    )
    {
        return await RegisterUser(dto, "Admin");
    }

    public async Task<IActionResult> RegisterCustomer(
        RegisterDto dto
    )
    {
        return await RegisterUser(dto, "Customer");
    }

    public async Task<IActionResult> RegisterScanner(
        RegisterDto dto
    )
    {
        return await RegisterUser(dto, "Scanner");
    }

    public async Task<IActionResult> RegisterReceptionist(
        RegisterDto dto
    )
    {
        return await RegisterUser(dto, "Receptionist");
    }

    // ── Profile ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> GetProfile(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return new UnauthorizedResult();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return new NotFoundObjectResult("User not found");

        return new OkObjectResult(new UserProfileDto
        {
            FullName = user.FullName ?? string.Empty,
            Email    = user.Email    ?? string.Empty,
            PhotoUrl = user.PhotoUrl
        });
    }

    // ── Upload Photo ──────────────────────────────────────────────────────────

    public async Task<IActionResult> UploadPhoto(
        ClaimsPrincipal principal,
        IFormFile file
    )
    {
        if (file == null || file.Length == 0)
            return new BadRequestObjectResult("No file uploaded");

        const string bucketName = "user-photos";

        var exists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName)
        );

        if (!exists)
        {
            await _minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucketName)
            );
        }

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        using var stream = file.OpenReadStream();

        await _minio.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(fileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType)
        );

        var url      = _configuration["Minio:EndpointOut"];
        var photoUrl = $"http://{url}/{bucketName}/{fileName}";

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return new UnauthorizedResult();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return new NotFoundObjectResult("User not found");

        user.PhotoUrl = photoUrl;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return new BadRequestObjectResult(result.Errors);

        return new OkObjectResult(new { photoUrl = user.PhotoUrl });
    }

    // ── Change Password ───────────────────────────────────────────────────────

    public async Task<IActionResult> ChangePassword(
        ClaimsPrincipal principal,
        ChangePasswordDto dto
    )
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return new UnauthorizedResult();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return new NotFoundObjectResult("User not found");

        var result = await _userManager.ChangePasswordAsync(
            user,
            dto.CurrentPassword,
            dto.NewPassword
        );

        if (!result.Succeeded)
            return new BadRequestObjectResult(result.Errors);

        return new OkObjectResult(new { message = "Password changed successfully" });
    }

    // ── Forgot Password ───────────────────────────────────────────────────────

    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);

        // Siempre 200 para no filtrar qué emails existen
        if (user == null)
            return new OkObjectResult(new { message = "Si ese correo existe, recibirás un email de confirmación." });

        // 1. Generar contraseña nueva aleatoria (segura)
        var newPassword = GenerateSecurePassword();

        // 2. Generar token de confirmación único
        var confirmToken = Guid.NewGuid().ToString("N");

        // 3. Guardar en Redis: token → { userId, newPassword } con TTL 15 min
        var db = _redis.GetDatabase();
        var payload = System.Text.Json.JsonSerializer.Serialize(new PasswordResetPayload
        {
            UserId      = user.Id,
            NewPassword = newPassword
        });
        await db.StringSetAsync(
            $"pwd_reset_confirm:{confirmToken}",
            payload,
            TimeSpan.FromMinutes(15)
        );

        // 4. Construir URL de confirmación
        var baseUrl    = _configuration["App:BaseUrl"] ?? "https://api.kepler.andrescortes.dev";
        var confirmUrl = $"{baseUrl}/api/auth/confirm-password-reset?token={confirmToken}";

        // 5. Enviar correo de confirmación (fire & forget)
        _ = _email.SendPasswordResetConfirmationEmailAsync(
            user.Email!,
            user.FullName ?? user.Email!,
            confirmUrl
        );

        return new OkObjectResult(new { message = "Si ese correo existe, recibirás un email de confirmación." });
    }

    // ── Confirm Password Reset (GET via link del correo) ──────────────────────

    public async Task<IActionResult> ConfirmPasswordReset(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new BadRequestObjectResult("Token inválido.");

        var db      = _redis.GetDatabase();
        var key     = $"pwd_reset_confirm:{token}";
        var payload = await db.StringGetAsync(key);

        if (!payload.HasValue)
            return new BadRequestObjectResult(
                "El enlace ha expirado o ya fue utilizado. Solicita uno nuevo en /api/auth/forgot-password."
            );

        // Deserializar
        var data = System.Text.Json.JsonSerializer.Deserialize<PasswordResetPayload>((string)payload!);
        if (data is null)
            return new BadRequestObjectResult("Token inválido.");

        var user = await _userManager.FindByIdAsync(data.UserId);
        if (user == null || !user.IsActive)
            return new BadRequestObjectResult("Usuario no encontrado.");

        // Aplicar la nueva contraseña usando Identity
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result     = await _userManager.ResetPasswordAsync(user, resetToken, data.NewPassword);

        if (!result.Succeeded)
            return new BadRequestObjectResult(result.Errors);

        // Invalidar el token de Redis (single-use)
        await db.KeyDeleteAsync(key);

        // Enviar correo con la contraseña nueva (fire & forget)
        _ = _email.SendNewPasswordEmailAsync(
            user.Email!,
            user.FullName ?? user.Email!,
            data.NewPassword
        );

        return new OkObjectResult(new
        {
            message = "Confirmación exitosa. Te hemos enviado tu nueva contraseña por correo."
        });
    }

    // ── Reset Password (mantener para compatibilidad) ─────────────────────────

    public async Task<IActionResult> ResetPassword(ResetPasswordRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null)
            return new BadRequestObjectResult("Invalid request.");

        var result = await _userManager.ResetPasswordAsync(user, req.Token, req.NewPassword);

        if (!result.Succeeded)
            return new BadRequestObjectResult(result.Errors);

        return new OkObjectResult(new { message = "Password reset successfully. You can now log in." });
    }

    // ── Refresh Token ─────────────────────────────────────────────────────────

    public async Task<IActionResult> RefreshToken(RefreshTokenDto dto)
    {
        var db = _redis.GetDatabase();
        var key = $"refresh:{dto.RefreshToken}";

        var userId = await db.StringGetAsync(key);

        if (!userId.HasValue)
            return new UnauthorizedObjectResult(new { message = "Refresh token inválido o expirado." });

        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null || !user.IsActive)
            return new UnauthorizedObjectResult(new { message = "Usuario no encontrado o inactivo." });

        // Rotate: delete old refresh token, issue new pair
        await db.KeyDeleteAsync(key);

        var newAccessToken  = await _jwtService.GenerateAccessToken(user);
        var newRefreshToken = await _jwtService.GenerateRefreshToken(user.Id);

        return new OkObjectResult(new
        {
            accessToken  = newAccessToken,
            refreshToken = newRefreshToken
        });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<IActionResult> RegisterUser(
        RegisterDto dto,
        string role
    )
    {
        var exists =
            await _userManager.FindByEmailAsync(dto.Email);

        if (exists != null)
        {
            return new BadRequestObjectResult(
                "User already exists"
            );
        }

        var user = new ApplicationUser
        {
            FullName = dto.FullName,
            Email = dto.Email,
            UserName = dto.FullName
        };

        var result =
            await _userManager.CreateAsync(
                user,
                dto.Password
            );

        if (!result.Succeeded)
        {
            return new BadRequestObjectResult(
                result.Errors
            );
        }

        await _userManager.AddToRoleAsync(user, role);

        // Send welcome email (fire and forget)
        _ = SendWelcomeEmailAsync(user);

        return new OkObjectResult(new
        {
            message =
                $"{role} registered successfully"
        });
    }

    private async Task SendWelcomeEmailAsync(ApplicationUser user)
    {
        try
        {
            await _email.SendWelcomeEmailAsync(
                user.Email!,
                user.FullName ?? user.Email!
            );
        }
        catch
        {
            // Silently ignore — no bloquear el registro
        }
    }

    private static string GenerateSecurePassword()
    {
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower   = "abcdefghijkmnpqrstuvwxyz";
        const string digits  = "23456789";
        const string special = "!@#$%&*";
        const string all     = upper + lower + digits + special;

        using var rng  = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);

        // Garantizar al menos uno de cada categoría (requisitos de Identity)
        var result = new System.Text.StringBuilder();
        result.Append(upper  [bytes[0] % upper.Length]);
        result.Append(lower  [bytes[1] % lower.Length]);
        result.Append(digits [bytes[2] % digits.Length]);
        result.Append(special[bytes[3] % special.Length]);

        for (int i = 4; i < 12; i++)
            result.Append(all[bytes[i] % all.Length]);

        // Mezclar
        var arr = result.ToString().ToCharArray();
        rng.GetBytes(bytes);
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = bytes[i % bytes.Length] % (i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }

        return new string(arr);
    }

    private sealed class PasswordResetPayload
    {
        public string UserId      { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
