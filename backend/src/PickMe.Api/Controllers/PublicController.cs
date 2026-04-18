using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickMe.Api.Common;
using PickMe.Application.Public;
using PickMe.Application.Reservations;

namespace PickMe.Api.Controllers;

[ApiController]
[Route("api")]
[AllowAnonymous]
public sealed class PublicController(IPublicService publicService) : ControllerBase
{
    private readonly IPublicService _publicService = publicService;

    [HttpGet("faqs")]
    public async Task<IActionResult> Faqs(CancellationToken ct)
        => (await _publicService.ListFaqsAsync(ct)).ToActionResult();

    [HttpPost("contact")]
    public async Task<IActionResult> Contact([FromBody] ContactFormDto dto, CancellationToken ct)
        => (await _publicService.SubmitContactAsync(dto, ct)).ToActionResult(StatusCodes.Status201Created);
}
