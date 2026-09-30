using Drive.Application.Billing.Plans.DTOs;
using Drive.Application.Billing.Plans.Interfaces;
using Drive.Core.Common;
using Drive.Core.Common.Result;
using Drive.Core.Entities;

namespace Drive.Application.Billing.Plans.Services;

public class PlanService(IPlanRepository planRepository) : IPlanService
{
    public async Task<Result<IEnumerable<PlanResponse>>> GetPlansAsync(CancellationToken cancellationToken = default)
    {
        var plans = await planRepository.GetAllActiveAsync(cancellationToken);
        return Result<IEnumerable<PlanResponse>>.Success(plans.Select(MapToResponse));
    }

    public async Task<Result<PlanResponse>> GetPlanByIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
        {
            return Result<PlanResponse>.Failure(Error.PlanNotFound);
        }

        return Result<PlanResponse>.Success(MapToResponse(plan));
    }

    public static PlanResponse MapToResponse(Plan plan)
    {
        var features = new List<string>
        {
            $"{FormatBytes(plan.StorageLimitBytes)} Storage",
            $"{plan.MonthlyRequestLimit:N0} API Requests/month",
            $"{FormatBytes(plan.MaxFileSizeBytes)} Max File Size"
        };

        if (plan.Price > 0)
        {
            features.Add("Priority Support");
            features.Add("Advanced Sharing & Collaboration");
        }

        if (plan.Price >= 10)
        {
            features.Add("Audit Logs & Compliance");
            features.Add("Dedicated Account Manager");
        }

        return new PlanResponse(
            plan.Id,
            plan.Name,
            plan.Price,
            plan.Currency,
            plan.BillingInterval,
            plan.StorageLimitBytes,
            plan.MonthlyRequestLimit,
            plan.MaxFileSizeBytes,
            plan.IsActive,
            features
        );
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:0.##} {suffixes[counter]}";
    }
}
