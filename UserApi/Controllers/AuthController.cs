using Microsoft.AspNetCore.Mvc;
using UserApi.Models.Dtos;
using UserApi.Services;

namespace UserApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/register  → yeni kullanıcı kaydı
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = await _authService.RegisterAsync(request);
        if (user is null)
            return BadRequest(new { message = "Bu email zaten kayıtlı." });

        // Şifre hash'ini ASLA geri döndürmüyoruz
        return Ok(new { user.Id, user.Name, user.Email, user.Role });
    }

    // POST /api/auth/login  → giriş yap, token al
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var token = await _authService.LoginAsync(request);
        if (token is null)
            return Unauthorized(new { message = "Email veya şifre hatalı." });

        return Ok(new { token });
    }
}
