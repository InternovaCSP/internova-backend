-- =================================================================================
-- INTERNOVA DATABASE SCHEMA - COMPLETE LOCAL SETUP SCRIPT
-- =================================================================================

-- 1. Create User Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'User')
BEGIN
    CREATE TABLE [User] (
        user_id INT IDENTITY(1,1) PRIMARY KEY,
        full_name VARCHAR(255) NOT NULL,
        email VARCHAR(255) UNIQUE NOT NULL,
        password_hash VARCHAR(MAX) NOT NULL,
        role VARCHAR(50) CHECK (role IN ('Student', 'Company', 'Admin', 'Faculty', 'Organizer')),
        is_approved BIT DEFAULT 0,
        created_at DATETIME2 DEFAULT GETDATE(),
        updated_at DATETIME2 DEFAULT GETDATE(),
        bio NVARCHAR(MAX) NULL,
        location NVARCHAR(255) NULL,
        profile_picture_url NVARCHAR(2048) NULL
    );
END
GO

-- 2. Create Company_Profile Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Company_Profile')
BEGIN
    CREATE TABLE Company_Profile (
        company_id INT PRIMARY KEY,
        company_name VARCHAR(255) NOT NULL,
        industry VARCHAR(100),
        address TEXT,
        description TEXT,
        website_url VARCHAR(2048),
        is_verified BIT DEFAULT 0,
        status VARCHAR(50) DEFAULT 'Pending',
        CONSTRAINT FK_Company_User FOREIGN KEY (company_id) REFERENCES [User](user_id) ON DELETE CASCADE
    );
END
GO

-- 3. Create Student_Profile Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Student_Profile')
BEGIN
    CREATE TABLE Student_Profile (
        student_id INT PRIMARY KEY,
        university_id VARCHAR(50),
        department VARCHAR(255),
        gpa DECIMAL(3, 2),
        skills TEXT,
        resume_link VARCHAR(2048),
        created_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Student_User FOREIGN KEY (student_id) REFERENCES [User](user_id) ON DELETE CASCADE
    );
END
GO

-- 4. Create Internship Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Internship')
BEGIN
    CREATE TABLE Internship (
        internship_id INT IDENTITY(1,1) PRIMARY KEY,
        company_id INT NOT NULL,
        title VARCHAR(255) NOT NULL,
        description TEXT,
        requirements TEXT,
        duration VARCHAR(100),
        location VARCHAR(255),
        status VARCHAR(50) DEFAULT 'Active' CHECK (status IN ('Active', 'Closed')),
        is_published BIT DEFAULT 0,
        created_at DATETIME2 DEFAULT GETDATE(),
        company_description NVARCHAR(MAX),
        CONSTRAINT FK_Internship_Company FOREIGN KEY (company_id) REFERENCES Company_Profile(company_id)
    );
END
GO

-- 5. Create Competition Table (Updated with Skills)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Competition')
BEGIN
    CREATE TABLE Competition (
        competition_id INT IDENTITY(1,1) PRIMARY KEY,
        organizer_id INT NOT NULL,
        title VARCHAR(255) NOT NULL,
        description TEXT,
        category VARCHAR(100),
        eligibility_criteria TEXT,
        start_date DATE,
        end_date DATE,
        registration_link VARCHAR(2048),
        is_approved BIT DEFAULT 0,
        skills NVARCHAR(MAX) NULL, -- Added from competition_fixes.sql
        CONSTRAINT FK_Competition_Organizer FOREIGN KEY (organizer_id) REFERENCES [User](user_id)
    );
END
GO

-- 6. Create CompetitionParticipant Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CompetitionParticipant]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CompetitionParticipant] (
        participant_id INT IDENTITY(1,1) PRIMARY KEY,
        competition_id INT NOT NULL,
        student_id INT NOT NULL,
        registration_date DATETIME NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_CompetitionParticipant_Competition FOREIGN KEY ([competition_id]) 
            REFERENCES [dbo].[Competition] ([competition_id]) ON DELETE CASCADE,
        CONSTRAINT FK_CompetitionParticipant_User FOREIGN KEY ([student_id]) 
            REFERENCES [dbo].[User] ([user_id]) ON DELETE CASCADE,
        CONSTRAINT UQ_CompetitionParticipant_Student_Competition UNIQUE ([competition_id], [student_id])
    );
