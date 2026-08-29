using System.Collections.Generic;
using System.Linq;
using InternshipManagementSystem.IdentityManagement.DTOs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;

namespace InternshipManagementSystem.IdentityManagement;

/// <summary>
/// Maps identity users onto their DTOs.
/// <para>
/// Written by hand rather than configured in AutoMapper. AutoMapper 14 carries a
/// known high-severity advisory (GHSA-rvv3-g6hj-g44x) and moved to a commercial
/// licence, which is why ABP 10 dropped it from its defaults. With only three maps
/// left in the project after the domain rewrite, keeping a vulnerable dependency to
/// avoid writing thirty lines was not a trade worth making.
/// </para>
/// <para>
/// ABP resolves <see cref="IObjectMapper{TSource,TDestination}"/> implementations
/// automatically, so <c>ObjectMapper.Map</c> call sites are unchanged.
/// </para>
/// </summary>
public class IdentityUserToUserDtoMapper : IObjectMapper<IdentityUser, UserDto>, ITransientDependency
{
    public UserDto Map(IdentityUser source) => Map(source, new UserDto());

    public UserDto Map(IdentityUser source, UserDto destination)
    {
        destination.Id = source.Id;
        destination.UserName = source.UserName;
        destination.Email = source.Email;
        destination.PhoneNumber = source.PhoneNumber;
        destination.FullName = source.Name;
        return destination;
    }
}

/// <summary>Batch form, so list call sites resolve without falling back to reflection.</summary>
public class IdentityUserListToUserDtoListMapper
    : IObjectMapper<List<IdentityUser>, List<UserDto>>, ITransientDependency
{
    private readonly IdentityUserToUserDtoMapper _single;

    public IdentityUserListToUserDtoListMapper(IdentityUserToUserDtoMapper single)
    {
        _single = single;
    }

    public List<UserDto> Map(List<IdentityUser> source) => source.Select(_single.Map).ToList();

    public List<UserDto> Map(List<IdentityUser> source, List<UserDto> destination)
    {
        destination.Clear();
        destination.AddRange(source.Select(_single.Map));
        return destination;
    }
}
