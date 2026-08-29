using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Lca.Api.Infrastructure;

public sealed class BearerSecurityDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT with sub, tenant_id, and explicit permission claims.",
        };
        return Task.CompletedTask;
    }
}

public sealed class BearerSecurityOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        bool allowsAnonymous = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
        bool requiresAuthorization = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any();
        if (!allowsAnonymous && requiresAuthorization)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document, null)] = [],
            });
        }

        return Task.CompletedTask;
    }
}
