using Microsoft.EntityFrameworkCore;
using SewingAdsBot.Api.Domain.Entities;

namespace SewingAdsBot.Api.Data;

/// <summary>
/// EF Core DbContext приложения.
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// Конструктор.
/// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Пользователи.
/// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Состояния диалогов.
/// </summary>
    public DbSet<UserState> UserStates => Set<UserState>();

    /// <summary>
    /// Категории.
/// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// Каналы.
/// </summary>
    public DbSet<Channel> Channels => Set<Channel>();

    /// <summary>
    /// Связь категория→канал.
/// </summary>
    public DbSet<CategoryChannel> CategoryChannels => Set<CategoryChannel>();

    /// <summary>
    /// Объявления.
/// </summary>
    public DbSet<Ad> Ads => Set<Ad>();

    /// <summary>
    /// Публикации.
/// </summary>
    public DbSet<AdPublication> AdPublications => Set<AdPublication>();

    /// <summary>
    /// Заявки на модерацию.
/// </summary>
    public DbSet<ModerationRequest> ModerationRequests => Set<ModerationRequest>();

    /// <summary>
    /// Счётчики по дням.
/// </summary>
    public DbSet<DailyCounter> DailyCounters => Set<DailyCounter>();

    /// <summary>
    /// Telegram администраторы.
/// </summary>
    public DbSet<TelegramAdmin> TelegramAdmins => Set<TelegramAdmin>();

    /// <summary>
    /// Настройки.
/// </summary>
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    /// <summary>
    /// Аккаунты админки.
    /// </summary>
    public DbSet<AdminAccount> AdminAccounts => Set<AdminAccount>();

    /// <summary>
    /// Telegram боты.
    /// </summary>
    public DbSet<TelegramBot> TelegramBots => Set<TelegramBot>();

    /// <summary>
    /// Инвойсы оплат.
/// </summary>
    public DbSet<PaymentInvoice> PaymentInvoices => Set<PaymentInvoice>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.TelegramUserId)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(x => x.ReferralCode)
            .IsUnique();

        modelBuilder.Entity<UserState>()
            .HasIndex(x => x.TelegramUserId)
            .IsUnique();

        modelBuilder.Entity<Category>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        modelBuilder.Entity<Channel>()
            .HasIndex(x => x.TelegramChatId)
            .IsUnique();

        modelBuilder.Entity<CategoryChannel>()
            .HasKey(x => new { x.CategoryId, x.ChannelId });

        // Связи для целостности данных
        modelBuilder.Entity<Category>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ReferrerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CategoryChannel>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CategoryChannel>()
            .HasOne<Channel>()
            .WithMany()
            .HasForeignKey(x => x.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Ad>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Ad>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdPublication>()
            .HasOne<Ad>()
            .WithMany()
            .HasForeignKey(x => x.AdId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AdPublication>()
            .HasOne<Channel>()
            .WithMany()
            .HasForeignKey(x => x.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ModerationRequest>()
            .HasOne<Ad>()
            .WithMany()
            .HasForeignKey(x => x.AdId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ModerationRequest>()
            .HasOne<Channel>()
            .WithMany()
            .HasForeignKey(x => x.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentInvoice>()
            .HasOne<Ad>()
            .WithMany()
            .HasForeignKey(x => x.AdId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DailyCounter>()
            .HasIndex(x => new { x.TelegramUserId, x.DateUtc, x.CounterKey })
            .IsUnique();

        modelBuilder.Entity<DailyCounter>()
            .Property(x => x.DateUtc)
            .HasColumnType("date");

        modelBuilder.Entity<TelegramAdmin>()
            .HasIndex(x => x.TelegramUserId)
            .IsUnique();

        modelBuilder.Entity<AppSetting>()
            .HasIndex(x => x.Key)
            .IsUnique();

        modelBuilder.Entity<AdminAccount>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<AdminAccount>()
            .Property(x => x.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<TelegramBot>()
            .HasIndex(x => x.Token)
            .IsUnique();

        modelBuilder.Entity<TelegramBot>()
            .HasIndex(x => x.Username);
    }
}
