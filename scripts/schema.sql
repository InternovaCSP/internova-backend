/*
   INTERNOVA - SQL SERVER / AZURE SQL SCHEMA
   Single canonical, idempotent schema executed by DatabaseInitializer at startup
   and usable directly in SSMS / Azure Data Studio.
*/

-- 1. User
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'User')
BEGIN
    CREATE TABLE dbo.[User] (
        user_id INT IDENTITY(1,1) PRIMARY KEY,
        full_name VARCHAR(255) NOT NULL,
        email VARCHAR(255) UNIQUE NOT NULL,
        password_hash VARCHAR(MAX) NOT NULL,
        role VARCHAR(50) CHECK (role IN ('Student', 'Company', 'Admin', 'Faculty', 'Organizer')),
        is_approved BIT DEFAULT 0,
        email_notifications_enabled BIT NOT NULL DEFAULT 1,
        push_notifications_enabled BIT NOT NULL DEFAULT 1,
        theme_preference VARCHAR(50) NOT NULL DEFAULT 'system',
        bio NVARCHAR(MAX) NULL,
        location NVARCHAR(255) NULL,
        profile_picture_url VARCHAR(2048) NULL,
        created_at DATETIME2 DEFAULT GETDATE(),
        updated_at DATETIME2 DEFAULT GETDATE()
    );
END
GO

IF COL_LENGTH('dbo.[User]', 'email_notifications_enabled') IS NULL
    ALTER TABLE dbo.[User] ADD email_notifications_enabled BIT NOT NULL DEFAULT 1;
IF COL_LENGTH('dbo.[User]', 'push_notifications_enabled') IS NULL
    ALTER TABLE dbo.[User] ADD push_notifications_enabled BIT NOT NULL DEFAULT 1;
IF COL_LENGTH('dbo.[User]', 'theme_preference') IS NULL
    ALTER TABLE dbo.[User] ADD theme_preference VARCHAR(50) NOT NULL DEFAULT 'system';
IF COL_LENGTH('dbo.[User]', 'bio') IS NULL
    ALTER TABLE dbo.[User] ADD bio NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.[User]', 'location') IS NULL
    ALTER TABLE dbo.[User] ADD location NVARCHAR(255) NULL;
IF COL_LENGTH('dbo.[User]', 'profile_picture_url') IS NULL
    ALTER TABLE dbo.[User] ADD profile_picture_url VARCHAR(2048) NULL;
GO

-- 2. Company_Profile
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Company_Profile')
BEGIN
    CREATE TABLE dbo.Company_Profile (
        company_id INT PRIMARY KEY,
        company_name VARCHAR(255) NOT NULL,
        industry VARCHAR(100),
        address TEXT,
        description TEXT,
        website_url VARCHAR(2048),
        is_verified BIT DEFAULT 0,
        status VARCHAR(50) DEFAULT 'Pending',
        CONSTRAINT FK_Company_User FOREIGN KEY (company_id) REFERENCES dbo.[User](user_id) ON DELETE CASCADE
    );
END
GO

-- 3. Student_Profile
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Student_Profile')
BEGIN
    CREATE TABLE dbo.Student_Profile (
        student_id INT PRIMARY KEY,
        university_id VARCHAR(50),
        department VARCHAR(255),
        gpa DECIMAL(3, 2),
        skills TEXT,
        resume_link VARCHAR(2048),
        created_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Student_User FOREIGN KEY (student_id) REFERENCES dbo.[User](user_id) ON DELETE CASCADE
    );
END
GO

-- 4. Internship
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Internship')
BEGIN
    CREATE TABLE dbo.Internship (
        internship_id INT IDENTITY(1,1) PRIMARY KEY,
        company_id INT NOT NULL,
        title VARCHAR(255) NOT NULL,
        description TEXT,
        requirements TEXT,
        duration VARCHAR(100),
        location VARCHAR(255),
        status VARCHAR(50) DEFAULT 'Active' CHECK (status IN ('Active', 'Closed', 'Pending Approval')),
        is_published BIT DEFAULT 0,
        created_at DATETIME2 DEFAULT GETDATE(),
        company_description NVARCHAR(MAX),
        CONSTRAINT FK_Internship_Company FOREIGN KEY (company_id) REFERENCES dbo.Company_Profile(company_id)
    );
END
GO

IF COL_LENGTH('dbo.Internship', 'company_description') IS NULL
    ALTER TABLE dbo.Internship ADD company_description NVARCHAR(MAX) NULL;
GO

-- 5. Internship_Application
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Internship_Application')
BEGIN
    CREATE TABLE dbo.Internship_Application (
        application_id INT IDENTITY(1,1) PRIMARY KEY,
        internship_id INT NOT NULL,
        student_id INT NOT NULL,
        status VARCHAR(50) DEFAULT 'Applied' CHECK (status IN ('Applied', 'Shortlisted', 'InterviewScheduled', 'Interviewing', 'Selected', 'Rejected')),
        applied_at DATETIME2 DEFAULT GETDATE(),
        updated_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Application_Internship FOREIGN KEY (internship_id) REFERENCES dbo.Internship(internship_id),
        CONSTRAINT FK_Application_Student FOREIGN KEY (student_id) REFERENCES dbo.[User](user_id)
    );
END
GO

