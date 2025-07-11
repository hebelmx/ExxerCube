namespace Aspire.OpenAI.Api.Models
{
    public class GenerateRequest
    {
        public string model { get; set; }
        public string prompt { get; set; }
        public bool stream { get; set; }
    }
}
