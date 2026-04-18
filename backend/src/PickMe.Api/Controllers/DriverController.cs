using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickMe.Api.Common;
using PickMe.Application.Abstractions;
using PickMe.Application.Reservations;
using PickMe.Domain;

namespace PickMe.Api.Controllers;

[ApiController]
[Route("api/driver/tasks")]
[Authorize(Roles = nameof(UserRole.Driver))]
public sealed class DriverTasksController(IReservationService reservations, ICurrentUser currentUser) : ControllerBase
{
    private readonly IReservationService _reservations = reservations;
    private readonly ICurrentUser _currentUser = currentUser;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _reservations.DriverListOwnTasksAsync(userId, ct)).ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _reservations.DriverGetOwnTaskAsync(userId, id, ct)).ToActionResult();
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _reservations.DriverStartAsync(userId, id, ct)).ToActionResult();
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _reservations.DriverCompleteAsync(userId, id, ct)).ToActionResult();
    }
}
