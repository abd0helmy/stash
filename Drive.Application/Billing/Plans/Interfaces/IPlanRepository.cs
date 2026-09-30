using Drive.Core.Entities;

namespace Drive.Application.Billing.Plans.Interfaces;

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Plan?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IEnumerable<Plan>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Plan plan, CancellationToken cancellationToken = default);

    void Update(Plan plan);
}
