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

public class InterviewsIntegrationTests : IAsyncLifetime
{
    private const string ConnectionString = "Data Source=localhost\\SQLEXPRESS;Initial Catalog=internova_db_local;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
    private ExtentTest? _test;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        TestReportManager.Flush();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task TestScheduleInterviewFlow()
    {
        _test = TestReportManager.Instance.CreateTest("Schedule Interview Flow", "Verifies scheduling an interview updates the application status.");
        
        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            _test.Log(Status.Info, "Database connection opened.");

            // 1. Setup: Get or Create a student and an internship
            const string getStudentAndInternshipSql = @"
                SELECT TOP 1 u.user_id, i.internship_id 
                FROM [User] u, Internship i 
                WHERE u.role = 'Student'";
            
            await using var setupCmd = new SqlCommand(getStudentAndInternshipSql, connection);
            await using var reader = await setupCmd.ExecuteReaderAsync();
            
            if (!await reader.ReadAsync())
            {
                _test.Log(Status.Fail, "Prerequisite data (Student user and Internship) not found in DB.");
                throw new Exception("Prerequisite data (Student user and Internship) not found in DB.");
            }

            int studentId = reader.GetInt32(0);
            int internshipId = reader.GetInt32(1);
            await reader.CloseAsync();
            _test.Log(Status.Info, $"StudentId={studentId}, InternshipId={internshipId}");

            // 2. Create an Application
            const string insertAppSql = @"
                INSERT INTO Internship_Application (internship_id, student_id, status, applied_at, updated_at)
                VALUES (@InternshipId, @StudentId, @Status, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";
            
            await using var appCmd = new SqlCommand(insertAppSql, connection);
            appCmd.Parameters.AddWithValue("@InternshipId", internshipId);
            appCmd.Parameters.AddWithValue("@StudentId", studentId);
            appCmd.Parameters.AddWithValue("@Status", ApplicationStatus.Applied.ToString());
            
            int applicationId = (int)await appCmd.ExecuteScalarAsync();
            _test.Log(Status.Info, $"ApplicationId={applicationId} created.");

            // 3. Action: Schedule Interview (Insert into Interview table)
            const string insertInterviewSql = @"
                INSERT INTO Interview (application_id, interview_date, location_or_link, created_at, updated_at)
                VALUES (@AppId, @Date, @Location, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";
            
            DateTime interviewDate = DateTime.Now.AddDays(2);
            await using var interviewCmd = new SqlCommand(insertInterviewSql, connection);
            interviewCmd.Parameters.AddWithValue("@AppId", applicationId);
            interviewCmd.Parameters.AddWithValue("@Date", interviewDate);
            interviewCmd.Parameters.AddWithValue("@Location", "http://zoom.us/test");
            
            int interviewId = (int)await interviewCmd.ExecuteScalarAsync();
            _test.Log(Status.Info, $"InterviewId={interviewId} scheduled for {interviewDate}.");

            // 4. Action: Update Application Status
            const string updateAppSql = "UPDATE Internship_Application SET status = @Status WHERE application_id = @Id";
            await using var updateCmd = new SqlCommand(updateAppSql, connection);
            updateCmd.Parameters.AddWithValue("@Status", ApplicationStatus.InterviewScheduled.ToString());
            updateCmd.Parameters.AddWithValue("@Id", applicationId);
            await updateCmd.ExecuteNonQueryAsync();
            _test.Log(Status.Info, "Application status updated to InterviewScheduled.");

            // 5. Assert: Verify Interview was saved
            const string verifyInterviewSql = "SELECT interview_id FROM Interview WHERE interview_id = @Id";
            await using var verifyICmd = new SqlCommand(verifyInterviewSql, connection);
            verifyICmd.Parameters.AddWithValue("@Id", interviewId);
            var exists = await verifyICmd.ExecuteScalarAsync();
            Assert.NotNull(exists);

            // 6. Assert: Verify Application status was updated
            const string verifyAppSql = "SELECT status FROM Internship_Application WHERE application_id = @Id";
            await using var verifyACmd = new SqlCommand(verifyAppSql, connection);
            verifyACmd.Parameters.AddWithValue("@Id", applicationId);
            var status = (string)await verifyACmd.ExecuteScalarAsync();
            Assert.Equal(ApplicationStatus.InterviewScheduled.ToString(), status);

            _test.Log(Status.Pass, "TestScheduleInterviewFlow assertion successful.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task TestGetFutureInterviewsByStudent()
    {
        _test = TestReportManager.Instance.CreateTest("Get Future Interviews By Student", "Verifies querying student interviews only returns future-dated ones.");

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 1. Find a student
            const string getStudentSql = "SELECT TOP 1 user_id FROM [User] WHERE role = 'Student'";
            await using var studentCmd = new SqlCommand(getStudentSql, connection);
            var studentIdObj = await studentCmd.ExecuteScalarAsync();
            if (studentIdObj == null) throw new Exception("No student found.");
            int studentId = (int)studentIdObj;
            _test.Log(Status.Info, $"Testing for studentId={studentId}.");

            // 2. We need an application for this student
            const string insertAppSql = @"
                INSERT INTO Internship_Application (internship_id, student_id, status, applied_at, updated_at)
                SELECT TOP 1 internship_id, @StudentId, 'Applied', GETDATE(), GETDATE() FROM Internship;
                SELECT CAST(SCOPE_IDENTITY() as int);";
            await using var appCmd = new SqlCommand(insertAppSql, connection);
            appCmd.Parameters.AddWithValue("@StudentId", studentId);
            int applicationId = (int)await appCmd.ExecuteScalarAsync();
            _test.Log(Status.Info, $"ApplicationId={applicationId} created for student.");

            // 3. Create one PAST and one FUTURE interview
            const string insertInterviewSql = @"
                INSERT INTO Interview (application_id, interview_date, location_or_link, created_at, updated_at)
                VALUES (@AppId, @Date, 'Test', GETDATE(), GETDATE())";
            
            // Past
            await using var pastCmd = new SqlCommand(insertInterviewSql, connection);
            pastCmd.Parameters.AddWithValue("@AppId", applicationId);
            pastCmd.Parameters.AddWithValue("@Date", DateTime.Now.AddDays(-5));
            await pastCmd.ExecuteNonQueryAsync();
            _test.Log(Status.Info, "Past interview created.");

            // Future
            await using var futureCmd = new SqlCommand(insertInterviewSql, connection);
            futureCmd.Parameters.AddWithValue("@AppId", applicationId);
            futureCmd.Parameters.AddWithValue("@Date", DateTime.Now.AddDays(5));
            await futureCmd.ExecuteNonQueryAsync();
            _test.Log(Status.Info, "Future interview created.");

            // 4. Query Future Interviews (Logic we want to test)
            const string querySql = @"
                SELECT i.interview_id 
                FROM Interview i
                JOIN Internship_Application a ON i.application_id = a.application_id
                WHERE a.student_id = @StudentId AND i.interview_date >= GETDATE()";
            
            var results = new List<int>();
            await using var queryCmd = new SqlCommand(querySql, connection);
            queryCmd.Parameters.AddWithValue("@StudentId", studentId);
            await using var reader = await queryCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetInt32(0));
            }

            // Assert: Only 1 future interview should be found
            Assert.Single(results);
            _test.Log(Status.Pass, "TestGetFutureInterviewsByStudent assertion successful (Only 1 future interview found).");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }
}
