using Internova.Api.Presentation.Extensions;
using Internova.Api.Domain.Enums;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Internova.Api.Presentation.Authorization;

public class CompanyApprovalRequirement : IAuthorizationRequirement;

public class CompanyApprovalHandler(ICompanyProfileRepository companyRepository)
    : AuthorizationHandler<CompanyApprovalRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CompanyApprovalRequirement requirement)
    {
        if (!context.User.TryGetUserId(out var companyId))
        {
            return;
        }

        var profile = await companyRepository.GetByCompanyIdAsync(companyId);
        if (profile is { Status: CompanyStatus.Active })
        {
            context.Succeed(requirement);
        }
    }
}
