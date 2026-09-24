using Drive.Application.Folders.Interfaces;
using Drive.Application.Interfaces;
using Drive.Core.Entities;
using Drive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Repositories;

public class FolderRepository(DriveDbContext context) : IFolderRepository
{
    public async Task<Folder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Folders.FindAsync([id], cancellationToken);
    }

    public async Task<Folder?> GetByIdAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await context.Folders.FirstOrDefaultAsync(folder => folder.Id == id && folder.OwnerId == ownerId,
            cancellationToken);
    }

    public async Task<IEnumerable<Folder>> GetChildrenAsync(
        Guid? parentFolderId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await context.Folders
            .Where(f =>
                f.OwnerId == ownerId &&
                f.ParentFolderId == parentFolderId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsDescendantOfAsync(Guid folderId, Guid ancestorId, Guid ownerId, CancellationToken ct)
    {
        Guid? currentId = folderId;

        while (currentId.HasValue)
        {
            if (currentId.Value == ancestorId)
                return true;

            var id = currentId;
            currentId = await context.Folders
                .Where(f => f.Id == id.Value && f.OwnerId == ownerId)
                .Select(f => f.ParentFolderId)
                .FirstOrDefaultAsync(ct);
        }

        return false;
    }

    public async Task<List<Folder>> GetDescendantsAsync(Guid folderId, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var result = new List<Folder>();
        var currentLevel = new List<Guid> { folderId };

        while (currentLevel.Count > 0)
        {
            var level = currentLevel;
            var children = await context.Folders
                .Where(f => f.OwnerId == ownerId
                            && f.ParentFolderId != null
                            && level.Contains(f.ParentFolderId.Value))
                .ToListAsync(cancellationToken);

            result.AddRange(children);
            currentLevel = children.Select(f => f.Id).ToList();
        }

        return result;
    }

    public async Task<List<Folder>> GetTrashedRootsAsync(Guid ownerId, CancellationToken ct = default)
    {
        var deleted = context.Folders
            .IgnoreQueryFilters()
            .Where(f => f.OwnerId == ownerId && f.DeletedAt != null);

        return await deleted
            .Where(f => f.ParentFolderId == null
                        || !deleted.Any(p => p.Id == f.ParentFolderId && p.DeletedAt == f.DeletedAt))
            .OrderByDescending(f => f.DeletedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Folder folder, CancellationToken cancellationToken = default)
    {
        await context.Folders.AddAsync(folder, cancellationToken);
    }

    public void Update(Folder folder)
    {
        context.Folders.Update(folder);
    }

    public void Delete(Folder folder)
    {
        context.Folders.Remove(folder);
    }
}