using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickMe.Api.Common;
using PickMe.Application.Reservations;
using PickMe.Domain;

namespace PickMe.Api.Controllers;

[ApiController]
[Route("api/admin/reservations")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminReservationsController(IReservationService reservations) : ControllerBase
{
    private readonly IReservationService _reservations = reservations;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ReservationStatus? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? driverId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new ReservationListQuery(status, fromUtc, toUtc, customerId, driverId, search, page, pageSize);
        return (await _reservations.AdminListAsync(query, ct)).ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => (await _reservations.AdminGetDetailAsync(id, ct)).ToActionResult();

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignDriverDto dto, CancellationToken ct)
        => (await _reservations.AdminAssignAsync(id, dto, ct)).ToActionResult();

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelReservationDto dto, CancellationToken ct)
        => (await _reservations.AdminCancelAsync(id, dto, ct)).ToActionResult();

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] ReservationStatus? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct)
    {
        var query = new ReservationListQuery(status, fromUtc, toUtc, null, null, null, 1, 10_000);
        var result = await _reservations.AdminListAsync(query, ct);
        if (!result.Success || result.Data is null) return result.ToActionResult();

        var csv = new StringBuilder();
        csv.AppendLine("Id;Status;ServiceType;ReservationAtUtc;CustomerName;CustomerPhone;DriverName;DriverPhone;Address;Lat;Lng;CreatedAtUtc");
        foreach (var r in result.Data.Items)
        {
            csv.AppendLine(string.Join(';', new[]
            {
                r.Id.ToString(),
                r.Status.ToString(),
                r.ServiceType.ToString(),
                r.ReservationDateTimeUtc.ToString("u", CultureInfo.InvariantCulture),
                EscapeCsv(r.CustomerName),
                EscapeCsv(r.CustomerPhone),
                EscapeCsv(r.DriverName ?? string.Empty),
                EscapeCsv(r.DriverPhone ?? string.Empty),
                EscapeCsv(r.Address),
                r.Lat.ToString(CultureInfo.InvariantCulture),
                r.Lng.ToString(CultureInfo.InvariantCulture),
                r.CreatedAtUtc.ToString("u", CultureInfo.InvariantCulture),
            }));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"reservations-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    private static string EscapeCsv(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        if (input.Contains(';') || input.Contains('"') || input.Contains('\n'))
        {
            return "\"" + input.Replace("\"", "\"\"") + "\"";
        }
        return input;
    }
}

[ApiController]
[Route("api/admin/drivers")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminDriversController(IReservationService reservations) : ControllerBase
{
    private readonly IReservationService _reservations = reservations;

    [HttpGet("active")]
    public async Task<IActionResult> ListActive(CancellationToken ct)
        => (await _reservations.ListActiveDriversAsync(ct)).ToActionResult();
}
