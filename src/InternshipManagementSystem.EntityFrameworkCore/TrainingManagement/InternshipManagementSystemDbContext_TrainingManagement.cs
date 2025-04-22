using InternshipManagementSystem.Candidate;
using InternshipManagementSystem.TrainingManagement;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
namespace InternshipManagementSystem.EntityFrameworkCore
{
    public partial class InternshipManagementSystemDbContext
    {
        public DbSet<Trainee> Trainees { get; set; }
        public DbSet<Specialization> Specializations { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }
        public DbSet<ExamAnswer> ExamAnswers { get; set; }
        public DbSet<InternshipManagementSystem.Candidate.Candidate> Candidates { get; set; }
        public DbSet<CandidateExamAttempt> CandidateExamAttempts { get; set; }

        protected void ConfigureTrainingManagement(ModelBuilder builder)
        {
            builder.Entity<Specialization>(b =>
            {
                b.ToTable("AppSpecializations");
                b.ConfigureByConvention();

                b.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasComment("اسم التخصص");
            });

            builder.Entity<Trainee>(b =>
            {
                b.ToTable("AppTrainees");
                b.ConfigureByConvention();

                b.Property(x => x.FullName)
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasComment("اسم المتدرب الكامل");

                b.Property(x => x.EmployeeNumber)
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasComment("الرقم الوظيفي للمتدرب");

                b.Property(x => x.SpecializationId)
                    .IsRequired()
                    .HasComment("معرّف التخصص المرتبط بالمتدرب");
                b.Property(x => x.UserId)
        .HasComment("معرّف المستخدم المرتبط بالمتدرب (اختياري)");
            });

            builder.Entity<Exam>(b =>
            {
                b.ToTable("AppExams");
                b.ConfigureByConvention();

                b.Property(x => x.Title)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasComment("عنوان الامتحان");

                b.Property(x => x.Description)
                    .HasComment("وصف مختصر للامتحان");

                b.Property(x => x.SpecializationId)
                    .HasComment("التخصص المرتبط بالامتحان");

                b.Property(x => x.Level)
                    .HasComment("مستوى الامتحان (مبتدئ/متوسط/متقدم)");

                b.Property(x => x.TimeLimitInMinutes)
                    .HasComment("المدة الإجمالية المسموح بها لحل الامتحان");

                b.Property(x => x.TotalQuestions)
                    .HasComment("عدد الأسئلة الكلي");

                b.Property(x => x.IsActive)
                    .HasComment("هل الامتحان مفعل أم لا");

                b.Property(x => x.AllowQuestionTimeLimit)
                    .HasDefaultValue(false)
                    .HasComment("هل يُسمح بتحديد وقت لكل سؤال بشكل مستقل؟");
            });

            builder.Entity<Question>(b =>
            {
                b.ToTable("AppQuestions");
                b.ConfigureByConvention();

                b.Property(x => x.Text)
                    .IsRequired()
                    .HasMaxLength(1024)
                    .HasComment("نص السؤال");

                b.Property(x => x.Type)
                    .IsRequired()
                    .HasComment("نوع السؤال");

                b.Property(x => x.OptionsJson)
                    .HasComment("خيارات السؤال بصيغة JSON");

                b.Property(x => x.CorrectAnswer)
                    .IsRequired()
                    .HasMaxLength(512)
                    .HasComment("الإجابة الصحيحة للسؤال");

                b.Property(x => x.Score)
                    .IsRequired()
                    .HasComment("عدد النقاط المخصصة لهذا السؤال");

                b.Property(x => x.TimeLimitInSeconds)
                    .HasComment("الحد الزمني لهذا السؤال بالثواني (اختياري)");

                b.Property(x => x.MediaUrl)
                    .HasMaxLength(1024)
                    .HasComment("رابط وسائط السؤال (صورة، صوت، فيديو)");

                b.Property(x => x.AllowPartialCredit)
                    .HasComment("السماح بالحصول على درجات جزئية للأسئلة متعددة الخيارات");
            });


            builder.Entity<ExamAttempt>(b =>
            {
                b.ToTable("AppExamAttempts");
                b.ConfigureByConvention();

                b.Property(x => x.StartTime).HasComment("وقت بدء الامتحان");
                b.Property(x => x.EndTime).HasComment("وقت إنهاء الامتحان");
                b.Property(x => x.Score).HasComment("نتيجة الامتحان");
                b.Property(x => x.IsPassed).HasComment("هل المتدرب نجح بالامتحان؟");
                b.Property(x => x.IsGraded)
        .HasDefaultValue(false)
        .HasComment("هل تم تصحيح المحاولة تلقائيًا أو يدويًا");

                b.Property(x => x.NeedsManualReview)
                    .HasDefaultValue(false)
                    .HasComment("هل تحتوي المحاولة على أسئلة تحتاج مراجعة يدوية");
                b.HasOne(x => x.Trainee)
                 .WithMany(x => x.ExamAttempts)
                 .HasForeignKey(x => x.TraineeId)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Exam)
                 .WithMany(x => x.ExamAttempts)
                 .HasForeignKey(x => x.ExamId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ExamAnswer>(b =>
            {
                b.ToTable("AppExamAnswers");
                b.ConfigureByConvention();

                b.Property(x => x.Answer)
                    .HasComment("إجابة المتدرب للسؤال");
                b.Property(x => x.IsCorrect)
        .HasComment("هل الإجابة صحيحة (مراجعة تلقائية أو يدوية)");

                b.Property(x => x.PartialScore)
                    .HasComment("الدرجة الجزئية لهذا الجواب إن وجدت");

                b.Property(x => x.ReviewComments)
                    .HasMaxLength(1024)
                    .HasComment("ملاحظات المدقق اليدوي للإجابة");
            });

            builder.Entity<InternshipManagementSystem.Candidate.Candidate>(b =>
            {
                b.ToTable("AppCandidates");
                b.ConfigureByConvention();

                b.Property(x => x.FullName)
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasComment("اسم المرشح الكامل");

                b.Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasComment("البريد الإلكتروني للمرشح");

                b.Property(x => x.PhoneNumber)
                    .HasMaxLength(32)
                    .HasComment("رقم هاتف المرشح");

                b.Property(x => x.PositionAppliedFor)
                    .HasMaxLength(128)
                    .HasComment("الوظيفة المتقدم لها المرشح");

                b.Property(x => x.Status)
                    .HasComment("حالة المرشح (قيد التقييم / ناجح / راسب)");
            });

            builder.Entity<CandidateExamAttempt>(b =>
            {
                b.ToTable("AppCandidateExamAttempts");
                b.ConfigureByConvention();

                b.Property(x => x.StartTime)
                    .HasComment("وقت بدء محاولة الامتحان");

                b.Property(x => x.EndTime)
                    .HasComment("وقت انتهاء محاولة الامتحان");

                b.Property(x => x.Score)
                    .HasComment("نتيجة الامتحان");

                b.Property(x => x.IsPassed)
                    .HasComment("هل اجتاز المرشح الامتحان بنجاح");
            });

        }
    }
}