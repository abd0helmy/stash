using Drive.Application.Folders.DTOs;
using Drive.Core.Common.Result;

namespace Drive.Application.Folders.Interfaces;

public interface IFolderService
{
    Task<Result<FolderResponse>> GetByIdAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<FolderResponse>>> GetChildrenAsync(
        Guid? parentFolderId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result<FolderResponse>> CreateAsync(
        string name,
        Guid? parentFolderId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result> RenameAsync(
        Guid id,
        string name,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result> MoveAsync(
        Guid id,
        Guid? parentFolderId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<FolderResponse>>>
        GetTrashAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<Result> RestoreAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default);
    
    Task<Result> DeleteForeverAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default);
}