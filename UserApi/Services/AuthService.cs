using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UserApi.Models;
using UserApi.Models.Dtos;
using UserApi.Settings;

namespace UserApi.Services;

public class AuthService
{
    private readonly UserService _userService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(UserService userService, IOptions<JwtSettings> jwtSettings)
    {
        _userService = userService;
        _jwtSettings = jwtSettings.Value;
    }

    // Yeni kullanıcı kaydı. Email zaten varsa null döner.
    public async Task<User?> RegisterAsync(RegisterRequest request)
    {
        var existing = await _userService.GetByEmailAsync(request.Email);
        if (existing is not null)
            return null;

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            // Şifreyi düz metin değil, hash'leyerek saklıyoruz
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User"
        };

        await _userService.CreateAsync(user);
        return user;
    }

    // Giriş. Email/şifre doğruysa JWT token döner, değilse null.
    public async Task<string?> LoginAsync(LoginRequest request)
    {
        var user = await _userService.GetByEmailAsync(request.Email);
        if (user is null)
            return null;

        // Girilen şifreyi kayıtlı hash ile karşılaştır
        bool passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
            return null;

        return GenerateJwtToken(user);
    }

    // Kullanıcı bilgilerini içeren imzalı token üret
    private string GenerateJwtToken(User user)
    {
        // Token'ın içine gömülecek bilgiler (claim'ler)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id!),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };

        // Gizli anahtardan imzalama bilgisi oluştur
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Token'ı kur
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes),
            signingCredentials: credentials);

        // Token'ı metne çevir
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
