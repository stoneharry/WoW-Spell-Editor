using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
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
        public async Task<AiSpellResult> GenerateSpellAsync(
            string userPrompt,
            string currentSpellName,
            uint currentSpellId,
            List<AiSimilarSpellSummary> similarSpells = null)
        {
            Logger.Info("Sending OpenAI request for spell generation...");

            var systemPrompt = LoadSystemPromptFromFileOrDefault();
            if (systemPrompt == null)
                throw new Exception("No AI-Prompt.txt file found.");

            StringBuilder input = new StringBuilder();

            input.AppendLine(systemPrompt);
            input.AppendLine();
            input.AppendLine("Current spell (if modifying):");
            input.AppendLine($"Name: {currentSpellName}");
            input.AppendLine($"ID: {currentSpellId}");
            input.AppendLine();

            // Optional: insert similar spells section
            if (similarSpells != null && similarSpells.Count > 0)
            {
                input.AppendLine("Here are some similar existing spells from the game as examples.");
                input.AppendLine("These are NOT to be edited; they are only reference for style, scale, and mechanics.");
                input.AppendLine();

                int index = 1;
                foreach (var s in similarSpells)
                {
                    input.AppendLine("=== Similar Spell " + index + " ===");
                    input.AppendLine(s.SummaryText.TrimEnd());
                    input.AppendLine();
                    index++;
                }

                input.AppendLine("End of existing examples.");
                input.AppendLine();
            }

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

        private string LoadSystemPromptFromFileOrDefault()
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string promptPath = Path.Combine(exeDir, "AI-Prompt.txt");

                if (File.Exists(promptPath))
                {
                    Logger.Info("Loading AI system prompt from file: " + promptPath);
                    return File.ReadAllText(promptPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to load AI-Prompt.txt.");
            }
            return null;
        }

        public async Task<string> AskQuestionAsync(string userPrompt, List<AiSimilarSpellSummary> similar)
        {
            var sb = new StringBuilder();

            if (similar != null && similar.Count > 0)
            {
                sb.AppendLine("Similar spells for context:");
                foreach (var s in similar)
                {
                    sb.AppendLine(s.SummaryText);
                    sb.AppendLine();
                }
            }

            sb.AppendLine("User question:");
            sb.AppendLine(userPrompt);

            var body = new
            {
                model = _aiModel,
                input = new object[]
                {
                    new { role="system", content="You are a WoW spell design expert. Answer clearly." },
                    new { role="user", content = sb.ToString() }
                }
            };

            var jsonBody = JsonConvert.SerializeObject(body);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("responses", content);
            var text = await response.Content.ReadAsStringAsync();

            var parsed = JObject.Parse(text);
            return parsed["output"]?[0]?["content"]?[0]?["text"]?.ToString()
                   ?? "(no response)";
        }

        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}
