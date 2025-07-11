namespace Aspire.OpenAI.Api.Models
{
    public class ChatRequest
    {
        public string model { get; set; }
        public List<Message> messages { get; set; }
        public Options options { get; set; }
    }
}
