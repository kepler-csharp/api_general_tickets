using ApiGeneral.AuthApi.DTOs;
using ApiGeneral.AuthApi.DTOs.AuthDTOs;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ApiGeneral.AuthApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthControllerService _service;

    public AuthController(
        IAuthControllerService service
    )
    {
        _service = service;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        return await _service.Login(dto);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var authHeader =
            HttpContext.Request.Headers.Authorization
                .ToString();

        var token =
            authHeader.Replace("Bearer ", "");

        return await _service.Logout(token);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register-admin")]
    public async Task<IActionResult> RegisterAdmin(
        RegisterDto dto
    )
    {
        return await _service.RegisterAdmin(dto);
    }

    [AllowAnonymous]
    [HttpPost("register-customer")]
    public async Task<IActionResult> RegisterCustomer(
        RegisterDto dto
    )
    {
        return await _service.RegisterCustomer(dto);
    }

    [Authorize(Roles = "Admin, Scanner")]
    [HttpPost("register-scanner")]
    public async Task<IActionResult> RegisterScanner(
        RegisterDto dto
    )
    {
        return await _service.RegisterScanner(dto);
    }

    [Authorize(Roles = "Admin, Receptionist")]
    [HttpPost("register-receptionist")]
    public async Task<IActionResult> RegisterReceptionist(
        RegisterDto dto
    )
    {
        return await _service.RegisterReceptionist(dto);
    }

    [Authorize]
    [HttpPost("upload-photo")]
    public async Task<IActionResult> UploadPhoto(
        IFormFile file
    )
    {
        return await _service.UploadPhoto(
            User,
            file
        );
    }
    
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
        => await _service.GetProfile(User);

    /// <summary>Cambia la contraseña del usuario autenticado.</summary>
    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        => await _service.ChangePassword(User, dto);
    
    // <summary>Solicita token de recuperación de contraseña → llega al correo.</summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        => await _service.ForgotPassword(req);

    /// <summary>
    /// El usuario hace click en el link del correo de confirmación.
    /// Aplica la nueva contraseña y la envía por correo al usuario.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("confirm-password-reset")]
    public async Task<IActionResult> ConfirmPasswordReset([FromQuery] string token)
        => await _service.ConfirmPasswordReset(token);
    
    /// <summary>Restablece la contraseña con el token recibido por correo.</summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        => await _service.ResetPassword(req);

    /// <summary>Renueva el access token usando el refresh token (sin re-login).</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
        => await _service.RefreshToken(dto);
}