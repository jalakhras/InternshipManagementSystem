using System;

namespace InternshipManagementSystem.TrainingManagement.DTOs.Trainees
{
    public class CreateUpdateTraineeDto
    {
        public string FullName { get; set; }
        public string EmployeeNumber { get; set; }
        public Guid SpecializationId { get; set; }
        public Guid? UserId { get; set; }
    }
}