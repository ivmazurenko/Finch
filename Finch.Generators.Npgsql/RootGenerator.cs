using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Finch.Generators.Npgsql;

[Generator]
public class RootGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => GetClassDeclarationForSourceGen(ctx))
            .Where(t => t.reportAttributeFound)
            .Select((t, _) => t.Item1);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            (ctx, t) => GenerateCode(ctx, t.Left, t.Right));
    }

    private static (ClassDeclarationSyntax, bool reportAttributeFound) GetClassDeclarationForSourceGen(
        GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        foreach (var attributeSyntax in attributeListSyntax.Attributes)
        {
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                continue;

            var attributeName = attributeSymbol.ContainingType.ToDisplayString();

            if (attributeName == "Finch.Abstractions.Npgsql.GenerateNpgsqlConnectionExtensionsAttribute")
                return (classDeclarationSyntax, true);
        }

        return (classDeclarationSyntax, false);
    }

    private void GenerateCode(SourceProductionContext context, Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classDeclarations)
    {
        NpgsqlConnectionExtensionsGenerator.GenerateQuery(context, compilation, classDeclarations);
        NpgsqlConnectionExtensionsQueryAsyncGenerator.GenerateQueryAsync(context, compilation, classDeclarations);
        NpgsqlConnectionExtensionsQueryAsyncWithParameterGenerator.GenerateQueryAsyncWithParameter(context, compilation, classDeclarations);
        ReaderGenericGenerator.Generate(context, compilation, classDeclarations);
        ReaderGenerator.Generate(context, compilation, classDeclarations);
    }
}