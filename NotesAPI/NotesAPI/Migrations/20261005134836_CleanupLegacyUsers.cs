using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotesAPI.Migrations
{
    /// <inheritdoc />
    public partial class CleanupLegacyUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
        UPDATE Users
        SET Role = 'User'
        WHERE Username IN (
            'testuserA',
            'testuserB',
            'testuserC'
        );
        """);

            migrationBuilder.Sql(
                """
        DELETE FROM Users
        WHERE Username = 'legacy';
        """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
