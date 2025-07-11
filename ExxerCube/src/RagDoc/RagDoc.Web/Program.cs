using Microsoft.Extensions.Options;
using RagDoc.Web.Components;
using RagDoc.Web.Models;
using RagDoc.Web.Services;
using RagDoc.Web.Services.Ingestion;
using Microsoft.Extensions.AI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        builder.AddOllamaApiClient("chat")
            .AddChatClient()
            .UseFunctionInvocation()
            .UseOpenTelemetry(configure: c =>
                c.EnableSensitiveData = builder.Environment.IsDevelopment());
        builder.AddOllamaApiClient("embeddings")
            .AddEmbeddingGenerator();

        builder.AddSeqEndpoint(connectionName: "seq");

        builder.AddQdrantClient("vectordb");
        builder.Services.AddQdrantCollection<Guid, IngestedChunk>("data-ragdoc-chunks");
        builder.Services.AddQdrantCollection<Guid, IngestedDocument>("data-ragdoc-documents");
        builder.Services.AddScoped<DataIngestor>();
        builder.Services.AddSingleton<SemanticSearch>();

        builder.Services.Configure<PdfDocumentsSettings>(
            builder.Configuration.GetSection("PdfDocuments"));

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseAntiforgery();

        app.UseStaticFiles();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        // Load configured directories
        var settings = app.Services.GetRequiredService<IOptions<PdfDocumentsSettings>>().Value;
        var allDirectories = new HashSet<string>(
            settings.Directories ?? [],
            StringComparer.OrdinalIgnoreCase);

        // Always include the default wwwroot/Data directory
        var defaultDir = Path.Combine(app.Environment.WebRootPath, "Data");
        allDirectories.Add(defaultDir);

        // Ingest all valid directories
        foreach (var dir in allDirectories)
        {
            if (Directory.Exists(dir))
            {
                var source = new PDFDirectorySource(dir);
                await DataIngestor.IngestDataAsync(app.Services, source);
            }
            else
            {
                app.Logger.LogWarning("PDF directory does not exist: {Directory}", dir);
            }
        }
    }
}