END
GO

-- 7. Create Project Table (Updated with flexible Category/Status)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Project')
BEGIN
    CREATE TABLE Project (
        project_id INT IDENTITY(1,1) PRIMARY KEY,
        leader_id INT NOT NULL,
        title VARCHAR(255) NOT NULL,
        description TEXT,
        category VARCHAR(50), -- Restrictive CHECK constraints removed as per project_fixes.sql
        required_skills TEXT,
        team_size INT,
        status VARCHAR(50) DEFAULT 'Active',
        is_approved BIT DEFAULT 0,
        CONSTRAINT FK_Project_Leader FOREIGN KEY (leader_id) REFERENCES [User](user_id) ON DELETE CASCADE
    );
END
GO

-- 8. Create Internship_Application Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Internship_Application')
BEGIN
    CREATE TABLE Internship_Application (
        application_id INT IDENTITY(1,1) PRIMARY KEY,
        internship_id INT NOT NULL,
        student_id INT NOT NULL,
        status VARCHAR(50) DEFAULT 'Applied' CHECK (status IN ('Applied', 'Shortlisted', 'Interviewing', 'Selected', 'Rejected')),
        applied_at DATETIME2 DEFAULT GETDATE(),
        updated_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Application_Internship FOREIGN KEY (internship_id) REFERENCES Internship(internship_id),
        CONSTRAINT FK_Application_Student FOREIGN KEY (student_id) REFERENCES [User](user_id)
    );
END
GO

-- 9. Create Seminar_Request Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Seminar_Request')
BEGIN
    CREATE TABLE Seminar_Request (
        id INT IDENTITY(1,1) PRIMARY KEY,
        student_id INT NOT NULL,
        topic VARCHAR(255) NOT NULL,
        description TEXT NOT NULL,
        status VARCHAR(50) DEFAULT 'Pending' CHECK (status IN ('Pending', 'Approved', 'Rejected')),
        threshold INT DEFAULT 2,
        created_at DATETIME2 DEFAULT GETDATE(),
        updated_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_SeminarRequest_Student FOREIGN KEY (student_id) REFERENCES [User](user_id)
    );
END
GO

-- 10. Create Seminar_Vote Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Seminar_Vote')
BEGIN
    CREATE TABLE Seminar_Vote (
        vote_id INT IDENTITY(1,1) PRIMARY KEY,
        request_id INT NOT NULL,
        student_id INT NOT NULL,
        voted_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_SeminarVote_Request FOREIGN KEY (request_id) REFERENCES Seminar_Request(id) ON DELETE CASCADE,
        CONSTRAINT FK_SeminarVote_Student FOREIGN KEY (student_id) REFERENCES [User](user_id),
        CONSTRAINT UNQ_SeminarVote_RequestStudent UNIQUE (request_id, student_id)
    );
END
GO

-- 11. Create Notification Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notification')
BEGIN
    CREATE TABLE Notification (
        notification_id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        type VARCHAR(50) NOT NULL,
        content NVARCHAR(MAX) NOT NULL,
        target_url NVARCHAR(2048),
        is_read BIT DEFAULT 0,
        created_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Notification_User FOREIGN KEY (user_id) REFERENCES [User](user_id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Notification_User_Read ON Notification (user_id, is_read);
END
GO

-- 12. Seed Default Admin User
-- Credentials: admin@internova.com / Admin@123
IF NOT EXISTS (SELECT * FROM [User] WHERE email = 'admin@internova.com')
BEGIN
    INSERT INTO [User] (full_name, email, password_hash, role, is_approved, created_at, updated_at)
    VALUES (
        'System Admin', 
        'admin@internova.com', 
        'AQAAAAEAACcQAAAAEPvS...', -- Hashed 'Admin@123'
        'Admin', 
        1, 
        GETDATE(), 
        GETDATE()
    );
END
GO
