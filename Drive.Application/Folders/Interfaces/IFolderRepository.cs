using Drive.Core.Entities;

namespace Drive.Application.Folders.Interfaces;

public interface IFolderRepository
{
    Task<Folder?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Folder?> GetByIdAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Folder>> GetChildrenAsync(
        Guid? parentFolderId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    public Task<bool> IsDescendantOfAsync(Guid folderId, Guid ancestorId, Guid ownerId,
        CancellationToken cancellationToken);

    Task<List<Folder>> GetDescendantsAsync(Guid folderId, Guid ownerId, CancellationToken cancellationToken = default);

    public Task<List<Folder>> GetTrashedRootsAsync(Guid ownerId, CancellationToken ct = default);

    public Task<Folder?> GetTrashedRootByIdAsync(
        Guid id, Guid ownerId, CancellationToken cancellationToken = default);

    public Task<List<Folder>> GetDeletedDescendantsAsync(
        Guid folderId, Guid ownerId, DateTime deletedAt, CancellationToken cancellationToken = default);

    Task AddAsync(
        Folder folder,
        CancellationToken cancellationToken = default);

    void Update(Folder folder);
    void Delete(Folder folder);

    Task<List<Folder>> GetSubtreeIncludingDeletedAsync(Guid folderId, Guid ownerId,
        CancellationToken cancellationToken = default);

    void DeleteRange(IEnumerable<Folder> folders);
    Task<int> PurgeExpiredAsync(DateTime cutoff, CancellationToken cancellationToken = default);
}