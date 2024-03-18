using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Finch.Generators.Shared;

public static class ConnectionExtensionsGenerator
{
    public static void GenerateQuery(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classDeclarations,
        DatabaseSpecificInfo info)
    {
        foreach (var classDeclarationSyntax in classDeclarations)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                continue;

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var code = $$"""
                         // <auto-generated/>

                         namespace {{namespaceName}};

                         public static partial class {{info.prefix}}ConnectionExtensions
                         {
                             public static global::System.Collections.Generic.List<T> Query<T>(
                                 this {{info.connectionType}} connection,
                                 string sql)
                                 where T : new()
                             {
                                 connection.Open();
                         
                                 var items = new global::System.Collections.Generic.List<T>();
                                 using var command = new {{info.commandType}}(sql, connection);
                                 using var reader = command.ExecuteReader();
                                 while (reader.Read())
                                 {
                                     T item = new T();
                         
                                     {{info.prefix}}GenericMapper.Map(item, reader);
                                     items.Add(item);
                                 }
                                 
                                 return items;
                             }
                         }
                         """;
            context.AddSource($"{info.prefix}ConnectionExtensions.Query.g.cs", SourceText.From(code, Encoding.UTF8));
            break;
        }
    }
}