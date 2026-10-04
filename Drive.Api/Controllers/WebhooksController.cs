using Drive.Application.Billing.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Controllers;

[AllowAnonymous]
public class WebhooksController(IStripeWebhookService stripeWebhookService) : BaseApiController
{
    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var jsonPayload = await reader.ReadToEndAsync(cancellationToken);

        var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

        var result = await stripeWebhookService.HandleWebhookAsync(
            jsonPayload,
            stripeSignature,
            cancellationToken);

        return HandleResult(result);
    }
}
