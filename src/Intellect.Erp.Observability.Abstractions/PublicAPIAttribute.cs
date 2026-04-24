namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Marker attribute indicating that a type or member is part of the public API surface.
/// Breaking changes to members marked with this attribute require a major version bump.
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
public sealed class PublicAPIAttribute : Attribute
{
}
