using Microsoft.EntityFrameworkCore;
using AppApi.DTOs;

namespace AppApi.Helpers;

public static class PaginationHelper
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedQuery pagedQuery)
    {
        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pagedQuery.Page - 1) * pagedQuery.PageSize)
            .Take(pagedQuery.PageSize)
            .ToListAsync();

        return PagedResult<T>.Create(items, totalCount, pagedQuery.Page, pagedQuery.PageSize);
    }
}