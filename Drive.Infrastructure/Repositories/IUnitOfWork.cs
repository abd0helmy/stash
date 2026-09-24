using Drive.Core.Interfaces;
using Drive.Infrastructure.Persistence;

namespace Drive.Infrastructure.Repositories;

public class UnitOfWork(DriveDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }
}