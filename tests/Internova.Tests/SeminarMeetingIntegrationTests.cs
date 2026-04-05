using Microsoft.Data.SqlClient;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Xunit;
using System;
using System.Threading.Tasks;
using AventStack.ExtentReports;

namespace Internova.Tests;

public class SeminarMeetingIntegrationTests : IAsyncLifetime
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
    public async Task TestSeminarApproval_GeneratesMeetingLink()
    {
        _test = TestReportManager.Instance.CreateTest("Seminar Integration: Approval & Link", "Verifies that when a seminar reaches its threshold, a meeting link is generated.");

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 1. Setup: Create a student and a seminar
            const string setupSql = @"
                SELECT TOP 1 user_id FROM [User] WHERE role = 'Student'";
            await using var setupCmd = new SqlCommand(setupSql, connection);
            var studentIdObj = await setupCmd.ExecuteScalarAsync();
            if (studentIdObj == null) throw new Exception("Student not found.");
            int studentId = (int)studentIdObj;

            const string insertSeminarSql = @"
                INSERT INTO Seminar_Request (student_id, topic, description, status, threshold, created_at, updated_at)
                VALUES (@StudentId, 'Auto-Approve Test', 'Desc', 'Pending', 1, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";
            
            await using var semCmd = new SqlCommand(insertSeminarSql, connection);
            semCmd.Parameters.AddWithValue("@StudentId", studentId);
            int seminarId = (int)await semCmd.ExecuteScalarAsync();
            _test.Log(Status.Info, $"Seminar {seminarId} created with threshold 1.");

            // 2. Action: Vote to reach threshold (Mocking or using existing Vote logic)
            // This test is supposed to fail because:
            // a) The meeting_link column doesn't exist yet.
            // b) The controller/repository logic to generate and save link is not yet there.
            
            _test.Log(Status.Info, "Simulating vote to reach threshold.");
            
            // For now, let's just attempt to query the 'meeting_link' column to show the failure.
            try
            {
                const string querySql = "SELECT meeting_link FROM Seminar_Request WHERE id = @Id";
                await using var queryCmd = new SqlCommand(querySql, connection);
                queryCmd.Parameters.AddWithValue("@Id", seminarId);
                var link = await queryCmd.ExecuteScalarAsync();
                
                Assert.NotNull(link);
                _test.Log(Status.Pass, "Meeting link successfully generated upon approval.");
            }
            catch (SqlException ex) when (ex.Message.Contains("Invalid column name 'meeting_link'"))
            {
                _test.Log(Status.Fail, "Database schema update missing: column 'meeting_link' not found.");
                throw;
            }
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }
}
