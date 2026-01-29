using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SewingAdsBot.Api.Data;

#nullable disable

namespace SewingAdsBot.Api.Migrations;

/// <summary>
/// Первичная миграция: создаёт все таблицы и индексы.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260129000100_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Users
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                FirstName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                Country = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                City = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                Balance = table.Column<decimal>(type: "numeric", nullable: false),
                ReferralCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ReferrerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.ForeignKey(
                    name: "FK_Users_Users_ReferrerUserId",
                    column: x => x.ReferrerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Users_TelegramUserId",
            table: "Users",
            column: "TelegramUserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_ReferralCode",
            table: "Users",
            column: "ReferralCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_ReferrerUserId",
            table: "Users",
            column: "ReferrerUserId");

        // UserStates
        migrationBuilder.CreateTable(
            name: "UserStates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                State = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PayloadJson = table.Column<string>(type: "text", nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserStates", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UserStates_TelegramUserId",
            table: "UserStates",
            column: "TelegramUserId",
            unique: true);

        // Categories
        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.Id);
                table.ForeignKey(
                    name: "FK_Categories_Categories_ParentId",
                    column: x => x.ParentId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Slug",
            table: "Categories",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Categories_ParentId",
            table: "Categories",
            column: "ParentId");

        // Channels
        migrationBuilder.CreateTable(
            name: "Channels",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
                TelegramUsername = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                ModerationMode = table.Column<int>(type: "integer", nullable: false),
                EnableSpamFilter = table.Column<bool>(type: "boolean", nullable: false),
                SpamFilterFreeOnly = table.Column<bool>(type: "boolean", nullable: false),
                RequireSubscription = table.Column<bool>(type: "boolean", nullable: false),
                SubscriptionChannelUsername = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                FooterLinkText = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                FooterLinkUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                PinnedMessageId = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Channels", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Channels_TelegramChatId",
            table: "Channels",
            column: "TelegramChatId",
            unique: true);

        // CategoryChannels
        migrationBuilder.CreateTable(
            name: "CategoryChannels",
            columns: table => new
            {
                CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CategoryChannels", x => new { x.CategoryId, x.ChannelId });
                table.ForeignKey(
                    name: "FK_CategoryChannels_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CategoryChannels_Channels_ChannelId",
                    column: x => x.ChannelId,
                    principalTable: "Channels",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CategoryChannels_ChannelId",
            table: "CategoryChannels",
            column: "ChannelId");

        // Ads
        migrationBuilder.CreateTable(
            name: "Ads",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Contacts = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                MediaType = table.Column<int>(type: "integer", nullable: false),
                MediaFileId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                LastPublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Ads", x => x.Id);
                table.ForeignKey(
                    name: "FK_Ads_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Ads_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Ads_UserId",
            table: "Ads",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Ads_CategoryId",
            table: "Ads",
            column: "CategoryId");

        // AdPublications
        migrationBuilder.CreateTable(
            name: "AdPublications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdId = table.Column<Guid>(type: "uuid", nullable: false),
                ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                TelegramMessageId = table.Column<int>(type: "integer", nullable: false),
                PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdPublications", x => x.Id);
                table.ForeignKey(
                    name: "FK_AdPublications_Ads_AdId",
                    column: x => x.AdId,
                    principalTable: "Ads",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AdPublications_Channels_ChannelId",
                    column: x => x.ChannelId,
                    principalTable: "Channels",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdPublications_AdId",
            table: "AdPublications",
            column: "AdId");

        migrationBuilder.CreateIndex(
            name: "IX_AdPublications_ChannelId",
            table: "AdPublications",
            column: "ChannelId");

        // ModerationRequests
        migrationBuilder.CreateTable(
            name: "ModerationRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdId = table.Column<Guid>(type: "uuid", nullable: false),
                ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
                TelegramMessageId = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                DecisionReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ModerationRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_ModerationRequests_Ads_AdId",
                    column: x => x.AdId,
                    principalTable: "Ads",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ModerationRequests_Channels_ChannelId",
                    column: x => x.ChannelId,
                    principalTable: "Channels",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ModerationRequests_AdId",
            table: "ModerationRequests",
            column: "AdId");

        migrationBuilder.CreateIndex(
            name: "IX_ModerationRequests_ChannelId",
            table: "ModerationRequests",
            column: "ChannelId");

        // DailyCounters
        migrationBuilder.CreateTable(
            name: "DailyCounters",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                Day = table.Column<DateOnly>(type: "date", nullable: false),
                FreeCount = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DailyCounters", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DailyCounters_TelegramUserId_Day",
            table: "DailyCounters",
            columns: new[] { "TelegramUserId", "Day" },
            unique: true);

        // TelegramAdmins
        migrationBuilder.CreateTable(
            name: "TelegramAdmins",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TelegramAdmins", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TelegramAdmins_TelegramUserId",
            table: "TelegramAdmins",
            column: "TelegramUserId",
            unique: true);

        // AppSettings
        migrationBuilder.CreateTable(
            name: "AppSettings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Value = table.Column<string>(type: "text", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppSettings", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AppSettings_Key",
            table: "AppSettings",
            column: "Key",
            unique: true);

        // AdminAccounts
        migrationBuilder.CreateTable(
            name: "AdminAccounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminAccounts", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminAccounts_Username",
            table: "AdminAccounts",
            column: "Username",
            unique: true);

        // PaymentInvoices
        migrationBuilder.CreateTable(
            name: "PaymentInvoices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                AdId = table.Column<Guid>(type: "uuid", nullable: true),
                InvoiceType = table.Column<int>(type: "integer", nullable: false),
                AmountMinor = table.Column<int>(type: "integer", nullable: false),
                Payload = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentInvoices", x => x.Id);
                table.ForeignKey(
                    name: "FK_PaymentInvoices_Ads_AdId",
                    column: x => x.AdId,
                    principalTable: "Ads",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_PaymentInvoices_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PaymentInvoices_Payload",
            table: "PaymentInvoices",
            column: "Payload",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PaymentInvoices_UserId",
            table: "PaymentInvoices",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentInvoices_AdId",
            table: "PaymentInvoices",
            column: "AdId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdPublications");
        migrationBuilder.DropTable(name: "ModerationRequests");
        migrationBuilder.DropTable(name: "PaymentInvoices");
        migrationBuilder.DropTable(name: "CategoryChannels");
        migrationBuilder.DropTable(name: "UserStates");
        migrationBuilder.DropTable(name: "DailyCounters");
        migrationBuilder.DropTable(name: "TelegramAdmins");
        migrationBuilder.DropTable(name: "AppSettings");
        migrationBuilder.DropTable(name: "AdminAccounts");
        migrationBuilder.DropTable(name: "Ads");
        migrationBuilder.DropTable(name: "Channels");
        migrationBuilder.DropTable(name: "Categories");
        migrationBuilder.DropTable(name: "Users");
    }
}
