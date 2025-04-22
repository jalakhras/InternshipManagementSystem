BEGIN TRANSACTION;
ALTER TABLE [AppTrainees] ADD [UserId] uniqueidentifier NULL;
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'معرّف المستخدم المرتبط بالمتدرب (اختياري)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppTrainees', 'COLUMN', N'UserId';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250422023648_Added_userId-to_Trainee', N'9.0.4');

COMMIT;
GO

