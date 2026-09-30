using Drive.Application.Billing.Plans.Interfaces;
using Drive.Core.Entities;
using Drive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Repositories;

public class PlanRepository(DriveDbContext context) : IPlanRepository
{
    public async Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Plans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Plan?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        return await context.Plans.FirstOrDefaultAsync(
            p => p.Name.ToLower() == normalized,
            cancellationToken);
    }

    public async Task<IEnumerable<Plan>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await context.Plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        await context.Plans.AddAsync(plan, cancellationToken);
    }

    public void Update(Plan plan)
    {
        context.Plans.Update(plan);
    }
}
