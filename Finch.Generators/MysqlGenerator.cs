using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Finch.Generators;

[Generator]
public class MysqlGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var info = new DatabaseSpecificInfo
        {
            readerType = "global::System.Data.Common.DbDataReader",
            commandType = "global::MySql.Data.MySqlClient.MySqlCommand",
            connectionType = "global::MySql.Data.MySqlClient.MySqlConnection",
            parameterType = "global::MySql.Data.MySqlClient.MySqlParameter",
            prefix = "Mysql"
        };

        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax or RecordDeclarationSyntax,
                (ctx, _) => ClassDeclarationForSourceGenService.Get(
                    ctx,
                    "Finch.Abstractions.GenerateMysqlConnectionExtensionsAttribute"))
            .Where(t => t.reportAttributeFound)
            .Select((t, _) => t.Item1);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            (ctx, t) => GenerateCode(ctx, t.Left, t.Right, info));
    }

    private static void GenerateCode(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax> classOrRecordDeclarations,
        DatabaseSpecificInfo info)
    {
        if (classOrRecordDeclarations.Length == 0)
            return;

        ConnectionExtensionsGenerator.GenerateQuery(context, compilation, classOrRecordDeclarations, info);
        ConnectionExtensionsQueryAsyncGenerator.GenerateQueryAsync(context, compilation, classOrRecordDeclarations,
            info);

        ConnectionExtensionsQueryAsyncWithParameterGenerator.GenerateQueryAsyncWithParameter(
            context,
            compilation,
            classOrRecordDeclarations,
            info);

        ObjectMapperGenerator.Generate(context, compilation, classOrRecordDeclarations, info);
        PropertyMapperGenerator.Generate(context, compilation, classOrRecordDeclarations, info);
    }
}