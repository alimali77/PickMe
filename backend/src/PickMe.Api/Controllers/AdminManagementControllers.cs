using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickMe.Api.Common;
using PickMe.Application.Abstractions;
using PickMe.Application.AdminManagement;
using PickMe.Domain;

namespace PickMe.Api.Controllers;

[ApiController]
[Route("api/admin/drivers")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminDriversManagementController(IDriverManagementService drivers) : ControllerBase
{
    private readonly IDriverManagementService _drivers = drivers;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => (await _drivers.ListAsync(search, page, pageSize, ct)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => (await _drivers.GetAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDriverDto dto, CancellationToken ct)
        => (await _drivers.CreateAsync(dto, ct)).ToActionResult(StatusCodes.Status201Created);

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDriverDto dto, CancellationToken ct)
        => (await _drivers.UpdateAsync(id, dto, ct)).ToActionResult();

    public sealed record SetActiveRequest(bool Active);

    [HttpPost("{id:guid}/set-active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveRequest req, CancellationToken ct)
        => (await _drivers.SetActiveAsync(id, req.Active, ct)).ToActionResult();

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct)
        => (await _drivers.ResetPasswordAsync(id, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _drivers.SoftDeleteAsync(id, ct)).ToActionResult();
}

[ApiController]
[Route("api/admin/recipients")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminRecipientsController(IRecipientsService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => (await service.ListAsync(ct)).ToActionResult();
    [HttpPost] public async Task<IActionResult> Add([FromBody] CreateRecipientDto dto, CancellationToken ct) => (await service.AddAsync(dto, ct)).ToActionResult(StatusCodes.Status201Created);

    public sealed record SetActiveRequest(bool Active);
    [HttpPost("{id:guid}/set-active")] public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveRequest req, CancellationToken ct) => (await service.SetActiveAsync(id, req.Active, ct)).ToActionResult();
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => (await service.DeleteAsync(id, ct)).ToActionResult();
}

[ApiController]
[Route("api/admin/faqs")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminFaqController(IFaqManagementService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => (await service.ListAsync(ct)).ToActionResult();
    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateFaqDto dto, CancellationToken ct) => (await service.CreateAsync(dto, ct)).ToActionResult(StatusCodes.Status201Created);
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFaqDto dto, CancellationToken ct) => (await service.UpdateAsync(id, dto, ct)).ToActionResult();
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => (await service.DeleteAsync(id, ct)).ToActionResult();
}

[ApiController]
[Route("api/admin/contact-messages")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminContactMessagesController(IContactMessagesService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] bool? unreadOnly, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => (await service.ListAsync(unreadOnly, page, pageSize, ct)).ToActionResult();
    [HttpPost("{id:guid}/mark-read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct) => (await service.MarkReadAsync(id, ct)).ToActionResult();
}

[ApiController]
[Route("api/admin/customers")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminCustomersController(ICustomerAdminService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => (await service.ListAsync(search, page, pageSize, ct)).ToActionResult();
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => (await service.GetAsync(id, ct)).ToActionResult();

    public sealed record SetActiveRequest(bool Active);
    [HttpPost("{id:guid}/set-active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveRequest req, CancellationToken ct)
        => (await service.SetActiveAsync(id, req.Active, ct)).ToActionResult();
}

[ApiController]
[Route("api/admin/ratings")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminRatingsController(IRatingAdminService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? driverId,
        [FromQuery] int? minScore,
        [FromQuery] int? maxScore,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await service.ListAsync(driverId, minScore, maxScore, page, pageSize, ct)).ToActionResult();

    [HttpPost("{id:guid}/flag")]
    public async Task<IActionResult> Flag(Guid id, [FromBody] FlagRatingDto dto, CancellationToken ct)
        => (await service.FlagAsync(id, dto, ct)).ToActionResult();
    [HttpPost("{id:guid}/unflag")]
    public async Task<IActionResult> Unflag(Guid id, CancellationToken ct)
        => (await service.UnflagAsync(id, ct)).ToActionResult();
}

[ApiController]
[Route("api/admin/admins")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminUsersController(IAdminUsersService service, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => (await service.ListAsync(ct)).ToActionResult();
    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateAdminDto dto, CancellationToken ct) => (await service.CreateAsync(dto, ct)).ToActionResult(StatusCodes.Status201Created);
    [HttpPatch("{id:guid}")] public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdminDto dto, CancellationToken ct) => (await service.UpdateAsync(id, dto, ct)).ToActionResult();
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await service.DeleteAsync(id, currentUser.UserId ?? Guid.Empty, ct)).ToActionResult();
}

[ApiController]
[Route("api/admin/settings")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminSettingsController(ISystemSettingsService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => (await service.ListAsync(ct)).ToActionResult();
    [HttpPut] public async Task<IActionResult> Update([FromBody] UpdateSystemSettingsDto dto, CancellationToken ct) => (await service.UpdateAsync(dto, ct)).ToActionResult();
}

/// <summary>
/// Anonymous: kamu erişimli site ayarı değerleri (örn: WhatsApp numarası, iletişim bilgileri).
/// </summary>
[ApiController]
[Route("api/settings")]
[AllowAnonymous]
public sealed class PublicSettingsController(ISystemSettingsService service) : ControllerBase
{
    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, CancellationToken ct) => (await service.GetPublicAsync(key, ct)).ToActionResult();
}
