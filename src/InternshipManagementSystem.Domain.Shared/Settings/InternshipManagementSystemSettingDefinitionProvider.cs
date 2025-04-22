using InternshipManagementSystem.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace InternshipManagementSystem.Settings
{
    public class InternshipManagementSystemSettingDefinitionProvider : SettingDefinitionProvider
    {
        public override void Define(ISettingDefinitionContext context)
        {
            context.Add(
                new SettingDefinition(
                    InternshipManagementSystemSettings.EnableSelfRegistration,
                    "false", // القيمة الافتراضية
                    L("DisplayName:EnableSelfRegistration"),
                    isVisibleToClients: true
                )
            );
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<InternshipManagementSystemResource>(name);
        }
    }
}
