using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using AppApi.Models;

namespace AppApi.Data;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        var entries = context.ChangeTracker.Entries<BaseEntity>().ToList();

        foreach (var entry in entries)
        {
            var originalState = entry.State;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = userName;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = userName;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = userName;
                    break;
            }

            if (originalState != EntityState.Detached
                && originalState != EntityState.Unchanged)
            {
                var auditLog = CreateAuditLog(entry, originalState, userId, userName);
                if (auditLog != null)
                {
                    context.Set<AuditLog>().Add(auditLog);
                }
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private AuditLog? CreateAuditLog(EntityEntry<BaseEntity> entry, EntityState originalState, string? userId, string? userName)
    {
        var action = originalState switch
        {
            EntityState.Added => "Create",
            EntityState.Modified => "Update",
            EntityState.Deleted => "Delete",
            _ => null
        };

        if (action == null) return null;

        var entityType = entry.Entity.GetType().Name;
        var entityId = entry.Entity.Id.ToString();

        string? oldValues = null;
        string? newValues = null;
        string? changedColumns = null;

        if (action == "Update")
        {
            var properties = entry.Properties
                .Where(p => p.IsModified && !IsAuditIgnored(p.Metadata))
                .ToList();
            changedColumns = string.Join(", ", properties.Select(p => p.Metadata.Name));

            var oldDict = properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue?.ToString());
            var newDict = properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString());

            oldValues = JsonSerializer.Serialize(oldDict);
            newValues = JsonSerializer.Serialize(newDict);
        }
        else if (action == "Create")
        {
            var dict = entry.Properties
                .Where(p => !IsAuditIgnored(p.Metadata))
                .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString());
            newValues = JsonSerializer.Serialize(dict);
        }

        return new AuditLog
        {
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            ChangedColumns = changedColumns
        };
    }

    private static bool IsAuditIgnored(Microsoft.EntityFrameworkCore.Metadata.IProperty property)
    {
        var clr = property.PropertyInfo;
        if (clr == null) return false;
        return clr.GetCustomAttributes(typeof(AuditIgnoreAttribute), inherit: true).Length > 0;
    }
}