BEGIN TRANSACTION;
ALTER TABLE [AppQuestions] ADD [CodeExpectedOutput] nvarchar(max) NULL;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'المخرجات المتوقعة من تنفيذ الكود';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'CodeExpectedOutput';

ALTER TABLE [AppQuestions] ADD [CodeLanguage] nvarchar(64) NULL;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'لغة البرمجة المطلوبة';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'CodeLanguage';

ALTER TABLE [AppQuestions] ADD [CodeStarterTemplate] nvarchar(max) NULL;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'نص الكود الابتدائي الذي يظهر للطالب (Code Starter)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'CodeStarterTemplate';

ALTER TABLE [AppQuestions] ADD [MediaFileName] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250423222719_Add-Code-To-Question', N'9.0.4');

COMMIT;
GO

