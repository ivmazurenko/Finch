using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Finch.Generators.Sqlite;

public static class ReaderGenerator
{
    public static void Generate(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classDeclarations,
        string dataReaderType)
    {
        foreach (var classDeclarationSyntax in classDeclarations)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                continue;

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            var className = classDeclarationSyntax.Identifier.Text;

            var methodBody = classSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Select(p =>
                {
                    if (p.Type.ToDisplayString() == "int")
                        return $@"         item.{p.Name} = Convert.ToInt32(reader[""{p.Name}""]);";
                    else if (p.Type.ToDisplayString() == "string")
                        return $@"         item.{p.Name} = reader[""{p.Name}""].ToString();";
                    else if (p.Type.ToDisplayString() == "bool")
                        return $@"         item.{p.Name} = Convert.ToBoolean(reader[""{p.Name}""]);";
                    return "// NOT IMPLEMENTED";
                });


            var code =
                $$"""
                  // <auto-generated/>

                  using System;

                  namespace {{namespaceName}};

                  internal partial class TypedMapper
                  {
                      public static void Map(
                          {{namespaceName}}.{{className}} item,
                          {{dataReaderType}} reader)
                      {
                  {{string.Join("\n", methodBody)}}
                      }
                  }
                  """;

            context.AddSource($"TypedMapper.{className}.g.cs", SourceText.From(code, Encoding.UTF8));
        }
    }
}