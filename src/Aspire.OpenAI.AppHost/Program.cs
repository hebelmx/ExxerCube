using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Ollama Phi SLM Container
// * By default, Ollama container exposes OpenAI API in port 11434. This configuration changes the port to 7876.

var ollama = builder.AddContainer("ollama", "langchain4j/ollama-phi")
       .WithVolume("/root/.ollama")
       .WithBindMount("./ollamaconfig", "/root/")
       .WithHttpEndpoint(port: 7876, targetPort: 11434, name: "OllamaOpenApiEndpointUri");
//.WithContainerRuntimeArgs("--gpus=all");


var ollamaOpenApiEndpointUri = ollama.GetEndpoint("OllamaOpenApiEndpointUri");

// Ollama Web UI Lite Chat User Interface
// * Aspire.Hosting.NodeJs must be installed to enable adding Npm applications to the solution.
// * Copy Ollama Web UI Lite's files and folders from Github to the folder (/OllamWebUi) under the solution.

builder.AddNpmApp("ollamawebui", "../OllamWebUi", "dev")
    .WithEnvironment("BROWSER", "none")
    .WithExternalHttpEndpoints();

// OpenAI-compatible Custom API
// * Semantic Kernel needs the Ollama container's OpenAI API endpoint url

builder.AddProject<Aspire_OpenAI_Api>("openai-api")
    .WithEnvironment("OllamaOpenApiEndpointUri", ollamaOpenApiEndpointUri)
    .WaitFor(ollama);

builder.Build().Run();