using Aspire.OpenAI.Api.Endpoints;
using Aspire.OpenAI.Api.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var ollamaOpenApiEndpoint = builder.Configuration["OllamaOpenApiEndpointUri"];

if (!string.IsNullOrEmpty(ollamaOpenApiEndpoint))
{
    // Install Microsoft.SemanticKernel NuGet package and add Kernel to the Dependency Injection.
    builder.Services
    .AddKernel()
        .AddOpenAIChatCompletion("phi", new Uri(ollamaOpenApiEndpoint), null);
}

builder.Services.AddScoped<IOllamaService, OllamaService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Register Minimal API endpoints
app.RegisterOllamaEndpoints();

app.Run();