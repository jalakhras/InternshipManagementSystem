-- Removes the rows the test tooling leaves behind.
--
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -d InternshipManagementSystem -i tools/purge-test-data.sql
--
-- The load test used to mint a fresh candidate for every virtual sitter on every
-- run, and those candidates then sat an exam — so the API will not delete them,
-- correctly: a candidate with attempts cannot be removed, because deleting the
-- person would leave a score belonging to nobody. That rule is right for real
-- people and wrong for six hundred rows called "Load m3f2x-17".
--
-- The load test now reuses a fixed pool and stops adding to this. This script is
-- for the ones already there.
--
-- The same is true of the live Playwright specs and the screenshot run, which
-- create a candidate per run and let it sit an exam. A hundred of them had piled
-- up in the host, where the only person who would ever look is somebody signing
-- in as the platform operator to see what the product looks like — and what they
-- saw was a roster of "screens-mtfm97dm@example.test", none of them in any class.
--
-- It touches only the address patterns the tooling itself generates:
--
--   load-…      tools/load-test.js
--   screens-…   angular/e2e/live/screenshot.spec.ts
--   live-…      angular/e2e/live/journey.spec.ts
--   diag-…, probe-…   throwaway probes
--
-- Nothing a person would type looks like these. Run it against a development
-- database.

SET NOCOUNT ON;

-- Required by the filtered indexes on these tables. sqlcmd defaults it off, and
-- without it every DELETE here is refused with a message about SET options that
-- says nothing about which one.
SET QUOTED_IDENTIFIER ON;

DECLARE @candidates TABLE (Id uniqueidentifier PRIMARY KEY);

INSERT INTO @candidates (Id)
SELECT Id FROM AppCandidates
WHERE Email LIKE 'load-%@example.test'
   OR Email LIKE 'screens-%@example.test'
   OR Email LIKE 'live-%@example.test'
   OR Email LIKE 'diag-%@example.test'
   OR Email LIKE 'probe-%@example.test';

DECLARE @attempts TABLE (Id uniqueidentifier PRIMARY KEY);

INSERT INTO @attempts (Id)
SELECT Id FROM AppAttempts WHERE CandidateId IN (SELECT Id FROM @candidates);

DECLARE @candidateCount int = (SELECT COUNT(*) FROM @candidates);
DECLARE @attemptCount int = (SELECT COUNT(*) FROM @attempts);

PRINT CONCAT('Load-test candidates: ', @candidateCount);
PRINT CONCAT('Their attempts:       ', @attemptCount);

-- Children first. Nothing here cascades, deliberately: an attempt's answers are
-- evidence, and a cascade delete on evidence is how a disputed result becomes
-- unanswerable.
DELETE FROM AppAnswers WHERE AttemptId IN (SELECT Id FROM @attempts);
DELETE FROM AppAttemptQuestions WHERE AttemptId IN (SELECT Id FROM @attempts);
DELETE FROM AppAttempts WHERE Id IN (SELECT Id FROM @attempts);

DELETE FROM AppExamLinks WHERE CandidateId IN (SELECT Id FROM @candidates);
DELETE FROM AppCandidateGroupMembers WHERE CandidateId IN (SELECT Id FROM @candidates);
DELETE FROM AppCandidates WHERE Id IN (SELECT Id FROM @candidates);

-- Assignments that now point at nobody. They were created one per load-test
-- candidate and are of no interest to anyone.
DELETE FROM AppAssignments
WHERE CandidateId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM AppCandidates c WHERE c.Id = AppAssignments.CandidateId);

PRINT 'Done.';

SELECT
    (SELECT COUNT(*) FROM AppCandidates) AS CandidatesRemaining,
    (SELECT COUNT(*) FROM AppAttempts) AS AttemptsRemaining;