IF COL_LENGTH('dbo.Internship_Application', 'updated_at') IS NULL
    ALTER TABLE dbo.Internship_Application ADD updated_at DATETIME2 DEFAULT GETDATE();
GO

-- 6. Interview
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Interview')
BEGIN
    CREATE TABLE dbo.Interview (
        interview_id INT IDENTITY(1,1) PRIMARY KEY,
        application_id INT NOT NULL,
        interview_date DATETIME2 NOT NULL,
        location_or_link NVARCHAR(2048) NOT NULL,
        created_at DATETIME2 DEFAULT GETDATE(),
        updated_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Interview_Application FOREIGN KEY (application_id) REFERENCES dbo.Internship_Application(application_id) ON DELETE CASCADE
    );
END
GO

-- 7. Competition
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Competition')
BEGIN
    CREATE TABLE dbo.Competition (
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
        CONSTRAINT FK_Competition_Organizer FOREIGN KEY (organizer_id) REFERENCES dbo.[User](user_id)
    );
END
GO

-- 8. Seminar_Request & 9. Seminar_Vote
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Seminar_Request')
BEGIN
    CREATE TABLE dbo.Seminar_Request (
        id INT IDENTITY(1,1) PRIMARY KEY,
        student_id INT NOT NULL,
        topic VARCHAR(255) NOT NULL,
        description TEXT NOT NULL,
        status VARCHAR(50) DEFAULT 'Pending' CHECK (status IN ('Pending', 'Approved', 'Rejected')),
        threshold INT DEFAULT 2,
        created_at DATETIME2 DEFAULT GETDATE(),
        updated_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_SeminarRequest_Student FOREIGN KEY (student_id) REFERENCES dbo.[User](user_id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Seminar_Vote')
BEGIN
    CREATE TABLE dbo.Seminar_Vote (
        vote_id INT IDENTITY(1,1) PRIMARY KEY,
        request_id INT NOT NULL,
        student_id INT NOT NULL,
        voted_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_SeminarVote_Request FOREIGN KEY (request_id) REFERENCES dbo.Seminar_Request(id) ON DELETE CASCADE,
        CONSTRAINT FK_SeminarVote_Student FOREIGN KEY (student_id) REFERENCES dbo.[User](user_id),
        CONSTRAINT UNQ_SeminarVote_RequestStudent UNIQUE (request_id, student_id)
    );
END
GO

-- 10. Project & 11. Project_Participation
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Project')
BEGIN
    CREATE TABLE dbo.Project (
        project_id INT IDENTITY(1,1) PRIMARY KEY,
        leader_id INT NOT NULL,
        title VARCHAR(255) NOT NULL,
        description TEXT,
        category VARCHAR(50) CHECK (category IN ('Research', 'Startup', 'Product')),
        required_skills TEXT,
        team_size INT,
        status VARCHAR(50) DEFAULT 'Active' CHECK (status IN ('Active', 'Completed')),
        is_approved BIT DEFAULT 0,
        CONSTRAINT FK_Project_Leader FOREIGN KEY (leader_id) REFERENCES dbo.[User](user_id) ON DELETE CASCADE
    );
END
GO

IF COL_LENGTH('dbo.Project', 'team_size') IS NULL
    ALTER TABLE dbo.Project ADD team_size INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Project_Participation')
BEGIN
    CREATE TABLE dbo.Project_Participation (
        participation_id INT IDENTITY(1,1) PRIMARY KEY,
        project_id INT NOT NULL,
        student_id INT NOT NULL,
        role VARCHAR(100),
        status VARCHAR(50) DEFAULT 'Pending' CHECK (status IN ('Pending', 'Accepted', 'Rejected')),
        joined_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_PP_Project FOREIGN KEY (project_id) REFERENCES dbo.Project(project_id) ON DELETE CASCADE,
        CONSTRAINT FK_PP_Student FOREIGN KEY (student_id) REFERENCES dbo.[User](user_id) ON DELETE NO ACTION
    );
END
GO

-- 12. BreakoutRoom
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BreakoutRoom')
BEGIN
    CREATE TABLE dbo.BreakoutRoom (
        room_id INT IDENTITY(1,1) PRIMARY KEY,
        organizer_id INT NOT NULL,
        title VARCHAR(255) NOT NULL,
        description NVARCHAR(MAX) NULL,
        scheduled_at DATETIME2 NOT NULL,
        meeting_link VARCHAR(2048) NULL,
        status VARCHAR(50) DEFAULT 'Scheduled',
        award_skills NVARCHAR(MAX) NULL,
        created_at DATETIME2 DEFAULT GETDATE(),
        updated_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_BreakoutRoom_Organizer FOREIGN KEY (organizer_id) REFERENCES dbo.[User](user_id)
    );
END
GO

-- 13. Notification
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Notification')
BEGIN
    CREATE TABLE dbo.Notification (
        notification_id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        type VARCHAR(50) NOT NULL,
        content NVARCHAR(MAX) NOT NULL,
        target_url NVARCHAR(2048),
        is_read BIT DEFAULT 0,
        created_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Notification_User FOREIGN KEY (user_id) REFERENCES dbo.[User](user_id) ON DELETE CASCADE
    );

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notification_User_Read' AND object_id = OBJECT_ID('dbo.Notification'))
        CREATE INDEX IX_Notification_User_Read ON dbo.Notification (user_id, is_read);
END
GO
