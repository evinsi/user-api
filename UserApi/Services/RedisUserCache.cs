using System.Text.Json;
using StackExchange.Redis;
using UserApi.Models;

namespace UserApi.Services;

public class RedisUserCache
{
    private const string HashKey = "users";
    private readonly IDatabase _db;

    public RedisUserCache(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    // Kullanıcıyı Redis hash'ine yaz (alan = id, değer = json)
    public async Task SetAsync(User user)
    {
        var dto = ToDto(user);
        await _db.HashSetAsync(HashKey, user.Id, JsonSerializer.Serialize(dto));
    }

    public async Task RemoveAsync(string id)
    {
        await _db.HashDeleteAsync(HashKey, id);
    }

    public async Task<List<User>> GetAllAsync()
    {
        var entries = await _db.HashGetAllAsync(HashKey);
        return entries
            .Select(e => JsonSerializer.Deserialize<CachedUser>(e.Value!))
            .Where(dto => dto is not null)
            .Select(dto => ToUser(dto!))
            .ToList();
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        var value = await _db.HashGetAsync(HashKey, id);
        if (value.IsNullOrEmpty)
            return null;
        var dto = JsonSerializer.Deserialize<CachedUser>(value!);
        return dto is null ? null : ToUser(dto);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(u => u.Email == email);
    }

    // --- JSON dönüşümü ---
    // User modelinde PasswordHash [JsonIgnore] olduğu için normal JSON'a girmez.
    // Redis'te tam kopya lazım (giriş de çalışsın diye), o yüzden ayrı bir DTO kullanıyoruz.
    private static CachedUser ToDto(User u) =>
        new(u.Id!, u.Name, u.Email, u.PasswordHash, u.Role);

    private static User ToUser(CachedUser c) =>
        new() { Id = c.Id, Name = c.Name, Email = c.Email, PasswordHash = c.PasswordHash, Role = c.Role };

    private record CachedUser(string Id, string Name, string Email, string PasswordHash, string Role);
}
