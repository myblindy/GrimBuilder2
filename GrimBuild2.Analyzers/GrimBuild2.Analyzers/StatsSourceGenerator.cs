using GrimBuilder2.Analyzers.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System;
using System.CodeDom.Compiler;
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
    record struct GdStatsField(string Name, GdStatsFieldType Type, string DbrFieldName, string DisplayFormatString);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context => context.AddSource("GdStats.Attribute.g.cs", SourceText.From($$"""
            public class GdStatAttribute(string dbrFieldName, string displayFormatString) : Attribute
            {
                public string DbrFieldName => dbrFieldName;
                public string DisplayFormatString => displayFormatString;
            }
            """, Encoding.UTF8)));

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
                        .Select(m =>
                        {
                            if (m.AttributeLists.SelectMany(x => x.Attributes).FirstOrDefault() is { } statsAttributeSyntax
                                && ctx.SemanticModel.GetOperation(statsAttributeSyntax) is { } statsAttributeOperation
                                && statsAttributeOperation.ChildOperations.FirstOrDefault() is IObjectCreationOperation { } statsAttributeCreationOperation
                                && statsAttributeCreationOperation.Arguments.Length >= 2

                                && statsAttributeCreationOperation.Arguments[0] is IArgumentOperation { } dbrFieldNameOperation
                                && dbrFieldNameOperation.ChildOperations.FirstOrDefault() is ILiteralOperation { } dbrFieldNameLiteralOperation
                                && dbrFieldNameLiteralOperation.ConstantValue.HasValue
                                && dbrFieldNameLiteralOperation.ConstantValue.Value is string { } dbrFieldName

                                && statsAttributeCreationOperation.Arguments[1] is IArgumentOperation { } displayFormatStringOperation
                                && displayFormatStringOperation.ChildOperations.FirstOrDefault() is ILiteralOperation { } displayFormatStringLiteralOperation
                                && displayFormatStringLiteralOperation.ConstantValue.HasValue
                                && displayFormatStringLiteralOperation.ConstantValue.Value is string { } displayFormatString)
                            {
                                return new GdStatsField(m.Identifier.ToString(), ctx.SemanticModel.GetTypeInfo(m.Type).Type?.ToFullyQualifiedString() switch
                                {
                                    "global::System.Single" => GdStatsFieldType.Float,
                                    _ => throw new NotImplementedException()
                                }, dbrFieldName, displayFormatString);
                            }
                            else
                                return default;
                        })
                        .Where(x => x.Name is not null)
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

                    public void Read(DbrData dbr)
                    {
                        {{string.Join("\n", fields.Select(field => $"{field.Name} = dbr.GetFloatValueOrDefault(\"{field.DbrFieldName}\");"))}}
                    }

                    public GdStatModel[] GetStatModels() => new[]
                    {
                        {{string.Join(",\n", fields.Select(field => $"new GdStatModel(\"{field.Name}\", {field.Name}, \"{field.DisplayFormatString}\")"))}}
                    };
                }

                public record GdStatModel(string Name, float Value, string DisplayFormatString);
                """, Encoding.UTF8));
        });
    }
}