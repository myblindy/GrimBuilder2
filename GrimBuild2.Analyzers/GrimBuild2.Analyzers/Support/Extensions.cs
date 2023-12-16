using Microsoft.CodeAnalysis;

namespace GrimBuilder2.Analyzers.Support;
static class Extensions
{
    static readonly SymbolDisplayFormat fullyQualifiedSymbolDisplayFormat = new(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public static string ToFullyQualifiedString(this ITypeSymbol type) => type.ToDisplayString(fullyQualifiedSymbolDisplayFormat);
}
