using Drive.Application.Sharing.DTOs;
using Drive.Application.Sharing.Interfaces;
using Drive.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Controllers;

[Authorize]
public class SharesController(IItemShareService itemShareService) : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> Share(
        [FromBody] ShareRequest request,
        CancellationToken cancellationToken)
    {
        var result = await itemShareService.ShareAsync(
            request.ItemId,
            request.ItemType,
            CurrentUserId,
            request.SharedWithUserId,
            request.Permission,
            request.ExpiresAt,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("shared-with-me")]
    public async Task<IActionResult> GetSharedWithMe(CancellationToken cancellationToken)
    {
        var result = await itemShareService.GetSharedWithUserAsync(CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/permission")]
    public async Task<IActionResult> UpdatePermission(
        Guid id,
        [FromBody] UpdateSharePermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await itemShareService.UpdatePermissionAsync(
            id,
            CurrentUserId,
            request.Permission,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await itemShareService.RevokeAsync(id, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("access-check")]
    public async Task<IActionResult> CheckAccess(
        [FromQuery] Guid itemId,
        [FromQuery] ShareableItemType itemType,
        CancellationToken cancellationToken)
    {
        var result = await itemShareService.HasAccessAsync(itemId, itemType, CurrentUserId, cancellationToken);
        return HandleResult(result);
    }
}
