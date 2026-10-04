using Drive.Application.Billing.Usage.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Controllers;

[Authorize]
public class UsageController(IUsageService usageService) : BaseApiController
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUsage(CancellationToken cancellationToken)
    {
        var result = await usageService.GetCurrentUsageAsync(CurrentUserId, cancellationToken);
        return HandleResult(result);
    }
}
