using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SonataCinemaV2.Helper
{
    public class ChatGPTHelper
    {
        private const string API_KEY = "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";
        private const string API_URL = "https://api.openai.com/v1/chat/completions";
        private static readonly HttpClient client = new HttpClient();

        public static async Task<string> GetChatResponse(string userMessage)
        {
            try
            {
                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "Bạn là trợ lý ảo của rạp chiếu phim Sonata Cinema. Hãy trả lời các câu hỏi về lịch chiếu phim, đặt vé, giá vé và các dịch vụ của rạp một cách thân thiện và chuyên nghiệp. Trả lời bằng tiếng Việt."
                        },
                        new
                        {
                            role = "user",
                            content = userMessage
                        }
                    },
                    temperature = 0.7,
                    max_tokens = 500
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");

                var response = await client.PostAsync(API_URL, content);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"API Response: {jsonResponse}");

                if (response.IsSuccessStatusCode)
                {
                    dynamic result = JsonConvert.DeserializeObject(jsonResponse);
                    return result.choices[0].message.content;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Error: {jsonResponse}");
                    return "Xin lỗi, tôi không thể xử lý yêu cầu của bạn lúc này.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                return "Xin lỗi, có lỗi xảy ra trong quá trình xử lý.";
            }
        }
    }
}