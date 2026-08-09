namespace LearnStack.Hub.SharedKernel.Pagination;

/// <summary>
/// Cursor-pagination request. Cursor is the default for every list endpoint.
/// The <see cref="Cursor"/> is an opaque token the server minted on a previous
/// response; the client never parses it. <see cref="Limit"/> defaults to
/// <see cref="DefaultLimit"/> and is capped at <see cref="MaxLimit"/>.
/// </summary>
public sealed record CursorPagination
{
    public const int DefaultLimit = 20;

    public const int MaxLimit = 100;

    private readonly int _limit = DefaultLimit;

    public CursorPagination(string? Cursor = null, int Limit = DefaultLimit)
    {
        this.Cursor = Cursor;
        this.Limit = Limit;
    }

    public string? Cursor { get; init; }

    public int Limit
    {
        get => _limit;
        init
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    $"Limit must be > 0. Got: {value}.");
            }

            _limit = value > MaxLimit ? MaxLimit : value;
        }
    }
}
