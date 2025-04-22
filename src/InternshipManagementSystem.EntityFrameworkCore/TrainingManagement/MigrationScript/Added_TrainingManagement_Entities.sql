BEGIN TRANSACTION;
CREATE TABLE [AppSpecializations] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [ExtraProperties] nvarchar(max) NOT NULL,
    [ConcurrencyStamp] nvarchar(40) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [CreatorId] uniqueidentifier NULL,
    [LastModificationTime] datetime2 NULL,
    [LastModifierId] uniqueidentifier NULL,
    CONSTRAINT [PK_AppSpecializations] PRIMARY KEY ([Id])
);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'اسم التخصص';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppSpecializations', 'COLUMN', N'Name';

CREATE TABLE [AppExams] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(256) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [SpecializationId] uniqueidentifier NOT NULL,
    [Level] nvarchar(max) NOT NULL,
    [TimeLimitInMinutes] int NOT NULL,
    [TotalQuestions] int NOT NULL,
    [IsActive] bit NOT NULL,
    [ExtraProperties] nvarchar(max) NOT NULL,
    [ConcurrencyStamp] nvarchar(40) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [CreatorId] uniqueidentifier NULL,
    [LastModificationTime] datetime2 NULL,
    [LastModifierId] uniqueidentifier NULL,
    CONSTRAINT [PK_AppExams] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppExams_AppSpecializations_SpecializationId] FOREIGN KEY ([SpecializationId]) REFERENCES [AppSpecializations] ([Id]) ON DELETE CASCADE
);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'عنوان الاختبار';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'Title';
SET @description = N'وصف الاختبار';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'Description';
SET @description = N'التخصص المرتبط بالاختبار';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'SpecializationId';
SET @description = N'مستوى الاختبار (مبتدئ/متوسط/متقدم)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'Level';
SET @description = N'الوقت المحدد بالدقائق للامتحان';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'TimeLimitInMinutes';
SET @description = N'عدد الأسئلة بالاختبار';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'TotalQuestions';
SET @description = N'هل الاختبار مفعل؟';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExams', 'COLUMN', N'IsActive';

CREATE TABLE [AppTrainees] (
    [Id] uniqueidentifier NOT NULL,
    [FullName] nvarchar(128) NOT NULL,
    [EmployeeNumber] nvarchar(64) NOT NULL,
    [SpecializationId] uniqueidentifier NOT NULL,
    [ExtraProperties] nvarchar(max) NOT NULL,
    [ConcurrencyStamp] nvarchar(40) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [CreatorId] uniqueidentifier NULL,
    [LastModificationTime] datetime2 NULL,
    [LastModifierId] uniqueidentifier NULL,
    CONSTRAINT [PK_AppTrainees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppTrainees_AppSpecializations_SpecializationId] FOREIGN KEY ([SpecializationId]) REFERENCES [AppSpecializations] ([Id]) ON DELETE CASCADE
);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'اسم المتدرب الكامل';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppTrainees', 'COLUMN', N'FullName';
SET @description = N'الرقم الوظيفي للمتدرب';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppTrainees', 'COLUMN', N'EmployeeNumber';
SET @description = N'معرّف التخصص المرتبط بالمتدرب';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppTrainees', 'COLUMN', N'SpecializationId';

CREATE TABLE [AppQuestions] (
    [Id] uniqueidentifier NOT NULL,
    [ExamId] uniqueidentifier NOT NULL,
    [Text] nvarchar(1024) NOT NULL,
    [Type] int NOT NULL,
    [OptionsJson] nvarchar(max) NOT NULL,
    [CorrectAnswer] nvarchar(512) NOT NULL,
    [ExtraProperties] nvarchar(max) NOT NULL,
    [ConcurrencyStamp] nvarchar(40) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [CreatorId] uniqueidentifier NULL,
    [LastModificationTime] datetime2 NULL,
    [LastModifierId] uniqueidentifier NULL,
    CONSTRAINT [PK_AppQuestions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppQuestions_AppExams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [AppExams] ([Id]) ON DELETE CASCADE
);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'نص السؤال';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'Text';
SET @description = N'نوع السؤال (اختياري/صح وخطأ/نصي)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'Type';
SET @description = N'خيارات السؤال (مخزنة كـ JSON)';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'OptionsJson';
SET @description = N'الإجابة الصحيحة للسؤال';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppQuestions', 'COLUMN', N'CorrectAnswer';

CREATE TABLE [AppExamAttempts] (
    [Id] uniqueidentifier NOT NULL,
    [ExamId] uniqueidentifier NOT NULL,
    [TraineeId] uniqueidentifier NOT NULL,
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
    CONSTRAINT [PK_AppExamAttempts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppExamAttempts_AppExams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [AppExams] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AppExamAttempts_AppTrainees_TraineeId] FOREIGN KEY ([TraineeId]) REFERENCES [AppTrainees] ([Id]) ON DELETE NO ACTION
);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'وقت بدء الامتحان';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAttempts', 'COLUMN', N'StartTime';
SET @description = N'وقت إنهاء الامتحان';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAttempts', 'COLUMN', N'EndTime';
SET @description = N'نتيجة الامتحان';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAttempts', 'COLUMN', N'Score';
SET @description = N'هل المتدرب نجح بالامتحان؟';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAttempts', 'COLUMN', N'IsPassed';

CREATE TABLE [AppExamAnswers] (
    [Id] uniqueidentifier NOT NULL,
    [ExamAttemptId] uniqueidentifier NOT NULL,
    [QuestionId] uniqueidentifier NOT NULL,
    [Answer] nvarchar(max) NOT NULL,
    [ExtraProperties] nvarchar(max) NOT NULL,
    [ConcurrencyStamp] nvarchar(40) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [CreatorId] uniqueidentifier NULL,
    [LastModificationTime] datetime2 NULL,
    [LastModifierId] uniqueidentifier NULL,
    CONSTRAINT [PK_AppExamAnswers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppExamAnswers_AppExamAttempts_ExamAttemptId] FOREIGN KEY ([ExamAttemptId]) REFERENCES [AppExamAttempts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AppExamAnswers_AppQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [AppQuestions] ([Id]) ON DELETE CASCADE
);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'إجابة المتدرب للسؤال';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppExamAnswers', 'COLUMN', N'Answer';

CREATE INDEX [IX_AppExamAnswers_ExamAttemptId] ON [AppExamAnswers] ([ExamAttemptId]);

CREATE INDEX [IX_AppExamAnswers_QuestionId] ON [AppExamAnswers] ([QuestionId]);

CREATE INDEX [IX_AppExamAttempts_ExamId] ON [AppExamAttempts] ([ExamId]);

CREATE INDEX [IX_AppExamAttempts_TraineeId] ON [AppExamAttempts] ([TraineeId]);

CREATE INDEX [IX_AppExams_SpecializationId] ON [AppExams] ([SpecializationId]);

CREATE INDEX [IX_AppQuestions_ExamId] ON [AppQuestions] ([ExamId]);

CREATE INDEX [IX_AppTrainees_SpecializationId] ON [AppTrainees] ([SpecializationId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250422005404_Added_TrainingManagement_Entities', N'9.0.4');

COMMIT;
GO

