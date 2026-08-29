-- Removes the duplicate host users and roles left behind by the seeder bug.
--
-- IdentityRole's tenant id defaulted to null, so every tenant pass wrote its
-- roles into the host instead, and the lookup guarding the insert could not see
-- them through the multi-tenant filter. Nineteen startups produced nineteen
-- roles named "Supervisor" and nineteen copies of each development account. The
-- seeder no longer does this; the rows it already wrote are still there, and
-- this removes them.
--
-- Which copy survives: the most recently modified, because that is where a
-- changed password or an edited profile lives. Failing that, the oldest — the
-- one anything else in the database was most likely pointed at.
--
-- Nothing outside ABP's own identity tables has a foreign key to either table,
-- so the blast radius is memberships and claims, and both are carried over.
--
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -d InternshipManagementSystem \
--          -i tools\dedupe-identity.sql
--
-- Set @DryRun to 0 below to actually remove anything. It reports and rolls back
-- otherwise.
--
-- Deliberately a plain T-SQL variable and not a sqlcmd one: a `:setvar` in the
-- file silently overrides the `-v` on the command line, so a run meant to be a
-- dry run commits instead and says it was a dry run while doing it. That is not
-- a switch to hand somebody for a script that deletes accounts.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @DryRun bit = 1;

DROP TABLE IF EXISTS #Users, #DropUsers, #Roles, #RoleMap;

-- ------------------------------------------------------------------- users

SELECT Id, TenantId, UserName,
       Keep = ROW_NUMBER() OVER (
           PARTITION BY TenantId, NormalizedUserName
           ORDER BY ISNULL(LastModificationTime, '1900-01-01') DESC, CreationTime ASC)
INTO #Users
FROM AbpUsers;

SELECT Id, TenantId, UserName INTO #DropUsers FROM #Users WHERE Keep > 1;

DECLARE @droppedUsers int = (SELECT COUNT(*) FROM #DropUsers);

PRINT CONCAT('duplicate users to remove: ', @droppedUsers);

SELECT ISNULL(CONVERT(varchar(40), TenantId), 'host') AS Tenant,
       UserName, COUNT(*) AS Removing
FROM #DropUsers GROUP BY TenantId, UserName ORDER BY Removing DESC;

-- ------------------------------------------------------------------- roles

SELECT Id, TenantId, Name, NormalizedName,
       Keep = ROW_NUMBER() OVER (
           PARTITION BY TenantId, NormalizedName
           ORDER BY CreationTime ASC, Id ASC)
INTO #Roles
FROM AbpRoles;

-- The survivor of each group, so memberships can be moved onto it rather than
-- deleted. Somebody who held a duplicate Supervisor role still holds Supervisor.
SELECT d.Id AS DropId, d.Name, d.TenantId, k.Id AS KeepId
INTO #RoleMap
FROM #Roles d
JOIN #Roles k
  ON k.NormalizedName = d.NormalizedName
 AND ISNULL(k.TenantId, '00000000-0000-0000-0000-000000000000')
   = ISNULL(d.TenantId, '00000000-0000-0000-0000-000000000000')
 AND k.Keep = 1
WHERE d.Keep > 1;

DECLARE @droppedRoles int = (SELECT COUNT(*) FROM #RoleMap);

PRINT CONCAT('duplicate roles to remove: ', @droppedRoles);

SELECT ISNULL(CONVERT(varchar(40), TenantId), 'host') AS Tenant,
       Name, COUNT(*) AS Removing
FROM #RoleMap GROUP BY TenantId, Name ORDER BY Removing DESC;

-- Everything that changes a row happens from here down, so a dry run has one
-- rollback to undo all of it.
BEGIN TRANSACTION;

DELETE FROM AbpUserRoles             WHERE UserId IN (SELECT Id FROM #DropUsers);
DELETE FROM AbpUserClaims            WHERE UserId IN (SELECT Id FROM #DropUsers);
DELETE FROM AbpUserLogins            WHERE UserId IN (SELECT Id FROM #DropUsers);
DELETE FROM AbpUserTokens            WHERE UserId IN (SELECT Id FROM #DropUsers);
DELETE FROM AbpUserOrganizationUnits WHERE UserId IN (SELECT Id FROM #DropUsers);
DELETE FROM AbpUserPasskeys          WHERE UserId IN (SELECT Id FROM #DropUsers);
DELETE FROM AbpUserPasswordHistories WHERE UserId IN (SELECT Id FROM #DropUsers);
DELETE FROM AbpUsers                 WHERE Id     IN (SELECT Id FROM #DropUsers);

-- Repoint first, but only where it does not collide: (UserId, RoleId) is the
-- key, and somebody holding two copies of one role would collapse onto a row
-- that already exists.
DELETE ur
FROM AbpUserRoles ur
JOIN #RoleMap m ON m.DropId = ur.RoleId
WHERE EXISTS (SELECT 1 FROM AbpUserRoles keep
              WHERE keep.UserId = ur.UserId AND keep.RoleId = m.KeepId);

UPDATE ur SET RoleId = m.KeepId
FROM AbpUserRoles ur JOIN #RoleMap m ON m.DropId = ur.RoleId;

DELETE our
FROM AbpOrganizationUnitRoles our
JOIN #RoleMap m ON m.DropId = our.RoleId
WHERE EXISTS (SELECT 1 FROM AbpOrganizationUnitRoles keep
              WHERE keep.OrganizationUnitId = our.OrganizationUnitId
                AND keep.RoleId = m.KeepId);

UPDATE our SET RoleId = m.KeepId
FROM AbpOrganizationUnitRoles our JOIN #RoleMap m ON m.DropId = our.RoleId;

-- A clone's claims are its own copy of what the survivor already carries.
DELETE FROM AbpRoleClaims WHERE RoleId IN (SELECT DropId FROM #RoleMap);
DELETE FROM AbpRoles      WHERE Id     IN (SELECT DropId FROM #RoleMap);

-- Permission grants are keyed by role *name*, not id, so the survivor already
-- owns every grant the clones appeared to have. Nothing to move.

IF @DryRun = 1
BEGIN
    PRINT 'dry run — rolled back, nothing was removed';
    ROLLBACK TRANSACTION;
END
ELSE
BEGIN
    COMMIT TRANSACTION;
    PRINT 'done';
END

DROP TABLE IF EXISTS #Users, #DropUsers, #Roles, #RoleMap;
