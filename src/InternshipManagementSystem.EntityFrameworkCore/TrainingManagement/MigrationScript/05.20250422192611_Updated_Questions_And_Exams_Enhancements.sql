BEGIN TRANSACTION;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'Type';
SET @description = N'نوع السؤال (اختيارات/صح وخطأ/نصي/برمجي)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'Type';

DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'OptionsJson';
SET @description = N'خيارات السؤال (مخزنة بصيغة JSON)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'OptionsJson';

ALTER TABLE [AppQuestions] ADD [MediaPath] nvarchar(512) NOT NULL DEFAULT N'';
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'رابط صورة أو فيديو أو ملف مرفق مع السؤال';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'MediaPath';

ALTER TABLE [AppQuestions] ADD [Score] int NOT NULL DEFAULT 1;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'العلامة المخصصة لهذا السؤال';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'Score';

ALTER TABLE [AppQuestions] ADD [TimeLimitInSeconds] int NULL;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'الوقت المسموح لحل السؤال (بالثواني)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'TimeLimitInSeconds';

DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'TotalQuestions';
SET @description = N'عدد الأسئلة الكلي';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'TotalQuestions';

DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'Title';
SET @description = N'عنوان الامتحان';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'Title';

DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'TimeLimitInMinutes';
SET @description = N'المدة الإجمالية المسموح بها لحل الامتحان';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'TimeLimitInMinutes';

DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'SpecializationId';
SET @description = N'التخصص المرتبط بالامتحان';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'SpecializationId';

DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'Level';
SET @description = N'مستوى الامتحان (مبتدئ/متوسط/متقدم)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'Level';

DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'IsActive';
SET @description = N'هل الامتحان مفعل أم لا';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'IsActive';

DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'Description';
SET @description = N'وصف مختصر للامتحان';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'Description';

ALTER TABLE [AppExams] ADD [AllowQuestionTimeLimit] bit NOT NULL DEFAULT CAST(0 AS bit);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'هل يُسمح بتحديد وقت لكل سؤال بشكل مستقل؟';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'AllowQuestionTimeLimit';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250422192611_Updated_Questions_And_Exams_Enhancements', N'9.0.4');

COMMIT;
GO

