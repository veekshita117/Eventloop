using Eventloop.Core.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Eventloop.Tests;

public class RolePolicyTests
{
    private readonly IAuthorizationService _authorizationService;
    private readonly AuthorizationOptions _authOptions;

    public RolePolicyTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole(UserRole.Admin.ToString()));

            options.AddPolicy("RequireOrganizer", policy =>
                policy.RequireRole(UserRole.Organizer.ToString(), UserRole.Admin.ToString()));
        });

        var provider = services.BuildServiceProvider();
        _authorizationService = provider.GetRequiredService<IAuthorizationService>();
        _authOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
    }

    [Fact]
    public void RequireAdminPolicy_ShouldRequireAdminRole()
    {
        var policy = _authOptions.GetPolicy("RequireAdmin");
        policy.Should().NotBeNull();

        var roleReq = policy!.Requirements.OfType<RolesAuthorizationRequirement>().FirstOrDefault();
        roleReq.Should().NotBeNull();
        roleReq!.AllowedRoles.Should().Contain(UserRole.Admin.ToString());
        roleReq.AllowedRoles.Should().NotContain(UserRole.Organizer.ToString());
        roleReq.AllowedRoles.Should().NotContain(UserRole.Student.ToString());
    }

    [Fact]
    public void RequireOrganizerPolicy_ShouldAllowBothOrganizerAndAdmin()
    {
        var policy = _authOptions.GetPolicy("RequireOrganizer");
        policy.Should().NotBeNull();

        var roleReq = policy!.Requirements.OfType<RolesAuthorizationRequirement>().FirstOrDefault();
        roleReq.Should().NotBeNull();
        roleReq!.AllowedRoles.Should().Contain(UserRole.Organizer.ToString());
        roleReq.AllowedRoles.Should().Contain(UserRole.Admin.ToString());
        roleReq.AllowedRoles.Should().NotContain(UserRole.Student.ToString());
    }
}
