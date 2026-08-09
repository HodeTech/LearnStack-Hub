namespace LearnStack.Hub.SharedKernel.Pagination;

/// <summary>
/// Cursor-pagination response envelope. Uniform shape across every list
/// endpoint so OpenAPI generation produces a consistent surface.
/// </summary>
public sealed record PageInfo(
    string? NextCursor,
    string? PreviousCursor,
    bool HasNext,
    bool HasPrevious);
