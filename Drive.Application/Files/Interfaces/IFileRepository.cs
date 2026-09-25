using File = Drive.Core.Entities.File;

namespace Drive.Application.Files.Interfaces;

public interface IFileRepository
{
    Task<File?> GetByIdAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<File>> GetByFolderIdAsync(
        Guid? folderId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<List<File>> GetByFolderIdsAsync(
        IEnumerable<Guid> folderIds,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<List<File>> GetByFolderIdsIncludingDeletedAsync(
        IEnumerable<Guid> folderIds,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<List<File>> GetDeletedFilesInFoldersAsync(
        IEnumerable<Guid> folderIds,
        Guid ownerId,
        DateTime deletedAt,
        CancellationToken cancellationToken = default);

    Task<List<File>> GetTrashedRootsAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<File?> GetTrashedByIdAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        File file,
        CancellationToken cancellationToken = default);

    void Update(File file);

    void Delete(File file);

    void DeleteRange(IEnumerable<File> files);

    Task<bool> ExistsAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}
