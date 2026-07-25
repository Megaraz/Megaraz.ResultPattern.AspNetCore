namespace Megaraz.ResultPattern.AspNetCore;

/// <summary>
/// Represents normalised pagination parameters. Construct via <see cref="Normalize"/>.
/// </summary>
public sealed record PaginationParameters
{
    /// <summary>Gets the 1-based page number.</summary>
    public int PageNumber { get; }

    /// <summary>Gets the number of items per page.</summary>
    public int PageSize { get; }

    private PaginationParameters(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Returns a <see cref="PaginationParameters"/> with both values clamped to valid ranges.
    /// <list type="bullet">
    ///   <item><description><paramref name="pageNumber"/> is clamped to a minimum of 1.</description></item>
    ///   <item><description><paramref name="pageSize"/> is clamped between 1 and <paramref name="maxPageSize"/>.</description></item>
    /// </list>
    /// </summary>
    /// <param name="pageNumber">Requested page number (1-based).</param>
    /// <param name="pageSize">Requested items per page.</param>
    /// <param name="maxPageSize">Application-configured upper bound for <paramref name="pageSize"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxPageSize"/> is less than 1.
    /// </exception>
    public static PaginationParameters Normalize(int pageNumber, int pageSize, int maxPageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPageSize, 1);

        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, maxPageSize);

        return new PaginationParameters(pageNumber, pageSize);
    }
}
