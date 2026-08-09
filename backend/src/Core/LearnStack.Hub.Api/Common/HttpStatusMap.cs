using System.Net;
using LearnStack.Hub.SharedKernel.Errors;
using LearnStack.Hub.SharedKernel.Results;

namespace LearnStack.Hub.Api.Common;

/// <summary>
/// Maps an <see cref="Error.Code"/> (or a <see cref="HubException"/> subclass)
/// to the HTTP status the API surface returns (mirror of LearnStack core's
/// <c>HttpStatusMap</c>). Adding a new error code requires updating the table.
/// </summary>
public static class HttpStatusMap
{
    public static int For(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return For(error.Code);
    }

    public static int For(string code) => code switch
    {
        "validation_failed" => (int)HttpStatusCode.BadRequest,
        "unsupported_locale" => (int)HttpStatusCode.BadRequest,
        "unauthorized" => (int)HttpStatusCode.Unauthorized,
        "forbidden" => (int)HttpStatusCode.Forbidden,
        "resource_scope_violation" => (int)HttpStatusCode.Forbidden,
        "feature_disabled" => (int)HttpStatusCode.Forbidden,
        "not_found" => (int)HttpStatusCode.NotFound,
        "concurrency_conflict" => (int)HttpStatusCode.Conflict,
        "business_rule_violation" => (int)HttpStatusCode.Conflict,
        "rate_limited" => (int)HttpStatusCode.TooManyRequests,
        "dependency_unavailable" => (int)HttpStatusCode.ServiceUnavailable,
        _ => (int)HttpStatusCode.InternalServerError,
    };

    public static int For(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            // 499 "client closed request" — the client disconnected before the
            // response; the L1 handler skips both capture and body write for
            // OperationCanceled. Status set here for parity with upstream proxies.
            OperationCanceledException => 499,

            // Every HubException carries a structured Error; deriving the status
            // from Error.Code keeps body+status consistent.
            HubException known => For(known.Error),
            _ => (int)HttpStatusCode.InternalServerError,
        };
    }
}
