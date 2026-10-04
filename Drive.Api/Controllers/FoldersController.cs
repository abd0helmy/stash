using Drive.Api.Contracts.Folders;
using Drive.Application.Folders.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Controllers;

[Authorize]
public class FoldersController(IFolderService folderService) : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateFolderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await folderService.CreateAsync(
            request.Name,
            request.ParentFolderId,
            CurrentUserId,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await folderService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetChildren(
        [FromQuery] Guid? parentFolderId,
        CancellationToken cancellationToken)
    {
        var result = await folderService.GetChildrenAsync(parentFolderId, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("trash")]
    public async Task<IActionResult> GetTrash(CancellationToken cancellationToken)
    {
        var result = await folderService.GetTrashAsync(CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/rename")]
    public async Task<IActionResult> Rename(
        Guid id,
        [FromBody] RenameFolderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await folderService.RenameAsync(id, request.Name, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/move")]
    public async Task<IActionResult> Move(
        Guid id,
        [FromBody] MoveFolderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await folderService.MoveAsync(id, request.TargetFolderId, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await folderService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await folderService.RestoreAsync(id, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}/forever")]
    public async Task<IActionResult> DeleteForever(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await folderService.DeleteForeverAsync(id, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }
}
