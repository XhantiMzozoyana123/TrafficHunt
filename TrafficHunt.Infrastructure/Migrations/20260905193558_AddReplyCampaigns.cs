using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrafficHunt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReplyCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE `Comments` ADD UNIQUE INDEX `IX_Comments_VideoId_YouTubeCommentId` (`VideoId`, `YouTubeCommentId`)");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorName",
                table: "Comments",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorChannelId",
                table: "Comments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplyCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MinDelaySeconds = table.Column<int>(type: "int", nullable: false),
                    MaxDelaySeconds = table.Column<int>(type: "int", nullable: false),
                    RepliesSent = table.Column<int>(type: "int", nullable: false),
                    RepliesFailed = table.Column<int>(type: "int", nullable: false),
                    RepliesPending = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplyCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplyCampaigns_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplyTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReplyCampaignId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Body = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    TimesUsed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplyTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplyTemplates_ReplyCampaigns_ReplyCampaignId",
                        column: x => x.ReplyCampaignId,
                        principalTable: "ReplyCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReplyCampaignId = table.Column<int>(type: "int", nullable: false),
                    ProspectId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    MessageSent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplyRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplyRecords_Prospects_ProspectId",
                        column: x => x.ProspectId,
                        principalTable: "Prospects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplyRecords_ReplyCampaigns_ReplyCampaignId",
                        column: x => x.ReplyCampaignId,
                        principalTable: "ReplyCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplyRecords_ReplyTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ReplyTemplates",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ReplyCampaigns_CampaignId",
                table: "ReplyCampaigns",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplyRecords_ProspectId",
                table: "ReplyRecords",
                column: "ProspectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplyRecords_ReplyCampaignId_Status",
                table: "ReplyRecords",
                columns: new[] { "ReplyCampaignId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplyRecords_TemplateId",
                table: "ReplyRecords",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplyTemplates_ReplyCampaignId",
                table: "ReplyTemplates",
                column: "ReplyCampaignId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplyRecords");

            migrationBuilder.DropTable(
                name: "ReplyTemplates");

            migrationBuilder.DropTable(
                name: "ReplyCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_Comments_VideoId",
                table: "Comments");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorName",
                table: "Comments",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorChannelId",
                table: "Comments",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
