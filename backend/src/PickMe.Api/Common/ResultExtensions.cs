using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PickMe.Application.Common;

namespace PickMe.Api.Common;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, int okStatus = StatusCodes.Status200OK)
    {
        if (result.Success)
        {
            return new ObjectResult(ApiResponse<T>.Ok(result.Data!)) { StatusCode = okStatus };
        }

        var statusCode = result.Code switch
        {
            "validation" => StatusCodes.Status400BadRequest,
            "auth.email_taken" => StatusCodes.Status409Conflict,
            "auth.invalid_credentials" or "auth.invalid_token" or "auth.invalid_refresh" => StatusCodes.Status401Unauthorized,
            "auth.email_not_verified" => StatusCodes.Status403Forbidden,
            "auth.locked" => StatusCodes.Status423Locked,
            "auth.inactive" or "auth.wrong_current_password" => StatusCodes.Status400BadRequest,
            "auth.not_found" or "auth.profile_missing" => StatusCodes.Status404NotFound,
            "reservation.invalid_transition" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        return new ObjectResult(ApiResponse<T>.Fail(result.Code ?? "error", result.Message ?? "İşlem başarısız.", result.Errors))
        {
            StatusCode = statusCode,
        };
    }
}
