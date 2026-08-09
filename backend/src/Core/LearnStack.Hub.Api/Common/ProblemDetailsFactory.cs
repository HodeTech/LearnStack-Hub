using System.Diagnostics;
using LearnStack.Hub.SharedKernel.Errors;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LearnStack.Hub.Api.Common;

/// <summary>
/// Builds RFC 7807 <see cref="ProblemDetails"/> bodies from Hub's
/// <see cref="Error"/> / <see cref="HubException"/> hierarchy (mirror of
/// LearnStack core's factory). Every API error response carries <c>code</c>,
/// <c>messageKey</c>, <c>correlationId</c>, optional <c>errors</c>. The
/// problem-type prefix is Hub's own error domain.
/// </summary>
public static class ProblemDetailsFactory
{
    private const string ProblemTypePrefix = "https://errors.hub.learnstack.dev/";

    public static ProblemDetails For(Error error, HttpContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        var problem = BuildBase(error.Code, error.Message.Key, HttpStatusMap.For(error.Code), context);

        if (error.Details is { Count: > 0 })
        {
            problem.Extensions["errors"] = ProjectDetails(error.Details);
        }

        return problem;
    }

    public static ProblemDetails For(Exception exception, HttpContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var status = HttpStatusMap.For(exception);

        if (exception is HubException known)
        {
            var problem = BuildBase(known.Error.Code, known.Error.Message.Key, status, context);
            if (known.Error.Details is { Count: > 0 })
            {
                problem.Extensions["errors"] = ProjectDetails(known.Error.Details);
            }

            return problem;
        }

        return BuildBase(
            code: "internal_error",
            messageKey: "lockey_internal_error",
            status: status,
            context: context);
    }

    private static ProblemDetails BuildBase(string code, string messageKey, int status, HttpContext? context)
    {
        var problem = new ProblemDetails
        {
            Type = ProblemTypePrefix + TrimFailedSuffix(code),
            Title = messageKey,
            Status = status,
            Instance = context?.Request.Path.Value,
        };

        problem.Extensions["code"] = code;
        problem.Extensions["messageKey"] = messageKey;
        problem.Extensions["correlationId"] = ResolveCorrelationId(context);
        return problem;
    }

    private static string TrimFailedSuffix(string code) =>
        code.EndsWith("_failed", StringComparison.Ordinal)
            ? code[..^"_failed".Length]
            : code;

    private static string? ResolveCorrelationId(HttpContext? context)
    {
        var traceParent = Activity.Current?.Id;
        if (!string.IsNullOrWhiteSpace(traceParent))
        {
            return traceParent;
        }

        return context?.TraceIdentifier;
    }

    private static Dictionary<string, IReadOnlyList<object>> ProjectDetails(
        IReadOnlyDictionary<string, IReadOnlyList<LocalizedMessage>> details)
    {
        var projected = new Dictionary<string, IReadOnlyList<object>>(StringComparer.Ordinal);
        foreach (var (key, list) in details)
        {
            projected[ToCamelCase(key)] = list
                .Select(m => (object)new
                {
                    key = m.Key,
                    @params = m.Params,
                })
                .ToArray();
        }

        return projected;
    }

    private static string ToCamelCase(string propertyPath)
    {
        if (string.IsNullOrEmpty(propertyPath))
        {
            return propertyPath;
        }

        if (!propertyPath.Contains('.', StringComparison.Ordinal))
        {
            return System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(propertyPath);
        }

        var segments = propertyPath.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(segments[i]);
        }

        return string.Join('.', segments);
    }
}
