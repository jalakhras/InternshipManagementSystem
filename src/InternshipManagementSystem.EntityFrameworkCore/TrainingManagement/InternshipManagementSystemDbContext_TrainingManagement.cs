using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            });

            builder.Entity<Exam>(b =>
            {
                b.ToTable("AppExams");
                b.ConfigureByConvention();

                b.Property(x => x.Title)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasComment("عنوان الاختبار");

                b.Property(x => x.Description)
                    .HasComment("وصف الاختبار");

                b.Property(x => x.SpecializationId)
                    .HasComment("التخصص المرتبط بالاختبار");

                b.Property(x => x.Level)
                    .HasComment("مستوى الاختبار (مبتدئ/متوسط/متقدم)");

                b.Property(x => x.TimeLimitInMinutes)
                    .HasComment("الوقت المحدد بالدقائق للامتحان");

                b.Property(x => x.TotalQuestions)
                    .HasComment("عدد الأسئلة بالاختبار");

                b.Property(x => x.IsActive)
                    .HasComment("هل الاختبار مفعل؟");
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
                    .HasComment("نوع السؤال (اختياري/صح وخطأ/نصي)");

                b.Property(x => x.OptionsJson)
                    .HasComment("خيارات السؤال (مخزنة كـ JSON)");

                b.Property(x => x.CorrectAnswer)
                    .IsRequired()
                    .HasMaxLength(512)
                    .HasComment("الإجابة الصحيحة للسؤال");
            });

            builder.Entity<ExamAttempt>(b =>
            {
                b.ToTable("AppExamAttempts");
                b.ConfigureByConvention();

                b.Property(x => x.StartTime).HasComment("وقت بدء الامتحان");
                b.Property(x => x.EndTime).HasComment("وقت إنهاء الامتحان");
                b.Property(x => x.Score).HasComment("نتيجة الامتحان");
                b.Property(x => x.IsPassed).HasComment("هل المتدرب نجح بالامتحان؟");

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
            });
        }
    }
}