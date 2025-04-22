BEGIN TRANSACTION;
ALTER TABLE [AppExamAttempts] ADD [IsGraded] bit NOT NULL DEFAULT CAST(0 AS bit);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'هل تم تصحيح المحاولة تلقائيًا أو يدويًا';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAttempts', 'COLUMN', N'IsGraded';

ALTER TABLE [AppExamAttempts] ADD [NeedsManualReview] bit NOT NULL DEFAULT CAST(0 AS bit);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'هل تحتوي المحاولة على أسئلة تحتاج مراجعة يدوية';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAttempts', 'COLUMN', N'NeedsManualReview';

ALTER TABLE [AppExamAnswers] ADD [IsCorrect] bit NULL;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'هل الإجابة صحيحة (مراجعة تلقائية أو يدوية)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAnswers', 'COLUMN', N'IsCorrect';

ALTER TABLE [AppExamAnswers] ADD [PartialScore] float NULL;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'الدرجة الجزئية لهذا الجواب إن وجدت';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAnswers', 'COLUMN', N'PartialScore';

ALTER TABLE [AppExamAnswers] ADD [ReviewComments] nvarchar(1024) NOT NULL DEFAULT N'';
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'ملاحظات المدقق اليدوي للإجابة';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAnswers', 'COLUMN', N'ReviewComments';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250422195024_Updated_ExamAnswers_And_ExamAttempt_Enhancements', N'9.0.4');

COMMIT;
GO

