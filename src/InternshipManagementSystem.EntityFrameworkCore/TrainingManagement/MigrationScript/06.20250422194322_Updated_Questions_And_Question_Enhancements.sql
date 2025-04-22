BEGIN TRANSACTION;
DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AppQuestions]') AND [c].[name] = N'MediaPath');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [AppQuestions] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [AppQuestions] DROP COLUMN [MediaPath];

DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'Type';
SET @description = N'نوع السؤال';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'Type';

DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'TimeLimitInSeconds';
SET @description = N'الحد الزمني لهذا السؤال بالثواني (اختياري)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'TimeLimitInSeconds';

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AppQuestions]') AND [c].[name] = N'Score');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [AppQuestions] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [AppQuestions] ALTER COLUMN [Score] float NOT NULL;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'Score';
SET @description = N'عدد النقاط المخصصة لهذا السؤال';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'Score';

DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'OptionsJson';
SET @description = N'خيارات السؤال بصيغة JSON';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'OptionsJson';

ALTER TABLE [AppQuestions] ADD [AllowPartialCredit] bit NOT NULL DEFAULT CAST(0 AS bit);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'السماح بالحصول على درجات جزئية للأسئلة متعددة الخيارات';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'AllowPartialCredit';

ALTER TABLE [AppQuestions] ADD [MediaUrl] nvarchar(1024) NOT NULL DEFAULT N'';
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'رابط وسائط السؤال (صورة، صوت، فيديو)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'MediaUrl';

ALTER TABLE [AppCandidateExamAttempts] ADD [IsSubmitted] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250422194322_Updated_Questions_And_Question_Enhancements', N'9.0.4');

COMMIT;
GO

