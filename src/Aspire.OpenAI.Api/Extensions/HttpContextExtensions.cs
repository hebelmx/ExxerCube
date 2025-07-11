using System.Text;
using System.Text.Json;

namespace Aspire.OpenAI.Api.Extensions
{
    public static class HttpContextExtensions
    {
        public static async Task<T> GetRequestBodyAsync<T>(this HttpContext context)
        {
            string body = "";
            using (StreamReader stream = new StreamReader(context.Request.Body))
            {
                body = await stream.ReadToEndAsync();
            }

            return JsonSerializer.Deserialize<T>(body);
        }

        public static T GetRequestBody<T>(this HttpContext context)
        {
            string body = "";
            using (StreamReader stream = new StreamReader(context.Request.Body))
            {
                body = stream.ReadToEndAsync().GetAwaiter().GetResult();
            }

            return JsonSerializer.Deserialize<T>(body);
        }

        public static async Task WriteResponse<T>(this HttpContext context, T response)
        {
            var sseMessage = $"{JsonSerializer.Serialize(response)}";
            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(sseMessage));
            await context.Response.Body.FlushAsync();
        }
    }
}
