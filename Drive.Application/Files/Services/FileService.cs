using Drive.Core.Common;
using Drive.Application.Files.DTOs;
using Drive.Application.Files.Interfaces;
using Drive.Application.Folders.Interfaces;
using Drive.Application.Interfaces;
using Drive.Core.Common.Result;
using File = Drive.Core.Entities.File;

namespace Drive.Application.Files.Services;

public class FileService(
    IFileRepository fileRepository,
    IUnitOfWork unitOfWork,
    IFolderRepository folderRepository,
    IObjectStorage objectStorage) : IFileService
{
    public async Task<Result<FileResponse>> GetByIdAsync(Guid id, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var file = await fileRepository.GetByIdAsync(id, ownerId, cancellationToken);

        if (file is null)
        {
            return Result<FileResponse>.Failure(
                new Error(
                    "File.NotFound",
                    "The specified file was not found.",
                    ErrorType.NotFound));
        }

        return Result<FileResponse>.Success(
            new FileResponse(
                file.Id,
                file.Name,
                file.MimeType,
                file.Size,
                file.FolderId,
                file.CreatedAt,
                file.UpdatedAt));
    }

    public async Task<Result<IEnumerable<FileResponse>>> GetByFolderIdAsync(Guid? folderId, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (folderId.HasValue)
        {
            var folder = await folderRepository.GetByIdAsync(folderId.Value, ownerId, cancellationToken);

            if (folder is null)
            {
                return Result<IEnumerable<FileResponse>>.Failure(
                    new Error(
                        "Folder.FolderNotFound",
                        "The specified folder was not found.",
                        ErrorType.NotFound));
            }
        }

        var files = await fileRepository.GetByFolderIdAsync(folderId, ownerId, cancellationToken);

        var response = files
            .Select(file =>
                new FileResponse(
                    file.Id,
                    file.Name,
                    file.MimeType,
                    file.Size,
                    file.FolderId,
                    file.CreatedAt,
                    file.UpdatedAt
                ));

        return Result<IEnumerable<FileResponse>>.Success(response);
    }

    public async Task<Result<IEnumerable<FileResponse>>> GetTrashAsync(Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var files = await fileRepository.GetTrashedRootsAsync(ownerId, cancellationToken);

        var response = files
            .Select(file =>
                new FileResponse(
                    file.Id,
                    file.Name,
                    file.MimeType,
                    file.Size,
                    file.FolderId,
                    file.CreatedAt,
                    file.UpdatedAt
                ));

        return Result<IEnumerable<FileResponse>>.Success(response);
    }

    public async Task<Result<FileResponse>> UploadAsync(
        Stream content,
        string fileName,
        string mimeType,
        long size,
        Guid? folderId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (folderId.HasValue)
        {
            var folder = await folderRepository.GetByIdAsync(
                folderId.Value,
                ownerId,
                cancellationToken);

            if (folder is null)
            {
                return Result<FileResponse>.Failure(
                    new Error(
                        "Folder.FolderNotFound",
                        "The specified folder was not found.",
                        ErrorType.NotFound));
            }
        }

        var fileId = Guid.NewGuid();

        var objectKey = $"users/{ownerId}/files/{fileId}";

        try
        {
            await objectStorage.UploadAsync(
                content,
                objectKey,
                mimeType,
                cancellationToken);
        }
        catch
        {
            return Result<FileResponse>.Failure(
                Error.InternalServerError);
        }

        var file = new File
        {
            Id = fileId,
            Name = fileName,
            ObjectKey = objectKey,
            MimeType = mimeType,
            Size = size,
            OwnerId = ownerId,
            FolderId = folderId,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await fileRepository.AddAsync(
                file,
                cancellationToken);

            var rowsAffected = await unitOfWork.SaveChangesAsync(
                cancellationToken);

            if (rowsAffected == 0)
            {
                await objectStorage.DeleteAsync(
                    objectKey,
                    CancellationToken.None);

                return Result<FileResponse>.Failure(
                    Error.InternalServerError);
            }
        }
        catch
        {
            try
            {
                await objectStorage.DeleteAsync(
                    objectKey,
                    CancellationToken.None);
            }
            catch
            {
                // Keep the original database exception.
            }

            return Result<FileResponse>.Failure(
                Error.InternalServerError);
        }

        return Result<FileResponse>.Success(
            new FileResponse(
                file.Id,
                file.Name,
                file.MimeType,
                file.Size,
                file.FolderId,
                file.CreatedAt,
                file.UpdatedAt));
    }

    public async Task<Result> RenameAsync(Guid id, string name, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                new Error(
                    "File.InvalidName",
                    "File name cannot be empty.",
                    ErrorType.Validation));
        }

        var file = await fileRepository.GetByIdAsync(id, ownerId, cancellationToken);

        if (file is null)
        {
            return Result.Failure(
                new Error(
                    "File.NotFound",
                    "The specified file was not found.",
                    ErrorType.NotFound));
        }

        file.Name = name;

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result.Failure(Error.InternalServerError);
        }

        return Result.Success();
    }

    public async Task<Result> MoveAsync(Guid id, Guid? folderId, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var file = await fileRepository.GetByIdAsync(id, ownerId, cancellationToken);

        if (file is null)
        {
            return Result.Failure(
                new Error(
                    "File.NotFound",
                    "The specified file was not found.",
                    ErrorType.NotFound));
        }

        if (file.FolderId == folderId)
        {
            return Result.Success();
        }

        if (folderId.HasValue)
        {
            var folder = await folderRepository.GetByIdAsync(folderId.Value, ownerId, cancellationToken);

            if (folder is null)
            {
                return Result.Failure(
                    new Error(
                        "Folder.DestinationFolderNotFound",
                        "The destination folder was not found.",
                        ErrorType.NotFound));
            }
        }

        file.FolderId = folderId;

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result.Failure(Error.InternalServerError);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        var file = await fileRepository.GetByIdAsync(id, ownerId, cancellationToken);

        if (file is null)
        {
            return Result.Failure(
                new Error(
                    "File.NotFound",
                    "The specified file was not found.",
                    ErrorType.NotFound));
        }

        file.DeletedAt = DateTime.UtcNow;

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result.Failure(Error.InternalServerError);
        }

        return Result.Success();
    }

    public async Task<Result> RestoreAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        var file = await fileRepository.GetTrashedByIdAsync(id, ownerId, cancellationToken);

        if (file is null)
        {
            return Result.Failure(
                new Error(
                    "File.NotFoundInTrash",
                    "The specified file was not found in the trash.",
                    ErrorType.NotFound));
        }

        if (file.FolderId.HasValue)
        {
            var folder = await folderRepository.GetByIdAsync(file.FolderId.Value, ownerId, cancellationToken);
            if (folder is null)
            {
                file.FolderId = null;
            }
        }

        file.DeletedAt = null;

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result.Failure(Error.InternalServerError);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteForeverAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        var file = await fileRepository.GetTrashedByIdAsync(id, ownerId, cancellationToken);

        if (file is null)
        {
            return Result.Failure(
                new Error(
                    "File.NotFoundInTrash",
                    "The specified file was not found in the trash.",
                    ErrorType.NotFound));
        }

        try
        {
            await objectStorage.DeleteAsync(file.ObjectKey, cancellationToken);
        }
        catch
        {
            // Continue with database deletion
        }

        fileRepository.Delete(file);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result.Failure(Error.InternalServerError);
        }

        return Result.Success();
    }
}
