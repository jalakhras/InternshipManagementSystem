BEGIN TRANSACTION;
ALTER TABLE [AppExamAnswers] ADD [AnswerFileName] nvarchar(1024) NULL;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'مرفق الاجابه للإجابة';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAnswers', 'COLUMN', N'AnswerFileName';

ALTER TABLE [AppExamAnswers] ADD [AnswerFileUrl] nvarchar(1024) NULL;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'رابط مرفق الاجابه للإجابة';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAnswers', 'COLUMN', N'AnswerFileUrl';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250423225205_Updated_Answer-as-a-file', N'9.0.4');

COMMIT;
GO

