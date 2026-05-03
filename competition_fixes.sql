-- 1. Add Skills column to Competition table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Competition]') AND name = N'skills')
BEGIN
    ALTER TABLE [dbo].[Competition] ADD [skills] NVARCHAR(MAX) NULL;
END
GO

-- 2. Create CompetitionParticipant table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CompetitionParticipant]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CompetitionParticipant] (
        [participant_id] INT IDENTITY(1,1) PRIMARY KEY,
        [competition_id] INT NOT NULL,
        [student_id] INT NOT NULL,
        [registration_date] DATETIME NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_CompetitionParticipant_Competition FOREIGN KEY ([competition_id]) 
            REFERENCES [dbo].[Competition] ([competition_id]) ON DELETE CASCADE,
        CONSTRAINT FK_CompetitionParticipant_User FOREIGN KEY ([student_id]) 
            REFERENCES [dbo].[User] ([user_id]) ON DELETE CASCADE,
        CONSTRAINT UQ_CompetitionParticipant_Student_Competition UNIQUE ([competition_id], [student_id])
    );
END
GO
