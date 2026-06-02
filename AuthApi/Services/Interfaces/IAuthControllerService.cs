using System.Security.Claims;
using ApiGeneral.AuthApi.DTOs;
using ApiGeneral.AuthApi.DTOs.AuthDTOs;
using Microsoft.AspNetCore.Mvc;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IAuthControllerService
{
    Task<IActionResult> Login(LoginDto dto);

    Task<IActionResult> Logout(string token);

    Task<IActionResult> RegisterAdmin(RegisterDto dto);

    Task<IActionResult> RegisterCustomer(RegisterDto dto);

    Task<IActionResult> RegisterScanner(RegisterDto dto);

    Task<IActionResult> RegisterReceptionist(RegisterDto dto);

    Task<IActionResult> UploadPhoto(
        ClaimsPrincipal principal,
        IFormFile file
    );
    
    Task<IActionResult> GetProfile(ClaimsPrincipal principal);

    /// <summary>Cambia la contraseña del usuario autenticado.</summary>
    Task<IActionResult> ChangePassword(
        ClaimsPrincipal principal,
        ChangePasswordDto dto
    );
    
    /// <summary>Envía un correo con token para restablecer contraseña.</summary>
    Task<IActionResult> ForgotPassword(ForgotPasswordRequest req);

    /// <summary>Restablece la contraseña usando el token recibido por correo.</summary>
    Task<IActionResult> ResetPassword(ResetPasswordRequest req);

    /// <summary>Renueva el access token usando un refresh token válido.</summary>
    Task<IActionResult> RefreshToken(RefreshTokenDto dto);

    /// <summary>
    /// Confirmación del reset de contraseña: el usuario hizo click en el correo.
    /// Aplica la nueva contraseña y la envía al correo del usuario.
    /// </summary>
    Task<IActionResult> ConfirmPasswordReset(string token);
}