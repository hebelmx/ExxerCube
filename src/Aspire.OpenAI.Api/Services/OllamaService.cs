using Aspire.OpenAI.Api.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Aspire.OpenAI.Api.Services
{
    public class OllamaService : IOllamaService
    {
        private Kernel _kernel;
        private IChatCompletionService _chatCompletionService;

        public OllamaService(Kernel kernel)
        {
            _kernel = kernel;
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        /// <summary>
        /// Get a single chat message content for the prompt and settings.
        /// </summary>
        public async Task<string> GetChatMessageContentAsync(string prompt)
        {
            var aiData = await _chatCompletionService.GetChatMessageContentAsync(prompt);
            return aiData.ToString();
        }

        /// <summary>
        /// Get streaming chat contents for the chat history provided using the specified settings.
        /// </summary>
        public async Task<Dictionary<int, ChatResponse>> GetStreamingChatMessageContentsAsync(ChatRequest request)
        {
            var chat = PrepareChatHistory(request);
            var aiData = _chatCompletionService.GetStreamingChatMessageContentsAsync(chat, kernel: _kernel);
            Dictionary<int, ChatResponse> chatResponses = [];

            // Gather the full sentences of response
            await foreach (var result in aiData)
            {
                chatResponses.TryGetValue(result.ChoiceIndex, out ChatResponse chatResponse);

                if (chatResponse == null)
                {
                    chatResponses[result.ChoiceIndex] = new ChatResponse()
                    {
                        model = result?.ModelId,
                        created_at = result?.Metadata["Created"].ToString(),
                        message = new Message()
                        {
                            content = result?.Content,
                            role = result?.Role.ToString()
                        }
                    };
                }
                else
                {
                    chatResponse.message.content += result.Content;
                }
            }

            return chatResponses;
        }

        private ChatHistory PrepareChatHistory(ChatRequest request)
        {
            ChatHistory chat = new("Hello!");

            if (request?.messages != null)
            {
                foreach (var item in request.messages)
                {
                    var role = item.role == "user" ? AuthorRole.User : AuthorRole.Assistant;
                    chat.AddMessage(role, item.content);
                }
            }
            return chat;
        }
    }
}
