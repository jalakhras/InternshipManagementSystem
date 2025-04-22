using System;

using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.TrainingManagement.IRepositories
{
    public interface ITraineeRepository : IRepository<Trainee, Guid>
    {
        // يمكنك لاحقاً إضافة دوال مخصصة إن احتجت
    }
}