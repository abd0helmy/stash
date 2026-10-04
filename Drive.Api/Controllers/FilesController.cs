using Drive.Api.Contracts.Files;
using Drive.Application.Files.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Controllers;

[Authorize]
public class FilesController(
    IFileService fileService,
    IObjectStorage objectStorage) : BaseApiController
{
    [HttpPost("upload")]
    [RequestSizeLimit(10L * 1024 * 1024 * 1024)] // 10 GB limit max
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile? file,
        [FromForm] Guid? folderId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "A non-empty file must be provided."
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await fileService.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            folderId,
            CurrentUserId,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await fileService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetByFolder(
        [FromQuery] Guid? folderId,
        CancellationToken cancellationToken)
    {
        var result = await fileService.GetByFolderIdAsync(folderId, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("trash")]
    public async Task<IActionResult> GetTrash(CancellationToken cancellationToken)
    {
        var result = await fileService.GetTrashAsync(CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(
        Guid id,
        CancellationToken cancellationToken)
    {
        var fileResult = await fileService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        if (fileResult.IsFailure)
        {
            return HandleResult(fileResult);
        }

        var file = fileResult.Value!;
        var objectKey = $"users/{CurrentUserId}/files/{id}";
        var stream = await objectStorage.DownloadAsync(objectKey, cancellationToken);

        if (stream is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = "File content could not be retrieved from storage."
            });
        }

        return File(stream, file.MimeType, file.Name);
    }

    [HttpPatch("{id:guid}/rename")]
    public async Task<IActionResult> Rename(
        Guid id,
        [FromBody] RenameFileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await fileService.RenameAsync(id, request.Name, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/move")]
    public async Task<IActionResult> Move(
        Guid id,
        [FromBody] MoveFileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await fileService.MoveAsync(id, request.TargetFolderId, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await fileService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await fileService.RestoreAsync(id, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}/forever")]
    public async Task<IActionResult> DeleteForever(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await fileService.DeleteForeverAsync(id, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }
}
