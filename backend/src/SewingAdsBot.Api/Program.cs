using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Serilog;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Options;
using SewingAdsBot.Api.Seed;
using SewingAdsBot.Api.Services;
using SewingAdsBot.Api.Telegram;

var builder = WebApplication.CreateBuilder(args);

/// <summary>
/// Настройка Serilog (консоль + файл).
/// </summary>
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
builder.Services.Configure<LimitOptions>(builder.Configuration.GetSection("Limits"));
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/// <summary>
/// Кэш в памяти для настроек (AppSettings) и прочих лёгких данных.
/// </summary>
builder.Services.AddMemoryCache();

/// <summary>
/// CORS для админки (React).
/// По умолчанию разрешаем запросы с любых origin (JWT в заголовке).
/// В production рекомендуется ограничить список origin конкретными доменами.
/// </summary>
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AdminPanel", p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});

/// <summary>
/// Настройка JWT-аутентификации для админки.
/// </summary>
var adminOptions = builder.Configuration.GetSection("Admin").Get<AdminOptions>() ?? new AdminOptions();
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(adminOptions.JwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

/// <summary>
/// Сервисы доменной логики.
/// </summary>
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ChannelService>();
builder.Services.AddScoped<AdService>();
builder.Services.AddScoped<LinkGuardService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<DailyLimitService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<PostFormatter>();
builder.Services.AddScoped<TelegramPublisher>();
builder.Services.AddScoped<ModerationService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<PublicationService>();
builder.Services.AddScoped<ReferralService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<PinService>();
builder.Services.AddScoped<AdminAuthService>();

/// <summary>
/// Telegram-бот.
/// </summary>
builder.Services.AddSingleton<TelegramBotClientFactory>();
builder.Services.AddScoped<BotUpdateHandler>();
builder.Services.AddHostedService<TelegramBotHostedService>();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

app.UseCors("AdminPanel");

app.UseHttpMetrics();
app.MapMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

/// <summary>
/// Применяем EF Core миграции и делаем начальный seed данных.
/// </summary>
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var seeder = new DatabaseSeeder(
        scope.ServiceProvider.GetRequiredService<CategoryService>(),
        scope.ServiceProvider.GetRequiredService<ChannelService>(),
        scope.ServiceProvider.GetRequiredService<AdminAuthService>());

    await seeder.SeedAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
