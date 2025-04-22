BEGIN TRANSACTION;
CREATE TABLE [AppCandidates] (
    [Id] uniqueidentifier NOT NULL,
    [FullName] nvarchar(128) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [PhoneNumber] nvarchar(32) NOT NULL,
    [PositionAppliedFor] nvarchar(128) NOT NULL,
    [Status] int NOT NULL,
    [ExtraProperties] nvarchar(max) NOT NULL,
    [ConcurrencyStamp] nvarchar(40) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [CreatorId] uniqueidentifier NULL,
    [LastModificationTime] datetime2 NULL,
    [LastModifierId] uniqueidentifier NULL,
    CONSTRAINT [PK_AppCandidates] PRIMARY KEY ([Id])
);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'اسم المرشح الكامل';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppCandidates', 'COLUMN', N'FullName';
SET @description = N'البريد الإلكتروني للمرشح';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppCandidates', 'COLUMN', N'Email';
SET @description = N'رقم هاتف المرشح';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppCandidates', 'COLUMN', N'PhoneNumber';
SET @description = N'الوظيفة المتقدم لها المرشح';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppCandidates', 'COLUMN', N'PositionAppliedFor';
SET @description = N'حالة المرشح (قيد التقييم / ناجح / راسب)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppCandidates', 'COLUMN', N'Status';

CREATE TABLE [AppCandidateExamAttempts] (
    [Id] uniqueidentifier NOT NULL,
    [CandidateId] uniqueidentifier NOT NULL,
    [ExamId] uniqueidentifier NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NOT NULL,
    [Score] float NOT NULL,
    [IsPassed] bit NOT NULL,
    [ExtraProperties] nvarchar(max) NOT NULL,
    [ConcurrencyStamp] nvarchar(40) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [CreatorId] uniqueidentifier NULL,
    [LastModificationTime] datetime2 NULL,
    [LastModifierId] uniqueidentifier NULL,
    CONSTRAINT [PK_AppCandidateExamAttempts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppCandidateExamAttempts_AppCandidates_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [AppCandidates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AppCandidateExamAttempts_AppExams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [AppExams] ([Id]) ON DELETE CASCADE
);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'وقت بدء محاولة الامتحان';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppCandidateExamAttempts', 'COLUMN', N'StartTime';
SET @description = N'وقت انتهاء محاولة الامتحان';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppCandidateExamAttempts', 'COLUMN', N'EndTime';
SET @description = N'نتيجة الامتحان';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppCandidateExamAttempts', 'COLUMN', N'Score';
SET @description = N'هل اجتاز المرشح الامتحان بنجاح';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppCandidateExamAttempts', 'COLUMN', N'IsPassed';

CREATE INDEX [IX_AppCandidateExamAttempts_CandidateId] ON [AppCandidateExamAttempts] ([CandidateId]);

CREATE INDEX [IX_AppCandidateExamAttempts_ExamId] ON [AppCandidateExamAttempts] ([ExamId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250422180610_Added_Candidate_Entities', N'9.0.4');

COMMIT;
GO

