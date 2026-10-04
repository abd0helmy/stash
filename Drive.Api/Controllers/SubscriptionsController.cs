using Drive.Application.Billing.Subscriptions.DTOs;
using Drive.Application.Billing.Subscriptions.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Controllers;

[Authorize]
public class SubscriptionsController(ISubscriptionService subscriptionService) : BaseApiController
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentSubscription(CancellationToken cancellationToken)
    {
        var result = await subscriptionService.GetActiveSubscriptionAsync(CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckoutSession(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await subscriptionService.CreateCheckoutSessionAsync(
            CurrentUserId,
            request.PlanId,
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new CheckoutResponse(result.Value!));
        }

        return HandleResult(result);
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelSubscription(CancellationToken cancellationToken)
    {
        var result = await subscriptionService.CancelSubscriptionAsync(CurrentUserId, cancellationToken);
        return HandleResult(result);
    }
}
