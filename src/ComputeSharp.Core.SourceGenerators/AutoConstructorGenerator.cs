﻿using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ComputeSharp.Core.SourceGenerators.Models;
using ComputeSharp.SourceGeneration.Extensions;
using ComputeSharp.SourceGeneration.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ComputeSharp.Core.SourceGenerators;

/// <summary>
/// A source generator creating constructors for types annotated with <see cref="AutoConstructorAttribute"/>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class AutoConstructorGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get all declared struct symbols with the [AutoConstructor] attribute
        IncrementalValuesProvider<INamedTypeSymbol> structDeclarations =
            context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, token) => node is StructDeclarationSyntax structDeclaration,
                static (context, token) => (INamedTypeSymbol?)context.SemanticModel.GetDeclaredSymbol(context.Node, token))
            .Where(static symbol => symbol is not null &&
                                    symbol.GetAttributes().Any(static a => a.AttributeClass?.ToDisplayString() == typeof(AutoConstructorAttribute).FullName))!;

        // Get the type hierarchy and fields info
        IncrementalValuesProvider<(HierarchyInfo Left, ConstructorInfo Right)> constructorInfo =
            structDeclarations
            .Select(static (item, token) => (Hierarchy: HierarchyInfo.From(item), Info: Ctor.GetData(item)))
            .Where(static item => !item.Info.Parameters.IsEmpty)
            .WithComparers(HierarchyInfo.Comparer.Default, ConstructorInfo.Comparer.Default);

        // Generate the constructors
        context.RegisterSourceOutput(constructorInfo, static (context, item) =>
        {
            CompilationUnitSyntax compilationUnit = Ctor.GetSyntax(item.Left, item.Right);

            context.AddSource($"{item.Left.FilenameHint}.Ctor", compilationUnit.GetText(Encoding.UTF8));
        });
    }

    /// <summary>
    /// A helper with all logic to generate the constructor declarations.
    /// </summary>
    private static class Ctor
    {
        /// <summary>
        /// Gets the sequence of <see cref="ParameterInfo"/> items for all fields in the input type.
        /// </summary>
        /// <param name="structDeclarationSymbol">The <see cref="INamedTypeSymbol"/> being inspected.</param>
        /// <returns>The <see cref="ConstructorInfo"/> instance for <paramref name="structDeclarationSymbol"/>.</returns>
        public static ConstructorInfo GetData(INamedTypeSymbol structDeclarationSymbol)
        {
            ImmutableArray<ParameterInfo>.Builder parameters = ImmutableArray.CreateBuilder<ParameterInfo>();
            ImmutableArray<string>.Builder defaulted = ImmutableArray.CreateBuilder<string>();

            foreach (IFieldSymbol fieldSymbol in structDeclarationSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                // Skip fields that are not instance ones (and also ignore generated fields and fixed size buffers)
                if (fieldSymbol is not { IsConst: false, IsStatic: false, IsFixedSizeBuffer: false, IsImplicitlyDeclared: false })
                {
                    continue;
                }

                // Check whether the field is annotated to have a special behavior
                if (fieldSymbol.Type.TryGetAttributeWithFullMetadataName("ComputeSharp.AutoConstructorBehaviorAttribute", out AttributeData? usageData) &&
                    usageData!.ConstructorArguments is { Length: 1 } arguments &&
                    arguments[0].Value is (int)AutoConstructorBehavior.IgnoreAndSetToDefault)
                {
                    defaulted.Add(fieldSymbol.Name);
                }
                else
                {
                    // Track the field normally
                    string typeName = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    parameters.Add(new ParameterInfo(typeName, fieldSymbol.Name));
                }
            }

            return new(parameters.ToImmutableArray(), defaulted.ToImmutableArray());
        }

        /// <summary>
        /// Gets the <see cref="CompilationUnitSyntax"/> instance for a given type and sequence of parameters.
        /// </summary>
        /// <param name="hierarchyInfo">The type hierarchy info.</param>
        /// <param name="constructorInfo">The <see cref="ConstructorInfo"/> instance with the info for the constructor to generate.</param>
        /// <returns>A <see cref="CompilationUnitSyntax"/> instance for a specified type.</returns>
        public static CompilationUnitSyntax GetSyntax(HierarchyInfo hierarchyInfo, ConstructorInfo constructorInfo)
        {
            // Create the constructor declaration for the type. This will
            // produce a constructor with simple initialization of all variables:
            //
            // public MyType(Foo a, Bar b, Baz c, ...)
            // {
            //     this.a = a;
            //     this.b = b;
            //     this.c = c;
            //     ...
            //     this.ignored1 = default;
            //     this.ignored2 = default;
            //     this.ignored3 = default;
            //     ...
            // }
            ConstructorDeclarationSyntax constructorDeclaration =
                ConstructorDeclaration(hierarchyInfo.Hierarchy[0].QualifiedName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(constructorInfo.Parameters.Select(field => Parameter(Identifier(field.Name)).WithType(IdentifierName(field.Type))).ToArray())
                .AddBodyStatements(constructorInfo.Parameters.Select(field => ParseStatement($"this.{field.Name} = {field.Name};")).ToArray())
                .AddBodyStatements(constructorInfo.DefaultedFields.Select(field => ParseStatement($"this.{field} = default;")).ToArray());

            // Add the unsafe modifier, if needed
            if (constructorInfo.Parameters.Any(static param => param.Type.Contains("*")))
            {
                constructorDeclaration = constructorDeclaration.AddModifiers(Token(SyntaxKind.UnsafeKeyword));
            }

            // Constructor attributes
            AttributeListSyntax[] attributes =
            {
                AttributeList(SingletonSeparatedList(
                    Attribute(IdentifierName("global::System.CodeDom.Compiler.GeneratedCode")).AddArgumentListArguments(
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(typeof(AutoConstructorGenerator).FullName))),
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(typeof(AutoConstructorGenerator).Assembly.GetName().Version.ToString())))))),
                AttributeList(SingletonSeparatedList(Attribute(IdentifierName("global::System.Diagnostics.DebuggerNonUserCode")))),
                AttributeList(SingletonSeparatedList(Attribute(IdentifierName("global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage"))))
            };

            return hierarchyInfo.GetSyntax(constructorDeclaration.AddAttributeLists(attributes));
        }
    }
}
