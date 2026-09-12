namespace UserApi.Settings;

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;        // token'ı imzalayan gizli anahtar
    public string Issuer { get; set; } = string.Empty;     // token'ı kim üretti
    public string Audience { get; set; } = string.Empty;   // token kimin için
    public int ExpiresInMinutes { get; set; }              // kaç dakika geçerli
}
