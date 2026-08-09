using LearnStack.Hub.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace LearnStack.Hub.Api.Common;

/// <summary>
/// Maps a <see cref="Result{T}"/> to an <see cref="IActionResult"/>. The
/// sanctioned shape — explicit at every controller endpoint:
/// <c>(await _mediator.Send(cmd, ct)).ToActionResult()</c>. Success →
/// <see cref="OkObjectResult"/>; failure → <see cref="ProblemDetailsActionResult"/>.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        var error = result.Error
            ?? throw new InvalidOperationException(
                "Result.IsFailure but Error is null — Result<T>.Fail enforces a non-null Error.");

        return new ProblemDetailsActionResult(error);
    }
}
