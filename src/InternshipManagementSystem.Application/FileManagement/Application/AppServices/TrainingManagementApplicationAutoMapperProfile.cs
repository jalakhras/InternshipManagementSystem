using AutoMapper;
using InternshipManagementSystem.TrainingManagement.DTOs.ExamAnswers;
using InternshipManagementSystem.TrainingManagement.DTOs.ExamAttempts;
using InternshipManagementSystem.TrainingManagement.DTOs.Exams;
using InternshipManagementSystem.TrainingManagement.DTOs.Questions;
using InternshipManagementSystem.TrainingManagement.DTOs.Specializations;
using InternshipManagementSystem.TrainingManagement.DTOs.Trainees;
using InternshipManagementSystem.TrainingManagement.ExamLinks.DTOs;

namespace InternshipManagementSystem.TrainingManagement
{
    public class TrainingManagementApplicationAutoMapperProfile : Profile
    {
        public TrainingManagementApplicationAutoMapperProfile()
        {
            CreateMap<Specialization, SpecializationDto>();
            CreateMap<CreateUpdateSpecializationDto, Specialization>();

            CreateMap<Trainee, TraineeDto>();
            CreateMap<CreateUpdateTraineeDto, Trainee>();

            CreateMap<Exam, ExamDto>();
            CreateMap<CreateUpdateExamDto, Exam>();

            CreateMap<Question, QuestionDto>();
            CreateMap<CreateUpdateQuestionDto, Question>();

            CreateMap<ExamAttempt, ExamAttemptDto>();
            CreateMap<CreateUpdateExamAttemptDto, ExamAttempt>();

            CreateMap<ExamAnswer, ExamAnswerDto>();
            CreateMap<CreateUpdateExamAnswerDto, ExamAnswer>();

            CreateMap<ExamLink, ExamLinkDto>();
            CreateMap<CreateExamLinkInput, ExamLink>();

        }
    }
}