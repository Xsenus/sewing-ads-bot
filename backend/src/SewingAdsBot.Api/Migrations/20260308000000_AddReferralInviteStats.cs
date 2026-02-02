using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SewingAdsBot.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralInviteStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReferralInvitesCount",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReferralPlacementsBalance",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ReferralUnlimitedPlacements",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferralInvitesCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReferralPlacementsBalance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReferralUnlimitedPlacements",
                table: "Users");
        }
    }
}
