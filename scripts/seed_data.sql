-- Internova Database Seed Script (Robust Version)
-- Created for local development and testing
-- Tables: User, Student_Profile, Company_Profile, Project, Competition, Internship, 
--         Project_Participation, Document_Storage, Competition_Participation, Internship_Application

-- ── 1. Helper to Insert Users safely ──
DECLARE @PasswordHash NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAEJ+6Gz2YfVJS/ggP0lmeQKwNMcmsD4sQ8Ij4qOUqED1j1hThLNKeBlG1qNhqrKLSZQ==';

-- Admin
IF NOT EXISTS (SELECT 1 FROM [User] WHERE email = 'admin@internova.com')
    INSERT INTO [User] (full_name, email, password_hash, role, is_approved) VALUES ('System Admin', 'admin@internova.com', @PasswordHash, 'Admin', 1);

-- Students
IF NOT EXISTS (SELECT 1 FROM [User] WHERE email = 'alice@university.edu')
    INSERT INTO [User] (full_name, email, password_hash, role, is_approved) VALUES ('Alice Student', 'alice@university.edu', @PasswordHash, 'Student', 1);

IF NOT EXISTS (SELECT 1 FROM [User] WHERE email = 'bob@university.edu')
    INSERT INTO [User] (full_name, email, password_hash, role, is_approved) VALUES ('Bob Student', 'bob@university.edu', @PasswordHash, 'Student', 1);

-- Companies
IF NOT EXISTS (SELECT 1 FROM [User] WHERE email = 'contact@techcorp.com')
    INSERT INTO [User] (full_name, email, password_hash, role, is_approved) VALUES ('Charlie Company', 'contact@techcorp.com', @PasswordHash, 'Company', 1);

IF NOT EXISTS (SELECT 1 FROM [User] WHERE email = 'hiring@designstudio.com')
    INSERT INTO [User] (full_name, email, password_hash, role, is_approved) VALUES ('Daisy Design', 'hiring@designstudio.com', @PasswordHash, 'Company', 0);

-- 1b. Force Password Update for target seeded users (to handle cases where they already exist with old hash)
UPDATE [User] 
SET password_hash = @PasswordHash 
WHERE email IN ('admin@internova.com', 'alice@university.edu', 'bob@university.edu', 'contact@techcorp.com', 'hiring@designstudio.com');

-- 1c. Force Company Status Update
UPDATE Company_Profile SET status = 'Active', is_verified = 1 WHERE company_name = 'TechCorp Solutions';
UPDATE Company_Profile SET status = 'Pending', is_verified = 0 WHERE company_name = 'DesignStudio X';

-- Get IDs
DECLARE @AdminId INT = (SELECT user_id FROM [User] WHERE email = 'admin@internova.com');
DECLARE @StudentAliceId INT = (SELECT user_id FROM [User] WHERE email = 'alice@university.edu');
DECLARE @StudentBobId INT = (SELECT user_id FROM [User] WHERE email = 'bob@university.edu');
DECLARE @CompanyTechId INT = (SELECT user_id FROM [User] WHERE email = 'contact@techcorp.com');
DECLARE @CompanyDesignId INT = (SELECT user_id FROM [User] WHERE email = 'hiring@designstudio.com');

-- ── 2. Seed Profiles ──
IF NOT EXISTS (SELECT 1 FROM Student_Profile WHERE student_id = @StudentAliceId)
    INSERT INTO Student_Profile (student_id, university_id, department, skills, gpa) VALUES (@StudentAliceId, 'U12345', 'Computer Science', 'C#, React, SQL', 3.85);

IF NOT EXISTS (SELECT 1 FROM Student_Profile WHERE student_id = @StudentBobId)
    INSERT INTO Student_Profile (student_id, university_id, department, skills, gpa) VALUES (@StudentBobId, 'U67890', 'Mechanical Engineering', 'AutoCAD, SolidWorks', 3.60);

IF NOT EXISTS (SELECT 1 FROM Company_Profile WHERE company_id = @CompanyTechId)
    INSERT INTO Company_Profile (company_id, company_name, industry, is_verified, status) VALUES (@CompanyTechId, 'TechCorp Solutions', 'Software', 1, 'Active');

IF NOT EXISTS (SELECT 1 FROM Company_Profile WHERE company_id = @CompanyDesignId)
    INSERT INTO Company_Profile (company_id, company_name, industry, is_verified, status) VALUES (@CompanyDesignId, 'DesignStudio X', 'Creative', 0, 'Pending');

-- ── 3. Seed Projects ──
IF NOT EXISTS (SELECT 1 FROM Project WHERE title = 'Internova Backend')
    INSERT INTO Project (leader_id, title, description, category, required_skills, status, is_approved)
    VALUES (@StudentAliceId, 'Internova Backend', 'Robust backend for matching.', 'Startup', 'C#, .NET 10', 'Active', 1);

DECLARE @ProjectId INT = (SELECT project_id FROM Project WHERE title = 'Internova Backend');

-- ── 4. Seed Competitions ──
IF NOT EXISTS (SELECT 1 FROM Competition WHERE title = 'National Hackathon 2026')
    INSERT INTO Competition (organizer_id, title, description, category, start_date, end_date, is_approved)
    VALUES (@AdminId, 'National Hackathon 2026', 'A 48-hour challenge.', 'Coding', '2026-05-01', '2026-05-03', 1);

DECLARE @CompId INT = (SELECT competition_id FROM Competition WHERE title = 'National Hackathon 2026');

-- ── 5. Seed Internships ──
IF NOT EXISTS (SELECT 1 FROM Internship WHERE title = 'Junior Software Engineer' AND company_id = @CompanyTechId)
    INSERT INTO Internship (company_id, title, description, duration, location, status, is_published, created_at)
    VALUES (@CompanyTechId, 'Junior Software Engineer', 'Cloud-based apps.', '6 months', 'Remote', 'Active', 1, GETDATE());

DECLARE @InternshipId INT = (SELECT internship_id FROM Internship WHERE title = 'Junior Software Engineer' AND company_id = @CompanyTechId);

-- ── 6. Seed Transactions ──
IF NOT EXISTS (SELECT 1 FROM Project_Participation WHERE project_id = @ProjectId AND student_id = @StudentBobId)
    INSERT INTO Project_Participation (project_id, student_id, role, status) VALUES (@ProjectId, @StudentBobId, 'Developer', 'Accepted');

IF NOT EXISTS (SELECT 1 FROM Competition_Participation WHERE competition_id = @CompId AND student_id = @StudentAliceId)
    INSERT INTO Competition_Participation (competition_id, student_id, status) VALUES (@CompId, @StudentAliceId, 'Registered');

IF NOT EXISTS (SELECT 1 FROM Internship_Application WHERE internship_id = @InternshipId AND student_id = @StudentAliceId)
    INSERT INTO Internship_Application (internship_id, student_id, status) VALUES (@InternshipId, @StudentAliceId, 'Applied');

PRINT '✅ Database seeding completed successfully.';
GO
