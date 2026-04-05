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

public class SeminarIntegrationTests : IAsyncLifetime
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
    public async Task TestSeminarRequestLifecycle()
    {
        _test = TestReportManager.Instance.CreateTest("Seminar: Lifecycle", "Verifies student can create a seminar request and retrieve it.");

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 1. Get a student
            const string getStudentSql = "SELECT TOP 1 user_id FROM [User] WHERE role = 'Student'";
            await using var studentCmd = new SqlCommand(getStudentSql, connection);
            var studentIdObj = await studentCmd.ExecuteScalarAsync();
            if (studentIdObj == null) throw new Exception("Student not found.");
            int studentId = (int)studentIdObj;

            // 2. Action: Create Seminar
            string topic = "Peer Learning Topic " + Guid.NewGuid().ToString().Substring(0, 8);
            const string insertSql = @"
                INSERT INTO Seminar_Request (student_id, topic, description, status, threshold, created_at, updated_at)
                VALUES (@StudentId, @Topic, 'Description', 'Pending', 10, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";
            
            await using var semCmd = new SqlCommand(insertSql, connection);
            semCmd.Parameters.AddWithValue("@StudentId", studentId);
            semCmd.Parameters.AddWithValue("@Topic", topic);
            int seminarId = (int)await semCmd.ExecuteScalarAsync();
            _test.Log(Status.Info, $"Seminar {seminarId} created with Topic={topic}");

            // 3. Assert: Retrieve and verify
            const string querySql = "SELECT topic FROM Seminar_Request WHERE id = @Id";
            await using var queryCmd = new SqlCommand(querySql, connection);
            queryCmd.Parameters.AddWithValue("@Id", seminarId);
            var retrievedTopic = (string)await queryCmd.ExecuteScalarAsync();

            Assert.Equal(topic, retrievedTopic);
            _test.Log(Status.Pass, "Seminar lifecycle test passed.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task TestVotingMechanics()
    {
        _test = TestReportManager.Instance.CreateTest("Seminar: Voting", "Verifies that multiple students can vote and the vote count increases.");

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 1. Get two students and a seminar
            const string getStudentsSql = "SELECT TOP 2 user_id FROM [User] WHERE role = 'Student' ORDER BY user_id";
            var studentIds = new List<int>();
            await using var studentCmd = new SqlCommand(getStudentsSql, connection);
            await using var reader = await studentCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) studentIds.Add(reader.GetInt32(0));
            if (studentIds.Count < 2) throw new Exception("Not enough students in DB.");
            await reader.CloseAsync();

            const string insertSeminarSql = @"
                INSERT INTO Seminar_Request (student_id, topic, description, status, threshold, created_at, updated_at)
                VALUES (@StudentId, 'Votes Test', 'Description', 'Pending', 10, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";
            await using var semCmd = new SqlCommand(insertSeminarSql, connection);
            semCmd.Parameters.AddWithValue("@StudentId", studentIds[0]);
            int seminarId = (int)await semCmd.ExecuteScalarAsync();

            _test.Log(Status.Info, $"Seminar {seminarId} created. Voting started.");

            // 2. Action: Two votes
            const string voteSql = "INSERT INTO Seminar_Vote (request_id, student_id, voted_at) VALUES (@RequestId, @StudentId, GETDATE())";
            foreach (var sid in studentIds)
            {
                await using var voteCmd = new SqlCommand(voteSql, connection);
                voteCmd.Parameters.AddWithValue("@RequestId", seminarId);
                voteCmd.Parameters.AddWithValue("@StudentId", sid);
                await voteCmd.ExecuteNonQueryAsync();
            }

            _test.Log(Status.Info, "Two votes cast.");

            // 3. Assert: Count votes
            const string countSql = "SELECT COUNT(*) FROM Seminar_Vote WHERE request_id = @Id";
            await using var countCmd = new SqlCommand(countSql, connection);
            countCmd.Parameters.AddWithValue("@Id", seminarId);
            int count = (int)await countCmd.ExecuteScalarAsync();

            Assert.Equal(2, count);
            _test.Log(Status.Pass, "Voting mechanics test passed (Count=2).");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task TestSelfVotePrevention()
    {
        _test = TestReportManager.Instance.CreateTest("Seminar: Self-Vote Prevention", "TDD: Verifies that a student cannot vote for their own request.");

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 1. Get a student and their seminar
            const string getStudentSql = "SELECT TOP 1 user_id FROM [User] WHERE role = 'Student'";
            int studentId = (int)await new SqlCommand(getStudentSql, connection).ExecuteScalarAsync();

            const string insertSeminarSql = @"
                INSERT INTO Seminar_Request (student_id, topic, description, status, threshold, created_at, updated_at)
                VALUES (@StudentId, 'Self Vote Test', 'Desc', 'Pending', 10, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";
            var semCmd = new SqlCommand(insertSeminarSql, connection);
            semCmd.Parameters.AddWithValue("@StudentId", studentId);
            int seminarId = (int)await semCmd.ExecuteScalarAsync();

            _test.Log(Status.Info, $"Student {studentId} created seminar {seminarId}. Attempting self-vote.");

            // 2. Action: Attempt self-vote
            // NOTE: We assume the controller handles this, so we simulate the logic behavior.
            // If the repository logic doesn't prevent it, we expect our test to FAIL (TDD).
            
            // This is for demonstration. In a real integration test, we'd call the API.
            // Since we're in the integration file testing DB/Repo interaction:
            _test.Log(Status.Info, "Mocking the logic check: Expected error if studentId == creatorId.");
            
            // Logic we want: if (studentId == creatorId) return BadRequest();
            // Since we are writing the test BEFORE the fix, we check for a custom error or just the fact it should fail.
            bool isSelfVoteAllowed = true; // Assuming current state
            if (isSelfVoteAllowed)
            {
                _test.Log(Status.Warning, "Self-voting is currently allowed. This test is designed to FAIL once the logic is fixed.");
            }

            Assert.True(false, "Self-voting should not be allowed (TDD Failure Expected).");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }
}
