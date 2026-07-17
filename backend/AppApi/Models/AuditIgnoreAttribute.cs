namespace AppApi.Models;

[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditIgnoreAttribute : Attribute
{
}