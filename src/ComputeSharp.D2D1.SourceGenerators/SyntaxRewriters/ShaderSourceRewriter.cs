﻿using ComputeSharp.SourceGeneration.Mappings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ComputeSharp.SourceGeneration.SyntaxRewriters;

/// <inheritdoc/>
partial class ShaderSourceRewriter
{
    /// <summary>
    /// Gets whether or not the shader needs the <c>[D2DRequiresScenePosition]</c> attribute.
    /// </summary>
    public bool NeedsD2DRequiresScenePositionAttribute { get; private set; }

    /// <inheritdoc/>
    private partial SyntaxNode RewriteSampledTextureAccess(IInvocationOperation operation, ExpressionSyntax expression, ArgumentSyntax arguments)
    {
        string fieldName = (operation.Instance as IFieldReferenceOperation)?.Member.Name ?? "";

        _ = HlslKnownKeywords.TryGetMappedName(fieldName, out string? mapped);

        // Transform an indexer syntax into a sampling call with the implicit static linear sampler.
        // For instance: texture.Sample(uv) will be rewritten as texture.Sample(__sampler__texture, uv).
        return
            InvocationExpression(expression)
            .AddArgumentListArguments(Argument(IdentifierName($"__sampler__{mapped ?? fieldName}")), arguments);
    }

    /// <inheritdoc/>
    private partial void TrackKnownPropertyAccess(IMemberReferenceOperation operation, MemberAccessExpressionSyntax node, string mappedName)
    {
        // No special tracking is needed for D2D1 shaders
    }

    /// <inheritdoc/>
    private partial void TrackKnownMethodInvocation(string metadataName)
    {
        // Track whether the method needs [D2DRequiresScenePosition]
        if (HlslKnownMethods.NeedsD2DRequiresScenePositionAttribute(metadataName))
        {
            NeedsD2DRequiresScenePositionAttribute = true;
        }
    }
}
