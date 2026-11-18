using Internova.Api.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace Internova.Tests.Integration;

[Trait("Category", "LocalDb")]
public class LocalDbIntegrationTests
{
    private const string ConnectionString =
        "Data Source=localhost\\SQLEXPRESS;Initial Catalog=internova_db_local;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

    [Fact]
    public async Task CheckUsersInDb()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string sql = "SELECT user_id, email, role, is_approved FROM [User]";
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            Assert.NotNull(reader["email"]);
        }
    }

    [Fact]
    public async Task TestCompetitionCrud()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string getUserSql = "SELECT TOP 1 user_id FROM [User] WHERE role IN ('Admin', 'Organizer')";
        await using var getUserCmd = new SqlCommand(getUserSql, connection);
        var organizerIdObj = await getUserCmd.ExecuteScalarAsync();

        int organizerId;
        if (organizerIdObj is null)
        {
            const string seedUserSql = """
                INSERT INTO [User] (full_name, email, password_hash, role, is_approved)
                VALUES ('Test Admin', 'testadmin@example.com', 'hash', 'Admin', 1);
                SELECT CAST(SCOPE_IDENTITY() as int);
                """;
            await using var seedUserCmd = new SqlCommand(seedUserSql, connection);
            organizerId = (int)(await seedUserCmd.ExecuteScalarAsync())!;
        }
        else
        {
            organizerId = (int)organizerIdObj;
        }

        var competition = new Competition
        {
            OrganizerId = organizerId,
            Title = "Test Hackathon " + Guid.NewGuid().ToString()[..8],
            Description = "A test competition for CRUD verification.",
            Category = "Testing",
            EligibilityCriteria = "Anyone",
            StartDate = DateTime.Now.AddDays(7),
            EndDate = DateTime.Now.AddDays(14),
            RegistrationLink = "http://test.com",
            IsApproved = false
        };

        const string insertSql = """
            INSERT INTO dbo.Competition (organizer_id, title, description, category, eligibility_criteria, start_date, end_date, registration_link, is_approved)
            VALUES (@OrganizerId, @Title, @Description, @Category, @EligibilityCriteria, @StartDate, @EndDate, @RegistrationLink, @IsApproved);
            SELECT CAST(SCOPE_IDENTITY() as int);
            """;

        await using var insertCmd = new SqlCommand(insertSql, connection);
        insertCmd.Parameters.AddWithValue("@OrganizerId", competition.OrganizerId);
        insertCmd.Parameters.AddWithValue("@Title", competition.Title);
        insertCmd.Parameters.AddWithValue("@Description", competition.Description);
        insertCmd.Parameters.AddWithValue("@Category", competition.Category);
        insertCmd.Parameters.AddWithValue("@EligibilityCriteria", competition.EligibilityCriteria);
        insertCmd.Parameters.AddWithValue("@StartDate", competition.StartDate);
        insertCmd.Parameters.AddWithValue("@EndDate", competition.EndDate);
        insertCmd.Parameters.AddWithValue("@RegistrationLink", competition.RegistrationLink);
        insertCmd.Parameters.AddWithValue("@IsApproved", competition.IsApproved);

        var id = (int)(await insertCmd.ExecuteScalarAsync())!;
        Assert.True(id > 0);

        const string selectSql = "SELECT title FROM dbo.Competition WHERE competition_id = @Id";
        await using var selectCmd = new SqlCommand(selectSql, connection);
        selectCmd.Parameters.AddWithValue("@Id", id);
        Assert.Equal(competition.Title, (string)(await selectCmd.ExecuteScalarAsync())!);

        const string updateSql = "UPDATE dbo.Competition SET title = @NewTitle WHERE competition_id = @Id";
        var newTitle = "Updated " + competition.Title;
        await using var updateCmd = new SqlCommand(updateSql, connection);
        updateCmd.Parameters.AddWithValue("@Id", id);
        updateCmd.Parameters.AddWithValue("@NewTitle", newTitle);
        Assert.Equal(1, await updateCmd.ExecuteNonQueryAsync());

        const string deleteSql = "DELETE FROM dbo.Competition WHERE competition_id = @Id";
        await using var deleteCmd = new SqlCommand(deleteSql, connection);
        deleteCmd.Parameters.AddWithValue("@Id", id);
        Assert.Equal(1, await deleteCmd.ExecuteNonQueryAsync());
    }
}
