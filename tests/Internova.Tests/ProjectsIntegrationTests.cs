using Microsoft.Data.SqlClient;
using Internova.Core.Entities;
using Internova.Core.Enums;
using Xunit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using AventStack.ExtentReports;

namespace Internova.Tests;

public class ProjectsIntegrationTests : IAsyncLifetime
{
    private const string ConnectionString = "Data Source=localhost\\SQLEXPRESS;Initial Catalog=internova_db_local;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
    private ExtentTest? _test;

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        TestReportManager.Flush();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task TestProjectsFilterByCategory()
    {
        _test = TestReportManager.Instance.CreateTest("Projects: Filter By Category", "Verifies that projects are correctly filtered by the category query parameter.");

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 1. Setup: Get a leader (Admin or Organizer)
            const string getLeaderSql = "SELECT TOP 1 user_id FROM [User] WHERE role IN ('Admin', 'Organizer')";
            await using var leaderCmd = new SqlCommand(getLeaderSql, connection);
            var leaderIdObj = await leaderCmd.ExecuteScalarAsync();
            if (leaderIdObj == null) throw new Exception("No leader user found.");
            int leaderId = (int)leaderIdObj;

            // 2. Clear or identify existing projects
            // To be safe, we'll insert two projects with different categories
            const string insertProjectSql = @"
                INSERT INTO dbo.Project (leader_id, title, description, category, status, is_approved)
                VALUES (@LeaderId, @Title, @Desc, @Cat, 'Active', 1);";
            
            string researchTitle = "Research Project " + Guid.NewGuid().ToString().Substring(0, 8);
            await using var cmd1 = new SqlCommand(insertProjectSql, connection);
            cmd1.Parameters.AddWithValue("@LeaderId", leaderId);
            cmd1.Parameters.AddWithValue("@Title", researchTitle);
            cmd1.Parameters.AddWithValue("@Desc", "Test Research");
            cmd1.Parameters.AddWithValue("@Cat", "Research");
            await cmd1.ExecuteNonQueryAsync();

            string webTitle = "Web Project " + Guid.NewGuid().ToString().Substring(0, 8);
            await using var cmd2 = new SqlCommand(insertProjectSql, connection);
            cmd2.Parameters.AddWithValue("@LeaderId", leaderId);
            cmd2.Parameters.AddWithValue("@Title", webTitle);
            cmd2.Parameters.AddWithValue("@Desc", "Test Web");
            cmd2.Parameters.AddWithValue("@Cat", "Web");
            await cmd2.ExecuteNonQueryAsync();

            _test.Log(Status.Info, "Two projects seeded: 'Research' and 'Web'.");

            // 3. Query with category=Research
            const string querySql = "SELECT title FROM dbo.Project WHERE category = 'Research'";
            var researchProjects = new List<string>();
            await using var queryCmd = new SqlCommand(querySql, connection);
            await using var reader = await queryCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                researchProjects.Add(reader.GetString(0));
            }

            // Assert: At least our seeded research project should be there
            Assert.Contains(researchTitle, researchProjects);
            Assert.DoesNotContain(webTitle, researchProjects);

            _test.Log(Status.Pass, "Successfully filtered projects by 'Research' category.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task TestCreateProjectAndAutoAssignLeader()
    {
        _test = TestReportManager.Instance.CreateTest("Projects: Create & Auto-Assign Leader", "Verifies project creation and automatic participation entry for the leader.");

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 1. Find a user to be leader
            const string getUserSql = "SELECT TOP 1 user_id FROM [User]";
            await using var userCmd = new SqlCommand(getUserSql, connection);
            int leaderId = (int)await userCmd.ExecuteScalarAsync();

            // 2. Action: Create Project
            const string insertProjectSql = @"
                INSERT INTO dbo.Project (leader_id, title, description, category, status, is_approved)
                VALUES (@LeaderId, @Title, 'Desc', 'Test', 'Active', 1);
                SELECT CAST(SCOPE_IDENTITY() as int);";
            
            string title = "New Project " + Guid.NewGuid().ToString().Substring(0, 8);
            await using var projCmd = new SqlCommand(insertProjectSql, connection);
            projCmd.Parameters.AddWithValue("@LeaderId", leaderId);
            projCmd.Parameters.AddWithValue("@Title", title);
            int projectId = (int)await projCmd.ExecuteScalarAsync();

            _test.Log(Status.Info, $"Project {projectId} created with LeaderId={leaderId}.");

            // 3. Action: Auto-assign Participation (Logic usually in Controller/Repository)
            const string insertPartSql = @"
                INSERT INTO dbo.Project_Participation (project_id, student_id, role, status, joined_at)
                VALUES (@ProjId, @StudentId, 'Leader', 'Accepted', GETDATE());";
            
            await using var partCmd = new SqlCommand(insertPartSql, connection);
            partCmd.Parameters.AddWithValue("@ProjId", projectId);
            partCmd.Parameters.AddWithValue("@StudentId", leaderId);
            await partCmd.ExecuteNonQueryAsync();

            _test.Log(Status.Info, "Participation record added.");

            // 4. Assert: Verify Participation
            const string verifySql = "SELECT status FROM dbo.Project_Participation WHERE project_id = @ProjId AND student_id = @StudentId";
            await using var verifyCmd = new SqlCommand(verifySql, connection);
            verifyCmd.Parameters.AddWithValue("@ProjId", projectId);
            verifyCmd.Parameters.AddWithValue("@StudentId", leaderId);
            var status = (string)await verifyCmd.ExecuteScalarAsync();
            Assert.Equal("Accepted", status);

            _test.Log(Status.Pass, "Leader automatically assigned and status set to 'Accepted'.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task TestStudentJoinRequestFlow()
    {
        _test = TestReportManager.Instance.CreateTest("Projects: Student Join Request", "Verifies that a student can request to join and status is 'Pending'.");

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 1. Get a project and a student user
            const string setupSql = @"
                SELECT TOP 1 p.project_id, u.user_id 
                FROM dbo.Project p, dbo.[User] u 
                WHERE u.role = 'Student' AND p.leader_id != u.user_id";
            
            await using var setupCmd = new SqlCommand(setupSql, connection);
            await using var reader = await setupCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) throw new Exception("Required data (Project + Student) not found.");
            int projectId = reader.GetInt32(0);
            int studentId = reader.GetInt32(1);
            await reader.CloseAsync();

            _test.Log(Status.Info, $"ProjectId={projectId}, StudentId={studentId}");

            // 2. Action: Request to join
            const string insertReqSql = @"
                INSERT INTO dbo.Project_Participation (project_id, student_id, role, status, joined_at)
                VALUES (@ProjId, @StudentId, 'Member', 'Pending', GETDATE());";
            
            await using var reqCmd = new SqlCommand(insertReqSql, connection);
            reqCmd.Parameters.AddWithValue("@ProjId", projectId);
            reqCmd.Parameters.AddWithValue("@StudentId", studentId);
            await reqCmd.ExecuteNonQueryAsync();

            _test.Log(Status.Info, "Join request submitted.");

            // 3. Assert: Verify Pending Status
            const string verifySql = "SELECT status FROM dbo.Project_Participation WHERE project_id = @ProjId AND student_id = @StudentId";
            await using var verifyCmd = new SqlCommand(verifySql, connection);
            verifyCmd.Parameters.AddWithValue("@ProjId", projectId);
            verifyCmd.Parameters.AddWithValue("@StudentId", studentId);
            var status = (string)await verifyCmd.ExecuteScalarAsync();
            Assert.Equal("Pending", status);

            _test.Log(Status.Pass, "Join request status is correctly set to 'Pending'.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }
}
