using System;
using System.Text.Json;
using System.Text;
using Aspire.OpenAI.Api.Extensions;
using Aspire.OpenAI.Api.Services;
using Aspire.OpenAI.Api.Models;

namespace Aspire.OpenAI.Api.Endpoints
{
    public static class Endpoints
    {
        public static void RegisterOllamaEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/api/generate", async (HttpContext httpContext, IOllamaService ollamaService) =>
            {
                var request = httpContext.GetRequestBody<GenerateRequest>();
                var response = await ollamaService.GetChatMessageContentAsync(request.prompt);
                return new
                {
                    response
                };
            }).WithName("Generate");

            endpoints.MapGet("/api/version", () => new
            {
                version = "0.1.32"
            }).WithName("Version");

            endpoints.MapGet("/api/tags", () => new
            {
                models = new[] {
                    new
                    {
                        name = "phi:latest",
                        model = "phi:latest",
                        modified_at = DateTime.UtcNow,
                        size = 1602463378,
                        digest = "e2fd6321a5fe6bb3ac8a4e6f1cf04477fd2dea2924cf53237a995387e152ee9c",
                        details = new
                        {
                            parent_model = "",
                            format = "gguf",
                            family = "phi2",
                            families = new[] { "phi2" },
                            parameter_size = "3B",
                            quantization_level = "Q4_0"
                        }
                    }
                }
            }).WithName("Tags");

            endpoints.MapPost("/api/chat", async Task (HttpContext httpContext, IOllamaService ollamaservice, CancellationToken ct) =>
            {
                httpContext.Response.Headers.Add("Content-Type", "text/event-stream");

                var request = httpContext.GetRequestBody<ChatRequest>();
                var responses = await ollamaservice.GetStreamingChatMessageContentsAsync(request);

                foreach (var response in responses.Values)
                {
                    // Server-Sent Events
                    var sseMessage = $"{JsonSerializer.Serialize(response)}";
                    await httpContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(sseMessage));
                    await httpContext.Response.Body.FlushAsync();
                }

            }).WithName("ChatCompletion");
        }
    }
}
