namespace Aspire.OpenAI.Api.Models
{
    public class ChatResponse
    {
        public string model { get; set; }
        public string created_at { get; set; }
        public Message message { get; set; }
        public bool done { get; set; }

        public int total_duration { get; set; }
        public int load_duration { get; set; }
        public int prompt_eval_count { get; set; }
        public int prompt_eval_duration { get; set; }
        public int eval_count { get; set; }
        public int eval_duration { get; set; }
    }
}
