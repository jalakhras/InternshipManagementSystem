using InternshipManagementSystem.TrainingManagement;
using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

public class Exam : AuditedAggregateRoot<Guid>
{
    public string Title { get; set; }
    public string Description { get; set; }
    public Guid SpecializationId { get; set; }
    public string Level { get; set; } // Beginner / Intermediate / Advanced
    public int TimeLimitInMinutes { get; set; } // الوقت الكامل للامتحان
    public int TotalQuestions { get; set; }
    public bool IsActive { get; set; }
    public bool AllowQuestionTimeLimit { get; set; } // هل الامتحان يسمح بتحديد وقت لكل سؤال؟

    public bool IsScheduled { get; set; } = false; // هل الامتحان مجدول أم مفتوح دائمًا
    public DateTime? ScheduledStartTime { get; set; } // بداية وقت التوفر
    public DateTime? ScheduledEndTime { get; set; } // نهاية وقت التوفر
    public Specialization Specialization { get; set; }
    public ICollection<Question> Questions { get; set; }
    public ICollection<ExamAttempt> ExamAttempts { get; set; }
}