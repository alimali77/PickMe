using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickMe.Api.Common;
using PickMe.Application.Abstractions;
using PickMe.Application.Reservations;
using PickMe.Domain;

namespace PickMe.Api.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize(Roles = nameof(UserRole.Customer))]
public sealed class ReservationsController(IReservationService reservations, IRatingService ratings, ICurrentUser currentUser) : ControllerBase
{
    private readonly IReservationService _reservations = reservations;
    private readonly IRatingService _ratings = ratings;
    private readonly ICurrentUser _currentUser = currentUser;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _reservations.CreateAsync(userId, dto, ct)).ToActionResult(StatusCodes.Status201Created);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ReservationStatus? status, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _reservations.ListOwnAsync(userId, status, ct)).ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _reservations.GetOwnDetailAsync(userId, id, ct)).ToActionResult();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _reservations.CancelByCustomerAsync(userId, id, ct)).ToActionResult();
    }

    [HttpPost("{id:guid}/rating")]
    public async Task<IActionResult> Rate(Guid id, [FromBody] RateReservationDto dto, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _ratings.RateAsync(userId, id, dto, ct)).ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPatch("{id:guid}/rating")]
    public async Task<IActionResult> EditRating(Guid id, [FromBody] RateReservationDto dto, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _ratings.EditAsync(userId, id, dto, ct)).ToActionResult();
    }
}
