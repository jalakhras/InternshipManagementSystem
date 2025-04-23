BEGIN TRANSACTION;
CREATE TABLE [AppCandidateExamAnswers] (
    [Id] uniqueidentifier NOT NULL,
    [CandidateExamAttemptId] uniqueidentifier NOT NULL,
    [QuestionId] uniqueidentifier NOT NULL,
    [Answer] nvarchar(4000) NOT NULL,
    [AnswerFileUrl] nvarchar(512) NULL,
    [AnswerFileName] nvarchar(256) NULL,
    [ExtraProperties] nvarchar(max) NOT NULL,
    [ConcurrencyStamp] nvarchar(40) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [CreatorId] uniqueidentifier NULL,
    [LastModificationTime] datetime2 NULL,
    [LastModifierId] uniqueidentifier NULL,
    CONSTRAINT [PK_AppCandidateExamAnswers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppCandidateExamAnswers_AppCandidateExamAttempts_CandidateExamAttemptId] FOREIGN KEY ([CandidateExamAttemptId]) REFERENCES [AppCandidateExamAttempts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AppCandidateExamAnswers_AppQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [AppQuestions] ([Id])
);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'الإجابة النصية للمرشح';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppCandidateExamAnswers', 'COLUMN', N'Answer';
SET @description = N'رابط الملف المرفق للإجابة';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppCandidateExamAnswers', 'COLUMN', N'AnswerFileUrl';
SET @description = N'اسم الملف المرفق';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'AppCandidateExamAnswers', 'COLUMN', N'AnswerFileName';

CREATE INDEX [IX_AppCandidateExamAnswers_CandidateExamAttemptId] ON [AppCandidateExamAnswers] ([CandidateExamAttemptId]);

CREATE INDEX [IX_AppCandidateExamAnswers_QuestionId] ON [AppCandidateExamAnswers] ([QuestionId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250423235021_Added_CandidateExamAnswer_Entity_Relation', N'9.0.4');

COMMIT;
GO

