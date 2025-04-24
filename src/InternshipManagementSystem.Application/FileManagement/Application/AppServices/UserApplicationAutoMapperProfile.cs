using AutoMapper;
using InternshipManagementSystem.IdentityManagement;
using InternshipManagementSystem.IdentityManagement.DTOs;
using Volo.Abp.Identity;

namespace InternshipManagementSystem
{
    public class UserApplicationAutoMapperProfile : Profile
    {
        public UserApplicationAutoMapperProfile()
        {
            CreateMap<IdentityUser, UserDto>();
            CreateMap<IdentityUserDto, UserDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber));

            CreateMap<CreateUpdateUserDto, IdentityUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber));
        }
    }
}