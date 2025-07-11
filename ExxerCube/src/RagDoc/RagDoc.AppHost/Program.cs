using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithOpenWebUI(); ;
var chat = ollama.AddModel("chat", "llama3.2");
var embeddings = ollama.AddModel("embeddings", "all-minilm");

var vectorDB = builder.AddQdrant("vectordb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var seq = builder.AddSeq("seq")
    .ExcludeFromManifest()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("ACCEPT_EULA", "Y");

var webApp = builder.AddProject<Projects.RagDoc_Web>("aichatweb-app");
webApp
    .WithReference(chat)
    .WithReference(embeddings)
    .WithReference(seq)
    .WaitFor(chat)
    .WaitFor(embeddings);
webApp
    .WithReference(vectorDB)
    .WaitFor(vectorDB)
    .WaitFor(seq);

builder.Build().Run();