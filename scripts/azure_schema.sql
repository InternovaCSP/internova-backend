/* 
   INTERNOVA - AZURE SQL DATABASE SCHEMA
   Consolidated schema for cloud deployment.
*/

-- 1. User Table
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
        updated_at DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- 2. Company_Profile Table
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

-- 3. Student_Profile Table
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

-- 4. Internship Table
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

-- 5. Internship_Application Table
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

-- 6. Competition Table
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
        CONSTRAINT FK_Competition_Organizer FOREIGN KEY (organizer_id) REFERENCES [User](user_id)
    );
END
GO

-- 7. Project Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Project')
BEGIN
    CREATE TABLE Project (
        project_id INT IDENTITY(1,1) PRIMARY KEY,
        leader_id INT NOT NULL,
        title VARCHAR(255) NOT NULL,
        description TEXT,
        category VARCHAR(100),
        required_skills TEXT,
        status VARCHAR(50) DEFAULT 'Active',
        is_approved BIT DEFAULT 0,
        created_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Project_Leader FOREIGN KEY (leader_id) REFERENCES [User](user_id)
    );
END
GO

-- 8. Project_Participation
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Project_Participation')
BEGIN
    CREATE TABLE Project_Participation (
        participation_id INT IDENTITY(1,1) PRIMARY KEY,
        project_id INT NOT NULL,
        student_id INT NOT NULL,
        role VARCHAR(100),
        status VARCHAR(50) DEFAULT 'Pending',
        joined_at DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Participation_Project FOREIGN KEY (project_id) REFERENCES Project(project_id),
        CONSTRAINT FK_Participation_Student FOREIGN KEY (student_id) REFERENCES [User](user_id)
    );
END
GO

-- Seeding Default Admin User
-- Password is 'Admin@123' hashed using ASP.NET Core Identity
DECLARE @AdminEmail VARCHAR(255) = 'admin@internova.com';
DECLARE @PasswordHash VARCHAR(MAX) = 'AQAAAAIAAYagAAAAEJ+6Gz2YfVJS/ggP0lmeQKwNMcmsD4sQ8Ij4qOUqED1j1hThLNKeBlG1qNhqrKLSZQ==';

IF NOT EXISTS (SELECT 1 FROM [User] WHERE email = @AdminEmail)
BEGIN
    INSERT INTO [User] (full_name, email, password_hash, role, is_approved, created_at, updated_at)
    VALUES ('System Admin', @AdminEmail, @PasswordHash, 'Admin', 1, GETDATE(), GETDATE());
END
GO
