using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.TrainingManagement.DTOs.Specializations
{
    public class SpecializationDto : AuditedEntityDto<Guid>
    {
        public string Name { get; set; }
    }
}
