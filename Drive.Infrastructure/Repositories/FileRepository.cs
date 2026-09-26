using Drive.Application.Files.Interfaces;
using Drive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using File = Drive.Core.Entities.File;

namespace Drive.Infrastructure.Repositories;

public class FileRepository(DriveDbContext context) : IFileRepository
{
    public async Task<File?> GetByIdAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await context.Files
            .FirstOrDefaultAsync(file => file.Id == id && file.OwnerId == ownerId, cancellationToken);
    }

    public async Task<File?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Files
            .FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<File>> GetByFolderIdAsync(Guid? folderId, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await context.Files
            .Where(file => file.FolderId == folderId && file.OwnerId == ownerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<File>> GetByFolderIdsAsync(IEnumerable<Guid> folderIds, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var ids = folderIds.ToList();
        return await context.Files
            .Where(file => file.OwnerId == ownerId && file.FolderId != null && ids.Contains(file.FolderId.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<File>> GetByFolderIdsIncludingDeletedAsync(IEnumerable<Guid> folderIds, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var ids = folderIds.ToList();
        return await context.Files
            .IgnoreQueryFilters()
            .Where(file => file.OwnerId == ownerId && file.FolderId != null && ids.Contains(file.FolderId.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<File>> GetDeletedFilesInFoldersAsync(IEnumerable<Guid> folderIds, Guid ownerId,
        DateTime deletedAt, CancellationToken cancellationToken = default)
    {
        var ids = folderIds.ToList();
        var minDeletedAt = deletedAt.AddSeconds(-2);
        var maxDeletedAt = deletedAt.AddSeconds(2);

        return await context.Files
            .IgnoreQueryFilters()
            .Where(file => file.OwnerId == ownerId
                           && file.FolderId != null
                           && ids.Contains(file.FolderId.Value)
                           && file.DeletedAt != null
                           && file.DeletedAt >= minDeletedAt
                           && file.DeletedAt <= maxDeletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<File>> GetTrashedRootsAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var deletedFiles = context.Files
            .IgnoreQueryFilters()
            .Where(f => f.OwnerId == ownerId && f.DeletedAt != null);

        var trashedFolders = context.Folders
            .IgnoreQueryFilters()
            .Where(f => f.OwnerId == ownerId && f.DeletedAt != null);

        return await deletedFiles
            .Where(f => f.FolderId == null || !trashedFolders.Any(folder => folder.Id == f.FolderId))
            .OrderByDescending(f => f.DeletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<File?> GetTrashedByIdAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await context.Files
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(file => file.Id == id && file.OwnerId == ownerId && file.DeletedAt != null,
                cancellationToken);
    }

    public async Task AddAsync(File file, CancellationToken cancellationToken = default)
    {
        await context.Files.AddAsync(file, cancellationToken);
    }

    public void Update(File file)
    {
        context.Files.Update(file);
    }

    public void Delete(File file)
    {
        context.Files.Remove(file);
    }

    public void DeleteRange(IEnumerable<File> files)
    {
        context.Files.RemoveRange(files);
    }

    public async Task<bool> ExistsAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await context.Files
            .AnyAsync(file => file.Id == id && file.OwnerId == ownerId, cancellationToken);
    }
}