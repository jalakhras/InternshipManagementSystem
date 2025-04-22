using System;

namespace InternshipManagementSystem.TrainingManagement.DTOs.ExamAttempts
{
    public class CreateUpdateExamAttemptDto
    {
        public Guid ExamId { get; set; } // معرّف الامتحان الذي يتم تقديمه
        public Guid TraineeId { get; set; } // معرّف المتدرب الذي يقدم الامتحان
        public DateTime StartTime { get; set; } // وقت بدء الامتحان
        public DateTime EndTime { get; set; } // وقت انتهاء الامتحان
        public double Score { get; set; } // نتيجة الامتحان
        public bool IsPassed { get; set; } // هل نجح أم لا
    }
}