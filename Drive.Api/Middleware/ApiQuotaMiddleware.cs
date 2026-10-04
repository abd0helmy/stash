using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Drive.Application.Billing.Common;
using Drive.Application.Billing.Usage.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Middleware;

public class ApiQuotaMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IQuotaService quotaService,
        IUsageService usageService)
    {
        // Only track authenticated API requests (skip webhooks and static/openapi endpoints)
        if (context.User.Identity?.IsAuthenticated == true &&
            context.Request.Path.StartsWithSegments("/api") &&
            !context.Request.Path.StartsWithSegments("/api/webhooks"))
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var quotaCheck = await quotaService.CanMakeRequestAsync(userId, context.RequestAborted);
                if (quotaCheck.IsFailure)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Too Many Requests",
                        Status = StatusCodes.Status429TooManyRequests,
                        Detail = quotaCheck.Error?.Description ?? "Monthly API request quota exceeded.",
                        Instance = context.Request.Path
                    };
                    problemDetails.Extensions["code"] = quotaCheck.Error?.Code ?? "Billing.ApiRequestQuotaExceeded";

                    await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
                    return;
                }

                // Proceed with the request
                await next(context);

                // Increment request counter asynchronously
                _ = usageService.IncrementApiRequestsAsync(userId, CancellationToken.None);
                return;
            }
        }

        await next(context);
    }
}
