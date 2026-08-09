using System.Diagnostics.CodeAnalysis;

namespace LearnStack.Hub.SharedKernel.Pagination;

/// <summary>
/// Cursor-paginated response. <see cref="Items"/> are the page payload;
/// <see cref="PageInfo"/> carries the next/previous opaque cursors plus the
/// boolean hints the client uses to render pagination controls.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Canonical empty-instance pattern (mirrors Array.Empty<T>).")]
public sealed record Page<T>(IReadOnlyList<T> Items, PageInfo PageInfo)
{
    public static Page<T> Empty { get; } = new(
        Array.Empty<T>(),
        new PageInfo(null, null, HasNext: false, HasPrevious: false));
}
