using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.AI
{
    public class OpenAIClient : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly HttpClient _http;

        private readonly string _aiModel = "gpt-4.1-mini";

        public OpenAIClient(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("OpenAI API key is null or empty.", nameof(apiKey));

            _http = new HttpClient
            {
                BaseAddress = new Uri("https://api.openai.com/v1/")
            };
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        public async Task<AiSpellResult> GenerateSpellAsync(string userPrompt, string currentSpellName, uint currentSpellId)
        {
            Logger.Info("Sending OpenAI request for spell generation...");

            string systemPrompt = @"
You are a spell designer for World of Warcraft: Wrath of the Lich King (3.3.5a).

Output ONLY a JSON object matching this class:

class AiSpellDefinition
{
  string Name;
  string Description;
  float? RangeYards;
  int? DirectDamage;
}

Rules:
- Do NOT wrap JSON in backticks.
- Do NOT add comments.
- Only output that JSON object.
";

            StringBuilder input = new StringBuilder();

            input.AppendLine(systemPrompt);
            input.AppendLine();
            input.AppendLine("Current spell (if modifying):");
            input.AppendLine($"Name: {currentSpellName}");
            input.AppendLine($"ID: {currentSpellId}");
            input.AppendLine();
            input.AppendLine("User request:");
            input.AppendLine(userPrompt);

            var body = new
            {
                model = _aiModel,
                input = input.ToString(),
                temperature = 0.2
            };

            string jsonBody = JsonConvert.SerializeObject(body);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("responses", content).ConfigureAwait(false);
            string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Error("OpenAI error: {0}", responseText);
                throw new Exception(responseText);
            }

            // API format:
            // output[0].content[0].text
            var parsed = JObject.Parse(responseText);

            string msg =
                parsed["output"]?[0]?["content"]?[0]?["text"]?.ToString();

            if (string.IsNullOrWhiteSpace(msg))
                throw new Exception("OpenAI result did not contain output content.");

            AiSpellDefinition definition;

            try
            {
                definition = JsonConvert.DeserializeObject<AiSpellDefinition>(msg);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to parse AiSpellDefinition JSON.\nRaw: " + msg);
                throw;
            }

            return new AiSpellResult
            {
                RawContent = msg,
                Definition = definition
            };
        }


        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}
