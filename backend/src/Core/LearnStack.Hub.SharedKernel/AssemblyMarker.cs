namespace LearnStack.Hub.SharedKernel;

/// <summary>
/// Anchor type for `Assembly.GetExecutingAssembly()` / `typeof(AssemblyMarker).Assembly`
/// lookups. NetArchTest scans IL TypeRefs; an unused project reference produces no
/// IL TypeRef. Tests and the host's composition root pin this type explicitly so the
/// assembly is loaded.
/// </summary>
public sealed class AssemblyMarker;
