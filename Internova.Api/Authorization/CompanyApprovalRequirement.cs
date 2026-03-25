using Internova.Core.Enums;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Internova.Api.Authorization;

public class CompanyApprovalRequirement : IAuthorizationRequirement { }

public class CompanyApprovalHandler(ICompanyProfileRepository companyRepository)
    : AuthorizationHandler<CompanyApprovalRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CompanyApprovalRequirement requirement)
    {
        var userIdClaim = context.User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var companyId))
        {
            return; // Not a valid user or not a company
        }

        var profile = await companyRepository.GetByCompanyIdAsync(companyId);
        if (profile != null && profile.Status == CompanyStatus.Active)
        {
            context.Succeed(requirement);
        }
    }
}
