using Drive.Core.Common.Result;

namespace Drive.Application.Billing.Common;

public interface IStripeWebhookService
{
    Task<Result> HandleWebhookAsync(
        string jsonPayload,
        string? stripeSignature,
        CancellationToken cancellationToken = default);
}
