using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternshipManagementSystem.TrainingManagement.DTOs.Trainees
{
    public class CreateUpdateTraineeDto
    {
        public string FullName { get; set; }
        public string EmployeeNumber { get; set; }
        public Guid SpecializationId { get; set; }
    }

}
