using System.Text.Json;
using TodoList.Application.Exceptions;
using TodoList.Domain.Exceptions;

namespace TodoList.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemDetails(context, StatusCodes.Status404NotFound, "Recurso não encontrado", ex.Message);
        }
        catch (DomainValidationException ex)
        {
            await WriteProblemDetails(context, StatusCodes.Status400BadRequest, "Requisição inválida", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado ao processar requisição {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemDetails(context, StatusCodes.Status500InternalServerError,
                "Erro interno", "Ocorreu um erro interno no servidor.");
        }
    }

    private static async Task WriteProblemDetails(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            detail
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
