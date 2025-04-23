BEGIN TRANSACTION;
ALTER TABLE [AppExamAttempts] ADD [IsSubmitted] bit NOT NULL DEFAULT CAST(0 AS bit);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'هل تم ارسال الاجابات';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAttempts', 'COLUMN', N'IsSubmitted';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250423231112_Added_IsSubmitted_To_ExamAttempt', N'9.0.4');

COMMIT;
GO

