using Frostvein.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Frostvein.GameObject.Extension.Message;
using Frostvein.Domain;
using System.Windows.Forms;

namespace Frostvein.GameObject.Extension.Translator
{
    public static class TranslatorExtension
    {
        private static readonly HttpClient client = new HttpClient
        {
            DefaultRequestHeaders = { { "Ocp-Apim-Subscription-Key", APIConfiguration.Key } }
        };

        public static async Task TranslateText(string Text)
        {
            var TextToTranslate = Text;
            var TranslatedText = await Translate(TextToTranslate, "en");
            Console.WriteLine(TranslatedText);
        }

        public static async Task<string> Translate(string Text, string Language)
        {
            var EncodedText = WebUtility.UrlEncode(Text);
            var URI = $"https://api.microsofttranslator.com/V2/Http.svc/Translate?" + $"to{Language}&text={EncodedText}";
            var Result = await client.GetStringAsync(URI);
            return XElement.Parse(Result).Value;
        }

        public static async Task TranslateChat(ClientSession Session, string Text)
        {
            if (Session.Account.Language == null)
            {
                Session.SendPacket("info You did not set a Language\nPlease visit the Language NPC at NosVille");
                return;
            }
            string route = $"/translate?api-version=3.0&from={Session.Account.Language}&to=en";
            string textToTranslate = Text;
            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(APIConfiguration.Endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", APIConfiguration.Key);
                request.Headers.Add("Ocp-Apim-Subscription-Region", APIConfiguration.Location);
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                string result = await response.Content.ReadAsStringAsync();
                string result2 = string.Join(string.Empty, result.Skip(26));
                var charsToRemove = new string[] { "]", "}", "." };
                foreach (var c in charsToRemove)
                {
                    result2 = result2.Replace(c, string.Empty);
                }
                string result3 = result2.Remove(result2.Length - 10);
                string finalResult = result3.Replace('"', ' ');
                //MessageExtension.SendYellow(Session, result);
                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay($"[{Session.Character.Name}][Translated]: " + finalResult, 10), ReceiverType.All);
                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(finalResult, 1), ReceiverType.All);
            }
        }
        public static async Task TranslateCommand(ClientSession Session, string Text, string From, string To)
        {
            string route = $"/translate?api-version=3.0&from={From}&to={To}";
            string textToTranslate = Text;
            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(APIConfiguration.Endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", APIConfiguration.Key);
                request.Headers.Add("Ocp-Apim-Subscription-Region", APIConfiguration.Location);
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                string result = await response.Content.ReadAsStringAsync();
                string result2 = string.Join(string.Empty, result.Skip(26));
                var charsToRemove = new string[] { "]", "}", "." };
                foreach (var c in charsToRemove)
                {
                    result2 = result2.Replace(c, string.Empty);
                }
                string result3 = result2.Remove(result2.Length - 10);
                string finalResult = result3.Replace('"', ' ');
                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay($"[{Session.Character.Name}][Translated]: " + finalResult, 10), ReceiverType.All);
                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(finalResult, 1), ReceiverType.All);
            }
        }
    }
}
