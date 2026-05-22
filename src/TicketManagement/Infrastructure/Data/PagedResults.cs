using System.Linq.Expressions;
using System.Reflection;
using FluentValidation;

namespace SupportHub.TicketManagement.Infrastructure.Data
{
    /// <summary>
    /// Standardized wrapper for paginated responses.
    /// </summary>
    public record PagedResult<T>(
        List<T> Items,
        int TotalCount,
        int PageNumber,
        int PageSize
    );

    /// <summary>
    /// Base validator for paginated queries, enforcing common rules for pagination and sorting.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query being validated.</typeparam>
    /// <typeparam name="TEntity">The entity type being queried, used for validating sort fields.</typeparam>
    public class PagedQueryValidatorBase<TQuery, TEntity> : AbstractValidator<TQuery>
    {
        //Cache of allowed sort fields for efficient validation
        private static readonly HashSet<string> AllowedSortFields = [.. typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name.ToLowerUtc())];

        public PagedQueryValidatorBase(
            Expression<Func<TQuery, int>> pageNumberExpression,
            Expression<Func<TQuery, int>> pageSizeExpression,
            Expression<Func<TQuery, string?>> sortByExpression)
        {
            RuleFor(pageNumberExpression)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

            RuleFor(pageSizeExpression)
                .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

            RuleFor(sortByExpression)
                .Must(sortBy => sortBy == null || AllowedSortFields.Contains(sortBy.ToLowerUtc()))
                .WithMessage(x => $"Invalid sorting field. Allowed fields are: {string.Join(", ", GetFriendlyPropertyNames())}.");
        }

        //Helper method to extract property names for validation error messages
        private static IEnumerable<string> GetFriendlyPropertyNames()
        {
            return typeof(TEntity)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name);
        }
    }

    internal static class StringExtensions
    {
        public static string ToLowerUtc(this string input) => input.ToLowerInvariant();
    }
}