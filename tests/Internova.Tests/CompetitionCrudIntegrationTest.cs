using Microsoft.Data.SqlClient;
using Internova.Core.Entities;

namespace Internova.Tests;

public class CompetitionCrudIntegrationTest
{
    private static string ConnectionString => 
        Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING") 
        ?? "Data Source=localhost\\SQLEXPRESS;Initial Catalog=internova_db_local;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

    [Fact]
    public async Task TestCompetitionCrud()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        // 1. Get an organizer_id (User with role 'Admin' or 'Organizer')
        const string getUserSql = "SELECT TOP 1 user_id FROM [User] WHERE role IN ('Admin', 'Organizer')";
        await using var getUserCmd = new SqlCommand(getUserSql, connection);
        var organizerIdObj = await getUserCmd.ExecuteScalarAsync();
        
        int organizerId;
        if (organizerIdObj == null)
        {
            // Seed a test admin
            const string seedUserSql = @"
                INSERT INTO [User] (full_name, email, password_hash, role, is_approved)
                VALUES ('Test Admin', 'testadmin@example.com', 'hash', 'Admin', 1);
                SELECT CAST(SCOPE_IDENTITY() as int);";
            await using var seedUserCmd = new SqlCommand(seedUserSql, connection);
            organizerId = (int)await seedUserCmd.ExecuteScalarAsync();
        }
        else
        {
            organizerId = (int)organizerIdObj;
        }

        // 2. CREATE
        var competition = new Competition
        {
            OrganizerId = organizerId,
            Title = "Test Hackathon " + Guid.NewGuid().ToString().Substring(0, 8),
            Description = "A test competition for CRUD verification.",
            Category = "Testing",
            EligibilityCriteria = "Anyone",
            StartDate = DateTime.Now.AddDays(7),
            EndDate = DateTime.Now.AddDays(14),
            RegistrationLink = "http://test.com",
            IsApproved = false
        };

        const string insertSql = @"
            INSERT INTO dbo.Competition (organizer_id, title, description, category, eligibility_criteria, start_date, end_date, registration_link, is_approved)
            VALUES (@OrganizerId, @Title, @Description, @Category, @EligibilityCriteria, @StartDate, @EndDate, @RegistrationLink, @IsApproved);
            SELECT CAST(SCOPE_IDENTITY() as int);";

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

        var id = (int)await insertCmd.ExecuteScalarAsync();
        Assert.True(id > 0);

        // 3. READ
        const string selectSql = "SELECT title FROM dbo.Competition WHERE competition_id = @Id";
        await using var selectCmd = new SqlCommand(selectSql, connection);
        selectCmd.Parameters.AddWithValue("@Id", id);
        var title = (string)await selectCmd.ExecuteScalarAsync();
        Assert.Equal(competition.Title, title);

        // 4. UPDATE
        const string updateSql = "UPDATE dbo.Competition SET title = @NewTitle WHERE competition_id = @Id";
        string newTitle = "Updated " + competition.Title;
        await using var updateCmd = new SqlCommand(updateSql, connection);
        updateCmd.Parameters.AddWithValue("@Id", id);
        updateCmd.Parameters.AddWithValue("@NewTitle", newTitle);
        var affected = await updateCmd.ExecuteNonQueryAsync();
        Assert.Equal(1, affected);

        // Verify update
        await using var verifyCmd = new SqlCommand(selectSql, connection);
        verifyCmd.Parameters.AddWithValue("@Id", id);
        var updatedTitle = (string)await verifyCmd.ExecuteScalarAsync();
        Assert.Equal(newTitle, updatedTitle);

        // 5. DELETE
        const string deleteSql = "DELETE FROM dbo.Competition WHERE competition_id = @Id";
        await using var deleteCmd = new SqlCommand(deleteSql, connection);
        deleteCmd.Parameters.AddWithValue("@Id", id);
        affected = await deleteCmd.ExecuteNonQueryAsync();
        Assert.Equal(1, affected);

        // Verify deletion
        await using var finalCheckCmd = new SqlCommand(selectSql, connection);
        finalCheckCmd.Parameters.AddWithValue("@Id", id);
        var exists = await finalCheckCmd.ExecuteScalarAsync();
        Assert.Null(exists);
    }
}
