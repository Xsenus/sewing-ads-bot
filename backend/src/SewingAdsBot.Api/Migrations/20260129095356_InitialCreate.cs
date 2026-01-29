using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SewingAdsBot.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TelegramChatId = table.Column<long>(type: "INTEGER", nullable: false),
                    TelegramUsername = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModerationMode = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableSpamFilter = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpamFilterFreeOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireSubscription = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubscriptionChannelUsername = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    FooterLinkText = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    FooterLinkUrl = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PinnedMessageId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyCounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TelegramUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    DateUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    CounterKey = table.Column<string>(type: "TEXT", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyCounters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelegramAdmins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TelegramUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramAdmins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelegramBots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    TelegramUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ShortDescription = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CommandsJson = table.Column<string>(type: "TEXT", nullable: true),
                    PhotoFileId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PhotoFilePath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TrackMessagesEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    LastErrorAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramBots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelegramMessageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TelegramBotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TelegramBotUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChatId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChatType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ChatTitle = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    MessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FromUserId = table.Column<long>(type: "INTEGER", nullable: true),
                    FromUsername = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    FromFirstName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    Caption = table.Column<string>(type: "TEXT", nullable: true),
                    IsForwarded = table.Column<bool>(type: "INTEGER", nullable: false),
                    ForwardFromUserId = table.Column<long>(type: "INTEGER", nullable: true),
                    ForwardFromChatId = table.Column<long>(type: "INTEGER", nullable: true),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramMessageLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TelegramUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Balance = table.Column<decimal>(type: "TEXT", nullable: false),
                    ReferralCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ReferrerUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "UserStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TelegramUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoryChannels",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChannelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Ads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Contacts = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsPaid = table.Column<bool>(type: "INTEGER", nullable: false),
                    MediaType = table.Column<int>(type: "INTEGER", nullable: false),
                    MediaFileId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BumpCount = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "AdPublications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChannelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TelegramMessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsBump = table.Column<bool>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ModerationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChannelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RejectReason = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewedByTelegramUserId = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "PaymentInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TelegramUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AdId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AmountMinor = table.Column<int>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IsPaid = table.Column<bool>(type: "INTEGER", nullable: false),
                    TelegramChargeId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAccounts_Username",
                table: "AdminAccounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdPublications_AdId",
                table: "AdPublications",
                column: "AdId");

            migrationBuilder.CreateIndex(
                name: "IX_AdPublications_ChannelId",
                table: "AdPublications",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_CategoryId",
                table: "Ads",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_UserId",
                table: "Ads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryChannels_ChannelId",
                table: "CategoryChannels",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_TelegramChatId",
                table: "Channels",
                column: "TelegramChatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyCounters_TelegramUserId_DateUtc_CounterKey",
                table: "DailyCounters",
                columns: new[] { "TelegramUserId", "DateUtc", "CounterKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModerationRequests_AdId",
                table: "ModerationRequests",
                column: "AdId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationRequests_ChannelId",
                table: "ModerationRequests",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInvoices_AdId",
                table: "PaymentInvoices",
                column: "AdId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramAdmins_TelegramUserId",
                table: "TelegramAdmins",
                column: "TelegramUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBots_Token",
                table: "TelegramBots",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBots_Username",
                table: "TelegramBots",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramMessageLogs_TelegramBotId_ChatId_MessageDateUtc",
                table: "TelegramMessageLogs",
                columns: new[] { "TelegramBotId", "ChatId", "MessageDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ReferralCode",
                table: "Users",
                column: "ReferralCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ReferrerUserId",
                table: "Users",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramUserId",
                table: "Users",
                column: "TelegramUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStates_TelegramUserId",
                table: "UserStates",
                column: "TelegramUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAccounts");

            migrationBuilder.DropTable(
                name: "AdPublications");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "CategoryChannels");

            migrationBuilder.DropTable(
                name: "DailyCounters");

            migrationBuilder.DropTable(
                name: "ModerationRequests");

            migrationBuilder.DropTable(
                name: "PaymentInvoices");

            migrationBuilder.DropTable(
                name: "TelegramAdmins");

            migrationBuilder.DropTable(
                name: "TelegramBots");

            migrationBuilder.DropTable(
                name: "TelegramMessageLogs");

            migrationBuilder.DropTable(
                name: "UserStates");

            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "Ads");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
