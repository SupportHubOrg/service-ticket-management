using System.Linq.Expressions;

namespace SupportHub.TicketManagement.Infrastructure.Data;

public static class QueryableExtensions
{
    /// <summary>
    /// Dynamically applies sorting to any IQueryable pipeline based on property name strings.
    /// </summary>
    public static IQueryable<TEntity> ApplySort<TEntity>(
        this IQueryable<TEntity> query, 
        string? sortBy, 
        bool sortDescending) where TEntity : class
    {
        //Return unsorted if no sort field provided
        if (string.IsNullOrWhiteSpace(sortBy))
            return query;

        //Build a dynamic lambda expression manually: x => EF.Property<object>(x, sortBy)
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        
        // This targets the specific property name on the entity dynamically
        var propertyAccess = Expression.PropertyOrField(parameter, sortBy);
        
        //Convert the property type to object so it matches any property data type seamlessly
        var conversion = Expression.Convert(propertyAccess, typeof(object));
        var lambda = Expression.Lambda<Func<TEntity, object>>(conversion, parameter);

        //Apply the appropriate order modifier and return
        return sortDescending 
            ? query.OrderByDescending(lambda) 
            : query.OrderBy(lambda);
    }
}