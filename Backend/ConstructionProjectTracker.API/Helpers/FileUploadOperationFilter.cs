using ConstructionProjectTracker.API.DTOs.Documents;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ConstructionProjectTracker.API.Helpers;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetParameters().All(p => p.ParameterType != typeof(UploadDocumentDto)))
            return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties =
                        {
                            ["projectId"] = new OpenApiSchema { Type = "integer", Format = "int32" },
                            ["category"] = new OpenApiSchema { Type = "string" },
                            ["file"] = new OpenApiSchema { Type = "string", Format = "binary" }
                        },
                        Required = new HashSet<string> { "projectId", "category", "file" }
                    }
                }
            }
        };
    }
}
