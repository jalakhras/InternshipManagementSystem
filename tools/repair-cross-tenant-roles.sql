-- Repoints a user's role to the same-named role in that user's own organisation.
--
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -d InternshipManagementSystem \
--          -i tools\repair-cross-tenant-roles.sql
--
-- Set @DryRun to 0 below to actually change anything. It reports and rolls back
-- otherwise. A plain T-SQL variable, not a sqlcmd one: a `:setvar` in a file
-- silently overrides the `-v` on the command line, so a run meant to be a dry
-- run commits while saying it was a dry run.
--
-- Why this exists. A role carries a tenant id, and ABP resolves a user's roles
-- through the multi-tenant filter — so a user linked to a role belonging to a
-- different organisation has, as far as the application is concerned, no roles
-- at all. The account signs in perfectly, lands on the one page that needs no
-- permission, and every menu entry is hidden. Nothing fails. Nothing is logged.
-- It reads exactly like "this account was never given anything", which is the
-- one explanation that is wrong.
--
-- This happened here to two host administrators, left over from when the seeder
-- created roles with a null tenant id and looked them up by name across every
-- organisation. The seeder passes the tenant now; this repairs the rows it wrote
-- before it did.
--
-- A link with no counterpart in the user's own tenant is reported and left
-- alone. Inventing a role for somebody is not a repair.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @DryRun bit = 1;

DROP TABLE IF EXISTS #Wrong;

SELECT
    ur.UserId,
    ur.RoleId               AS WrongRoleId,
    u.UserName,
    r.Name                  AS RoleName,
    ISNULL(CONVERT(varchar(40), u.TenantId), 'host') AS UserTenant,
    ISNULL(CONVERT(varchar(40), r.TenantId), 'host') AS RoleTenant,
    mine.Id                 AS RightRoleId
INTO #Wrong
FROM AbpUserRoles ur
JOIN AbpUsers u ON u.Id = ur.UserId
JOIN AbpRoles r ON r.Id = ur.RoleId
LEFT JOIN AbpRoles mine
       ON mine.NormalizedName = r.NormalizedName
      AND ISNULL(CONVERT(varchar(40), mine.TenantId), 'host')
        = ISNULL(CONVERT(varchar(40), u.TenantId), 'host')
WHERE ISNULL(CONVERT(varchar(40), u.TenantId), 'host')
   <> ISNULL(CONVERT(varchar(40), r.TenantId), 'host');

DECLARE @found int = (SELECT COUNT(*) FROM #Wrong);

PRINT CONCAT('cross-tenant role links found: ', @found);

SELECT UserName, UserTenant, RoleName, RoleTenant,
       CASE WHEN RightRoleId IS NULL
            THEN 'no such role in this user''s own tenant — left alone'
            ELSE 'repointed' END AS Action
FROM #Wrong;

BEGIN TRANSACTION;

-- Somebody who already holds the right role as well only needs the wrong link
-- gone; (UserId, RoleId) is the key and the update would collide.
DELETE ur
FROM AbpUserRoles ur
JOIN #Wrong w ON w.UserId = ur.UserId AND w.WrongRoleId = ur.RoleId
WHERE w.RightRoleId IS NOT NULL
  AND EXISTS (SELECT 1 FROM AbpUserRoles keep
              WHERE keep.UserId = w.UserId AND keep.RoleId = w.RightRoleId);

UPDATE ur SET RoleId = w.RightRoleId
FROM AbpUserRoles ur
JOIN #Wrong w ON w.UserId = ur.UserId AND w.WrongRoleId = ur.RoleId
WHERE w.RightRoleId IS NOT NULL;

IF @DryRun = 1
BEGIN
    PRINT 'dry run — rolled back, nothing was changed';
    ROLLBACK TRANSACTION;
END
ELSE
BEGIN
    COMMIT TRANSACTION;
    PRINT 'done';
END

DROP TABLE IF EXISTS #Wrong;
