namespace Drive.Infrastructure.Billing.Stripe;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string PublishableKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public string SuccessUrl { get; set; } = "https://drive.app/billing/success?session_id={CHECKOUT_SESSION_ID}";

    public string CancelUrl { get; set; } = "https://drive.app/billing/cancel";
}
