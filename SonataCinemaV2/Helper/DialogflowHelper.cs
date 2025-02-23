using Google.Apis.Auth.OAuth2;
using Google.Cloud.Dialogflow.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace SonataCinemaV2.Helper
{
    public class DialogflowHelper
    {
        private static readonly string ProjectId = "newagent-vnks";
        private static readonly string JsonPath = HttpContext.Current.Server.MapPath("~/App_Data/newagent-vnks-78bf30b8b9bd.json");

        public static async Task<string> GetResponseFromDialogflow(string message, string sessionId)
        {
            try
            {
                var credential = GoogleCredential.FromFile(JsonPath);
                var builder = new SessionsClientBuilder
                {
                    CredentialsPath = JsonPath
                };
                var client = builder.Build();

                var sessionName = SessionName.FromProjectSession(ProjectId, sessionId);
                var textInput = new TextInput
                {
                    Text = message,
                    LanguageCode = "vi"
                };
                var queryInput = new QueryInput
                {
                    Text = textInput
                };

                var response = await client.DetectIntentAsync(sessionName, queryInput);
                return response.QueryResult.FulfillmentText;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dialogflow Error: {ex.Message}");
                return "Xin lỗi, có lỗi xảy ra. Vui lòng thử lại sau.";
            }
        }
    }
}