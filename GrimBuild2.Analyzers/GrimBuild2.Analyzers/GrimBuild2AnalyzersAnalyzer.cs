using GrimBuilder2.Analyzers.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace GrimBuild2.Analyzers;

[Generator]
public class StatsSourceGenerator : IIncrementalGenerator
{
    enum GdStatsFieldType { Float }
    record struct GdStatsField(string Name, GdStatsFieldType Type);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var gdStatsClasses = context.SyntaxProvider.CreateSyntaxProvider(
            static (n, _) => n.IsKind(SyntaxKind.ClassDeclaration) && ((ClassDeclarationSyntax)n).Identifier.ToString() is "GdStats",
            static (ctx, _) =>
            {
                var cds = (ClassDeclarationSyntax)ctx.Node;
                if (ctx.SemanticModel.GetDeclaredSymbol(cds) is not { } cdsSymbol) return default;

                return (
                    @namespace: cdsSymbol.ContainingNamespace.IsGlobalNamespace ? null : cdsSymbol.ContainingNamespace.ToDisplayString(),
                    name: cds.Identifier.ToString(),
                    fields: cds.Members
                        .OfType<PropertyDeclarationSyntax>()
                        .Select(m => new GdStatsField(m.Identifier.ToString(), ctx.SemanticModel.GetTypeInfo(m.Type).Type?.ToFullyQualifiedString() switch
                        {
                            "global::System.Single" => GdStatsFieldType.Float,
                            _ => throw new NotImplementedException()
                        }))
                        .ToImmutableArray());
            }).Collect();

        context.RegisterSourceOutput(gdStatsClasses, static (ctx, gdStatsClasses) =>
        {
            if (gdStatsClasses.Length == 0) return;
            var (@namespace, name, fields) = gdStatsClasses.Single();
            if (name is null) return;

            ctx.AddSource("GdStats.g.cs", SourceText.From($$"""
                {{(@namespace is null ? null : $"namespace {@namespace};")}}

                partial class {{name}}
                {
                    public void AddFrom(in GdStats source)
                    {
                        {{string.Join("\n", fields.Select(field => $"{field.Name} += source.{field.Name};"))}}
                    }
                }
                """, Encoding.UTF8));
        });
    }
}