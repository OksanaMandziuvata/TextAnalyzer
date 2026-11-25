using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TextAnalyzer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AllowLongLongestWord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LongestWord",
                table: "Results",
                type: "character varying(2097152)",
                maxLength: 2097152,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LongestWord",
                table: "Results",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2097152)",
                oldMaxLength: 2097152);
        }
    }
}
