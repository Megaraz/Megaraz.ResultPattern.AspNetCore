namespace Megaraz.ResultPattern.AspNetCore
{
    /// <summary>
    /// Represents normalised pagination parameters. Construct via <see cref="Normalize"/>.
    /// </summary>
    public sealed record PaginationParameters
    {
        /// <summary>Gets the 1-based page number.</summary>
        public int PageNumber { get; init; }

        /// <summary>Gets the number of items per page.</summary>
        public int PageSize { get; init; }

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
        /// <param name="maxPageSize">Upper bound for <paramref name="pageSize"/>. Defaults to 100.</param>
        public static PaginationParameters Normalize(int pageNumber, int pageSize, int maxPageSize = 100)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > maxPageSize) pageSize = maxPageSize;
            return new PaginationParameters(pageNumber, pageSize);
        }
    }
}
