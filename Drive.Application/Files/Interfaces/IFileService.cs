using Drive.Application.Files.DTOs;
using Drive.Core.Common.Result;

namespace Drive.Application.Files.Interfaces;

public interface IFileService
{
    Task<Result<FileResponse>> GetByIdAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);
    
    Task<Result<FileResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<FileResponse>>> GetByFolderIdAsync(
        Guid? folderId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<FileResponse>>> GetTrashAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result<FileResponse>> UploadAsync(
        Stream content,
        string fileName,
        string mimeType,
        long size,
        Guid? folderId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result> RenameAsync(
        Guid id,
        string name,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result> MoveAsync(
        Guid id,
        Guid? folderId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result> RestoreAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteForeverAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}
