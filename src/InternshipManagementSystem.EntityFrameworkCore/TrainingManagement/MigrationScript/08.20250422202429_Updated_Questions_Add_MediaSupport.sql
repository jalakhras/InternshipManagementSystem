BEGIN TRANSACTION;
DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AppQuestions]') AND [c].[name] = N'MediaUrl');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [AppQuestions] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [AppQuestions] ALTER COLUMN [MediaUrl] nvarchar(2048) NULL;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'MediaUrl';
SET @description = N'رابط الوسائط (صورة/صوت/فيديو/مستند) المرتبطة بالسؤال';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'MediaUrl';

ALTER TABLE [AppQuestions] ADD [MediaType] int NULL;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'نوع الوسائط المرتبطة بالسؤال (صورة، صوت، فيديو، مستند)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'MediaType';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250422202429_Updated_Questions_Add_MediaSupport', N'9.0.4');

COMMIT;
GO

