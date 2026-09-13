namespace Zomp.MethodCloning.Variants;

/// <summary>
/// Diagnostics the variant generator reports.
/// </summary>
internal static class VariantDiagnostics
{
    /// <summary>
    /// Two methods would declare the same member.
    /// </summary>
    internal static readonly DiagnosticDescriptor CollidingVariants = new(
        id: "ZMC001",
        title: "Variants collide",
        messageFormat: "Cannot write this variant. It produces '{0}', which another method also produces.",
        category: Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// The method's name does not contain the word its variants replace.
    /// </summary>
    internal static readonly DiagnosticDescriptor NameWithoutOriginal = new(
        id: "ZMC002",
        title: "Method name does not name its variant",
        messageFormat: "Cannot write variants of '{0}'. Its name has to contain the word they replace, or every variant would be named like the original.",
        category: Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// A type named after the original variant has no counterpart named after another.
    /// </summary>
    internal static readonly DiagnosticDescriptor NoCounterpart = new(
        id: "ZMC003",
        title: "Type has no counterpart in the variant",
        messageFormat: "{0}. The variant keeps the type as it is.",
        category: Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// The method is a member of a C# 14 extension block, which the generator cannot declare again.
    /// </summary>
    internal static readonly DiagnosticDescriptor ExtensionBlockMember = new(
        id: "ZMC004",
        title: "Extension block members are not supported",
        messageFormat: "Cannot write variants of '{0}'. Members of C# 14 extension blocks are not supported yet.",
        category: Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private const string Usage = "Usage";
}
