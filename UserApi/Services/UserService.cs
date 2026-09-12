using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UserApi.Models;
using UserApi.Settings;

namespace UserApi.Services;

public class UserService
{
    private readonly IMongoCollection<User> _mongoUsers;
    private readonly RedisUserCache _redis;

    public UserService(IOptions<MongoDbSettings> settings, RedisUserCache redis)
    {
        // Mongo erişilemezse hızlı hata versin diye sunucu seçim zaman aşımını kısalttık
        // (varsayılan 30 sn; biz 2 sn yaptık ki Redis'e hızlı düşelim)
        var mongoSettings = MongoClientSettings.FromConnectionString(settings.Value.ConnectionString);
        mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);

        var client = new MongoClient(mongoSettings);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _mongoUsers = database.GetCollection<User>(settings.Value.UsersCollectionName);
        _redis = redis;
    }

    // ---------- OKUMA: önce Mongo, patlarsa Redis ----------

    public async Task<List<User>> GetAllAsync()
    {
        try
        {
            var users = await _mongoUsers.Find(_ => true).ToListAsync();
            await WarmCache(users); // Redis'i tazele (yedek güncel kalsın)
            return users;
        }
        catch (Exception)
        {
            // Mongo erişilemiyor → Redis'ten dön
            return await _redis.GetAllAsync();
        }
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        try
        {
            return await _mongoUsers.Find(u => u.Id == id).FirstOrDefaultAsync();
        }
        catch (Exception)
        {
            return await _redis.GetByIdAsync(id);
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            return await _mongoUsers.Find(u => u.Email == email).FirstOrDefaultAsync();
        }
        catch (Exception)
        {
            return await _redis.GetByEmailAsync(email);
        }
    }

    // ---------- YAZMA: Mongo'ya yaz, Redis'i de güncelle ----------

    public async Task CreateAsync(User user)
    {
        await _mongoUsers.InsertOneAsync(user);
        await _redis.SetAsync(user);
    }

    public async Task<bool> UpdateAsync(string id, string name, string email)
    {
        var update = Builders<User>.Update
            .Set(u => u.Name, name)
            .Set(u => u.Email, email);

        var result = await _mongoUsers.UpdateOneAsync(u => u.Id == id, update);
        if (result.MatchedCount == 0)
            return false;

        // Güncel kaydı okuyup Redis'e yaz (tam kopya kalsın)
        var updated = await _mongoUsers.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (updated is not null)
            await _redis.SetAsync(updated);

        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _mongoUsers.DeleteOneAsync(u => u.Id == id);
        if (result.DeletedCount == 0)
            return false;

        await _redis.RemoveAsync(id);
        return true;
    }

    // Redis yedeğini Mongo verisiyle tazele (Redis yoksa okuma yine de çalışsın diye best-effort)
    private async Task WarmCache(List<User> users)
    {
        try
        {
            foreach (var user in users)
                await _redis.SetAsync(user);
        }
        catch (Exception)
        {
            // Redis erişilemezse okumayı engelleme
        }
    }
}
