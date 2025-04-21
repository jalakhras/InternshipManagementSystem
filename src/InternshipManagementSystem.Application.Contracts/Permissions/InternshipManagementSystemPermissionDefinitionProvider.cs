using InternshipManagementSystem.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace InternshipManagementSystem.Permissions;

public class InternshipManagementSystemPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(InternshipManagementSystemPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(InternshipManagementSystemPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<InternshipManagementSystemResource>(name);
    }
}
