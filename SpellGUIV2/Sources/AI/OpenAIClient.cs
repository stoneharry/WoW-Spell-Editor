using ControlzEx.Standard;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using SpellEditor.Sources.Database;
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
            bool isModifySpell,
            List<AiSimilarSpellSummary> similarSpells = null)
        {
            Logger.Info("Sending OpenAI request for spell generation...");

            var systemPrompt = LoadSystemPromptFromFileOrDefault(isModifySpell);
            if (systemPrompt == null)
                throw new Exception("Misisng AI folder prompt file");

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

            AppendModifyPromptText(ref input, currentSpellId);

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

        private void AppendModifyPromptText(ref StringBuilder input, uint currentSpellId)
        {
            using (var adapter = AdapterFactory.Instance.GetAdapter(false))
            {
                using (var query = adapter.Query("SELECT * FROM spell WHERE id = " + currentSpellId))
                {
                    var snapshot = AiSpellSemanticExtractor.Extract(query.Rows[0]);
                    input.AppendLine("=== MODIFYING SPELL ====");
                    input.AppendLine("Treat this snapshot as the existing spell state.");
                    input.AppendLine("Only output fields you want to change.");
                    input.AppendLine("Do NOT repeat unchanged values.");
                    var snapshotJson = JsonConvert.SerializeObject(
                        snapshot,
                        Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Ignore
                        });
                    input.AppendLine(snapshotJson);
                    input.AppendLine();
                    Logger.Info("Current spell serialised:\n" + snapshotJson);
                }
            }
        }

        private string LoadSystemPromptFromFileOrDefault(bool isModifySpell)
        {
            string promptPath = "";
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                promptPath = Path.Combine(exeDir, "AI", $"AI-Prompt{(isModifySpell ? "-Mod" : "-Gen")}.txt");

                if (File.Exists(promptPath))
                {
                    Logger.Info("Loading AI system prompt from file: " + promptPath);
                    return File.ReadAllText(promptPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to load: " + promptPath);
            }
            return null;
        }

        public async Task<string> AskQuestionAsync(
            string userPrompt,
            string currentSpellName,
            uint currentSpellId,
            List<AiSimilarSpellSummary> similarSpells)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a WoW 3.3.5a spell designer and technical assistant.");
            sb.AppendLine("Answer the user's question in plain English.");
            sb.AppendLine("Do NOT output JSON. Do NOT use backticks or Markdown code blocks.");
            sb.AppendLine("You may use bullet lists and short headings, but keep it readable.");
            sb.AppendLine();

            // Optional: insert similar spells section
            if (similarSpells != null && similarSpells.Count > 0)
            {
                sb.AppendLine("Here are some similar existing spells from the game as examples.");
                sb.AppendLine("These are NOT to be edited; they are only reference for style, scale, and mechanics.");
                sb.AppendLine();

                int index = 1;
                foreach (var s in similarSpells)
                {
                    sb.AppendLine("=== Similar Spell " + index + " ===");
                    sb.AppendLine(s.SummaryText.TrimEnd());
                    sb.AppendLine();
                    index++;
                }

                sb.AppendLine("End of existing examples.");
                sb.AppendLine();
            }

            sb.AppendLine("Current editor context:");
            sb.AppendLine("Selected spell name: " + (currentSpellName ?? "(none)"));
            sb.AppendLine("Selected spell ID: " + currentSpellId);
            sb.AppendLine();
            sb.AppendLine("User question:");
            sb.AppendLine(userPrompt ?? string.Empty);

            string bodyJson = JsonConvert.SerializeObject(new
            {
                model = _aiModel,
                input = new object[]
                {
                    new {
                        role = "user",
                        content = sb.ToString()
                    }
                },
                temperature = 0.4
            });

            Logger.Info("Sending OpenAI request for AskQuestionAsync...");

            var response = await _http.PostAsync(
                "responses",
                new StringContent(bodyJson, Encoding.UTF8, "application/json")
            ).ConfigureAwait(false);

            string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Error("OpenAI error (AskQuestionAsync): {0}", responseText);
                throw new Exception(responseText);
            }

            var parsed = JObject.Parse(responseText);
            string content =
                parsed["output"]?[0]?["content"]?[0]?["text"]?.ToString();

            if (string.IsNullOrWhiteSpace(content))
                throw new Exception("OpenAI AskQuestionAsync result did not contain output content.");

            return content.Trim();
        }

        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}
