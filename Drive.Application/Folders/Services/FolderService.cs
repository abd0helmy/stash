using Drive.Core.Common;
using Drive.Application.Files.Interfaces;
using Drive.Application.Folders.DTOs;
using Drive.Application.Folders.Interfaces;
using Drive.Application.Interfaces;
using Drive.Core.Common.Result;
using Drive.Core.Entities;

namespace Drive.Application.Folders.Services;

public class FolderService(
    IFolderRepository folderRepository,
    IFileRepository fileRepository,
    IObjectStorage objectStorage,
    IUnitOfWork unitOfWork) : IFolderService
{
    public async Task<Result<FolderResponse>> GetByIdAsync(Guid id, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var folder = await folderRepository.GetByIdAsync(id, ownerId, cancellationToken);

        if (folder is null)
        {
            return Result<FolderResponse>.Failure(
                new Error(
                    "Folder.NotFound",
                    "The specified folder was not found.",
                    ErrorType.NotFound));
        }

        return Result<FolderResponse>.Success(
            new FolderResponse(
                folder.Id,
                folder.Name,
                folder.ParentFolderId,
                folder.OwnerId,
                folder.CreatedAt,
                folder.UpdatedAt));
    }

    public async Task<Result<IEnumerable<FolderResponse>>> GetChildrenAsync(Guid? parentFolderId, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (parentFolderId.HasValue)
        {
            var parent = await folderRepository.GetByIdAsync(parentFolderId.Value, ownerId, cancellationToken);

            if (parent is null)
            {
                return Result<IEnumerable<FolderResponse>>.Failure(
                    new Error(
                        "Folder.ParentNotFound",
                        "The specified parent folder was not found.",
                        ErrorType.NotFound));
            }
        }

        var folders = await folderRepository.GetChildrenAsync(parentFolderId, ownerId, cancellationToken);

        var response = folders.Select(folder => new FolderResponse(
            folder.Id,
            folder.Name,
            folder.ParentFolderId,
            folder.OwnerId,
            folder.CreatedAt,
            folder.UpdatedAt));

        return Result<IEnumerable<FolderResponse>>.Success(response);
    }

    public async Task<Result<FolderResponse>> CreateAsync(
        string name,
        Guid? parentFolderId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (parentFolderId.HasValue)
        {
            var parentFolder = await folderRepository.GetByIdAsync(
                parentFolderId.Value, ownerId, cancellationToken);

            if (parentFolder is null)
            {
                return Result<FolderResponse>.Failure(
                    new Error(
                        "Folder.ParentNotFound",
                        "The specified parent folder was not found.",
                        ErrorType.NotFound));
            }
        }

        var folder = new Folder
        {
            Name = name,
            OwnerId = ownerId,
            ParentFolderId = parentFolderId
        };

        await folderRepository.AddAsync(folder, cancellationToken);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result<FolderResponse>.Failure(Error.InternalServerError);
        }

        return Result<FolderResponse>.Success(new FolderResponse(
            folder.Id,
            folder.Name,
            folder.ParentFolderId,
            folder.OwnerId,
            folder.CreatedAt,
            folder.UpdatedAt));
    }

    public async Task<Result> RenameAsync(Guid id, string name, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var folder = await folderRepository.GetByIdAsync(id, ownerId, cancellationToken);

        if (folder is null)
        {
            return Result.Failure(
                new Error(
                    "Folder.NotFound",
                    "The specified folder was not found.",
                    ErrorType.NotFound));
        }

        if (folder.Name == name)
        {
            return Result.Success();
        }

        folder.Name = name;
        folderRepository.Update(folder);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result.Failure(Error.InternalServerError);
        }

        return Result.Success();
    }

    public async Task<Result> MoveAsync(Guid id, Guid? parentFolderId, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var folder = await folderRepository.GetByIdAsync(id, ownerId, cancellationToken);

        if (folder is null)
        {
            return Result.Failure(
                new Error(
                    "Folder.NotFound",
                    "The specified folder was not found.",
                    ErrorType.NotFound));
        }

        if (folder.ParentFolderId == parentFolderId)
        {
            return Result.Success();
        }

        if (parentFolderId.HasValue)
        {
            if (parentFolderId.Value == id)
            {
                return Result.Failure(
                    new Error(
                        "Folder.InvalidMove",
                        "A folder cannot be moved into itself.",
                        ErrorType.Validation));
            }

            var newParent = await folderRepository.GetByIdAsync(parentFolderId.Value, ownerId, cancellationToken);

            if (newParent is null)
            {
                return Result.Failure(
                    new Error(
                        "Folder.ParentNotFound",
                        "The specified parent folder was not found.",
                        ErrorType.NotFound));
            }

            if (await folderRepository.IsDescendantOfAsync(parentFolderId.Value, id, ownerId, cancellationToken))
            {
                return Result.Failure(
                    new Error(
                        "Folder.InvalidMove",
                        "A folder cannot be moved into one of its own subfolders.",
                        ErrorType.Validation));
            }
        }

        folder.ParentFolderId = parentFolderId;

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result.Failure(Error.InternalServerError);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var folder = await folderRepository.GetByIdAsync(id, ownerId, cancellationToken);

        if (folder is null)
        {
            return Result.Failure(
                new Error(
                    "Folder.NotFound",
                    "The specified folder was not found.",
                    ErrorType.NotFound));
        }

        var descendants = await folderRepository.GetDescendantsAsync(id, ownerId, cancellationToken);
        var allFolderIds = descendants.Select(d => d.Id).Append(folder.Id).ToList();

        var files = await fileRepository.GetByFolderIdsAsync(allFolderIds, ownerId, cancellationToken);

        var deletedAt = DateTime.UtcNow;

        folder.DeletedAt = deletedAt;

        foreach (var descendant in descendants)
        {
            descendant.DeletedAt = deletedAt;
        }

        foreach (var file in files)
        {
            file.DeletedAt = deletedAt;
        }

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result.Failure(Error.InternalServerError);
        }

        return Result.Success();
    }

    public async Task<Result<IEnumerable<FolderResponse>>> GetTrashAsync(Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var folders = await folderRepository.GetTrashedRootsAsync(ownerId, cancellationToken);

        var response = folders.Select(folder => new FolderResponse(
            folder.Id,
            folder.Name,
            folder.ParentFolderId,
            folder.OwnerId,
            folder.CreatedAt,
            folder.UpdatedAt));

        return Result<IEnumerable<FolderResponse>>.Success(response);
    }

    public async Task<Result> RestoreAsync(Guid id, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var folder = await folderRepository.GetTrashedRootByIdAsync(id, ownerId, cancellationToken);

        if (folder is null)
        {
            return Result.Failure(
                new Error(
                    "Folder.NotFoundInTrash",
                    "The specified folder was not found in the trash.",
                    ErrorType.NotFound));
        }

        var deletedAt = folder.DeletedAt!.Value;
        var descendants = await folderRepository.GetDeletedDescendantsAsync(
            id, ownerId, deletedAt, cancellationToken);

        var allFolderIds = descendants.Select(d => d.Id).Append(folder.Id).ToList();
        var files = await fileRepository.GetDeletedFilesInFoldersAsync(
            allFolderIds, ownerId, deletedAt, cancellationToken);

        if (folder.ParentFolderId.HasValue)
        {
            var parent = await folderRepository.GetByIdAsync(
                folder.ParentFolderId.Value, ownerId, cancellationToken);

            if (parent is null)
            {
                folder.ParentFolderId = null;
            }
        }

        folder.DeletedAt = null;

        foreach (var descendant in descendants)
        {
            descendant.DeletedAt = null;
        }

        foreach (var file in files)
        {
            file.DeletedAt = null;
        }

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result.Failure(Error.InternalServerError);
        }

        return Result.Success();
    }
    
    public async Task<Result> DeleteForeverAsync(Guid id, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var folder = await folderRepository.GetTrashedRootByIdAsync(id, ownerId, cancellationToken);

        if (folder is null)
        {
            return Result.Failure(
                new Error(
                    "Folder.NotFoundInTrash",
                    "The specified folder was not found in the trash.",
                    ErrorType.NotFound));
        }

        var subtree = await folderRepository.GetSubtreeIncludingDeletedAsync(id, ownerId, cancellationToken);
        var allFolders = subtree.Append(folder).ToList();
        var allFolderIds = allFolders.Select(f => f.Id).ToList();

        var files = await fileRepository.GetByFolderIdsIncludingDeletedAsync(allFolderIds, ownerId, cancellationToken);

        foreach (var file in files)
        {
            try
            {
                await objectStorage.DeleteAsync(file.ObjectKey, cancellationToken);
            }
            catch
            {
                // Continue to ensure database cleanup
            }
        }

        fileRepository.DeleteRange(files);
        folderRepository.DeleteRange(allFolders);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result.Failure(Error.InternalServerError);
        }

        return Result.Success();
    }
}
