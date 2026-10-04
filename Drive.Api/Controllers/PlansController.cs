using Drive.Application.Billing.Plans.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Controllers;

public class PlansController(IPlanService planService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await planService.GetPlansAsync(cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await planService.GetPlanByIdAsync(id, cancellationToken);
        return HandleResult(result);
    }
}
