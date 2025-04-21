using System.Threading.Tasks;

namespace InternshipManagementSystem.Data;

public interface IInternshipManagementSystemDbSchemaMigrator
{
    Task MigrateAsync();
}
