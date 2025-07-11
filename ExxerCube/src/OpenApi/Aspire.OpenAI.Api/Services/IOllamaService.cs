using Aspire.OpenAI.Api.Models;

namespace Aspire.OpenAI.Api.Services
{

    public interface IOllamaService
    {
        Task<Dictionary<int, ChatResponse>> GetStreamingChatMessageContentsAsync(ChatRequest request);
        Task<string> GetChatMessageContentAsync(string prompt);
    }

}
