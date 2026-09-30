using System.ComponentModel.DataAnnotations;

namespace Drive.Application.Billing.Subscriptions.DTOs;

public record CheckoutRequest(
    [Required] Guid PlanId
);

public record CheckoutResponse(
    string CheckoutUrl
);
