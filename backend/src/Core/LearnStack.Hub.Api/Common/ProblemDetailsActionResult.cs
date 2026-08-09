using LearnStack.Hub.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace LearnStack.Hub.Api.Common;

/// <summary>
/// <see cref="ObjectResult"/> subtype that defers <see cref="ProblemDetails"/>
/// body construction until ASP.NET invokes <see cref="ExecuteResultAsync"/> —
/// that is when the <see cref="Microsoft.AspNetCore.Http.HttpContext"/> is
/// available to populate <see cref="ProblemDetails.Instance"/> and the
/// <c>correlationId</c> extension.
/// </summary>
public sealed class ProblemDetailsActionResult : ObjectResult
{
    public ProblemDetailsActionResult(Error error)
        : base(value: null)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
        StatusCode = HttpStatusMap.For(error);
    }

    /// <summary>The carried failure — kept around for tests + extension points.</summary>
    public Error Error { get; }

    public override Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var problem = ProblemDetailsFactory.For(Error, context.HttpContext);
        Value = problem;
        StatusCode = problem.Status;
        ContentTypes.Clear();
        ContentTypes.Add("application/problem+json");
        return base.ExecuteResultAsync(context);
    }
}
