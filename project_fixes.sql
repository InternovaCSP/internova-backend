-- 1. Remove restrictive Category constraint from Project table
-- We find the constraint name first as it might be system-generated
DECLARE @CategoryConstraintName nvarchar(200)
SELECT @CategoryConstraintName = Name 
FROM SYS.CHECK_CONSTRAINTS 
WHERE PARENT_OBJECT_ID = OBJECT_ID('dbo.Project') AND NAME LIKE '%category%'

IF @CategoryConstraintName IS NOT NULL
    EXEC('ALTER TABLE dbo.Project DROP CONSTRAINT ' + @CategoryConstraintName)

-- 2. Remove restrictive Status constraint from Project table
DECLARE @StatusConstraintName nvarchar(200)
SELECT @StatusConstraintName = Name 
FROM SYS.CHECK_CONSTRAINTS 
WHERE PARENT_OBJECT_ID = OBJECT_ID('dbo.Project') AND NAME LIKE '%status%'

IF @StatusConstraintName IS NOT NULL
    EXEC('ALTER TABLE dbo.Project DROP CONSTRAINT ' + @StatusConstraintName)

-- 3. Update existing Category values to match frontend if any exist
UPDATE dbo.Project SET category = 'Product Development' WHERE category = 'Product';
