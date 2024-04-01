using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Finch.Generators.Shared;

public static class TypedMapperGenerator
{
    public static void Generate(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax> classOrRecordDeclarations,
        DatabaseSpecificInfo info)
    {
        foreach (var classDeclarationSyntax in classOrRecordDeclarations)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                continue;

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            var className = classDeclarationSyntax.Identifier.Text;

            var methodBody = classSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(ps =>
                    !(classDeclarationSyntax is RecordDeclarationSyntax && ps.ToDisplayString() == "EqualityContract"))
                .Select(p =>
                {
                    if (p.Type.ToDisplayString() == "int")
                        return $@"         item.{p.Name} = Convert.ToInt32(reader[""{p.Name}""]);";
                    if (p.Type.ToDisplayString() == "int?")
                        return $"""
                                        if (reader.IsDBNull(reader.GetOrdinal("{p.Name}")))
                                            item.{p.Name} = null;
                                        else
                                            item.{p.Name} = Convert.ToInt32(reader["{p.Name}"]);
                                """;

                    if (p.Type.ToDisplayString() == "short")
                        return $@"         item.{p.Name} = Convert.ToInt16(reader[""{p.Name}""]);";
                    if (p.Type.ToDisplayString() == "short?")
                        return $"""
                                        if (reader.IsDBNull(reader.GetOrdinal("{p.Name}")))
                                            item.{p.Name} = null;
                                        else
                                            item.{p.Name} = Convert.ToInt16(reader["{p.Name}"]);
                                """;

                    if (p.Type.ToDisplayString() == "long")
                        return $@"         item.{p.Name} = Convert.ToInt64(reader[""{p.Name}""]);";
                    if (p.Type.ToDisplayString() == "long?")
                        return $"""
                                        if (reader.IsDBNull(reader.GetOrdinal("{p.Name}")))
                                            item.{p.Name} = null;
                                        else
                                            item.{p.Name} = Convert.ToInt64(reader["{p.Name}"]);
                                """;

                    if (p.Type.ToDisplayString() == "decimal")
                        return $@"         item.{p.Name} = Convert.ToDecimal(reader[""{p.Name}""]);";
                    if (p.Type.ToDisplayString() == "decimal?")
                        return $"""
                                        if (reader.IsDBNull(reader.GetOrdinal("{p.Name}")))
                                            item.{p.Name} = null;
                                        else
                                            item.{p.Name} = Convert.ToDecimal(reader["{p.Name}"]);
                                """;

                    if (p.Type.ToDisplayString() == "string")
                        return $@"         item.{p.Name} = reader[""{p.Name}""].ToString();";

                    if (p.Type.ToDisplayString() == "bool")
                        return $@"         item.{p.Name} = Convert.ToBoolean(reader[""{p.Name}""]);";
                    if (p.Type.ToDisplayString() == "bool?")
                        return $"""
                                        if (reader.IsDBNull(reader.GetOrdinal("{p.Name}")))
                                            item.{p.Name} = null;
                                        else
                                            item.{p.Name} = Convert.ToBoolean(reader["{p.Name}"]);
                                """;
                    return $"// CAN NOT PROCESS: {p.Type.ToDisplayString()} -> {p.Name}";
                });

            var code =
                $$"""
                  // <auto-generated/>

                  using System;

                  namespace {{namespaceName}};

                  internal partial class {{info.prefix}}TypedMapper
                  {
                      public static void Map(
                          {{namespaceName}}.{{className}} item,
                          {{info.readerType}} reader)
                      {
                  {{string.Join("\n\n", methodBody)}}
                      }
                  }
                  """;

            context.AddSource($"{info.prefix}TypedMapper.{className}.g.cs", SourceText.From(code, Encoding.UTF8));
        }
    }
}