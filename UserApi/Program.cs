using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using StackExchange.Redis;
using UserApi.Services;
using UserApi.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// appsettings.json'daki "MongoDbSettings" bölümünü MongoDbSettings sınıfına bağla
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// JWT ayarlarını bağla
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// Redis bağlantısı (tüm uygulama tek bağlantıyı paylaşır)
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<RedisUserCache>();

// UserService'i uygulamaya tanıt (ihtiyaç duyan yere otomatik verilsin)
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<AuthService>();

// JWT ile kimlik doğrulamayı kur: gelen token'ı nasıl doğrulayacağını anlat
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,            // "kim üretti" doğru mu
            ValidateAudience = true,          // "kimin için" doğru mu
            ValidateLifetime = true,          // süresi dolmuş mu
            ValidateIssuerSigningKey = true,  // imza gizli anahtarla eşleşiyor mu
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });

// CORS: Angular arayüzü (localhost:4200) API'ye istek atabilsin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
