using Drive.Application.Billing.Plans.DTOs;
using Drive.Core.Common.Result;

namespace Drive.Application.Billing.Plans.Interfaces;

public interface IPlanService
{
    Task<Result<IEnumerable<PlanResponse>>> GetPlansAsync(CancellationToken cancellationToken = default);

    Task<Result<PlanResponse>> GetPlanByIdAsync(Guid planId, CancellationToken cancellationToken = default);
}
