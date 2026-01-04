using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using NLog;
using SpellEditor.Sources.AI;
using SpellEditor.Sources.BLP;

namespace SpellEditor
{
    public partial class OpenAIWindow : MetroWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly MainWindow _mainWindow;
        private OpenAIClient _client;
        private AiSpellResult _lastResult;

        private bool IsAskQuestionMode => RadioAskQuestion.IsChecked == null ? false : RadioAskQuestion.IsChecked.Value;
        private bool IsBalanceMode => RadioBalanceSpell.IsChecked == null ?  false : RadioBalanceSpell.IsChecked.Value;
        private bool IsModifySpell => RadioModifyExisting.IsChecked == null ? false : RadioModifyExisting.IsChecked.Value;
        private bool IsNewSpell => RadioCreateNew.IsChecked == null ? false : RadioCreateNew.IsChecked.Value;


        public OpenAIWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

            InitializeComponent();
        }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;

            try
            {
                // For now: read API key from environment variable.
                var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User)
                          ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.Machine)
                          ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    StatusTextBlock.Text = "Set OPENAI_API_KEY environment variable to use the AI tool.";
                    GenerateButton.IsEnabled = false;
                    ApplyButton.IsEnabled = false;
                    return;
                }

                _client = new OpenAIClient(apiKey);
                UpdateMode();
                StatusTextBlock.Text = "Ready. Describe the spell you want.";

                // TODO: Implement balance spell functionality
                RadioBalanceSpell.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize OpenAI client.");
                StatusTextBlock.Text = "Failed to initialize OpenAI client. See log.";
                GenerateButton.IsEnabled = false;
                ApplyButton.IsEnabled = false;
            }
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error(e.Exception, "Unhandled exception in OpenAI window.");
            File.WriteAllText("error.txt", e.Exception.ToString(), Encoding.GetEncoding(0));
            e.Handled = true;
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_client == null)
            {
                StatusTextBlock.Text = "OpenAI client not initialized.";
                return;
            }

            var prompt = PromptTextBox.Text;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                StatusTextBlock.Text = "Enter a description for the spell.";
                return;
            }

            ShowSpinner(true);
            try
            {
                GenerateButton.IsEnabled = false;
                ApplyButton.IsEnabled = false;
                StatusTextBlock.Text = "Starting...";

                bool useSimilar = UseSimilarSpellsCheckBox.IsChecked == true;
                int maxExamples = SimilarSpellCountCombo.SelectedIndex + 1;
                List<AiSimilarSpellSummary> similar = null;
                string currentName = _mainWindow.GetSpellNameById(_mainWindow.selectedID);
                uint currentId = _mainWindow.selectedID;

                if (useSimilar)
                {
                    StatusTextBlock.Text = "Finding similar spells...";
                    similar = AiSimilarSpellFinder.FindSimilarSpells(prompt, maxExamples);
                    SimilarSpellsTextBox.Text = FormatSimilarSpellsForDisplay(similar);
                    SimilarSpellsExpander.IsExpanded = true;
                }
                else
                {
                    SimilarSpellsTextBox.Text = "Similar spell examples disabled.";
                    SimilarSpellsExpander.IsExpanded = false;
                }

                if (IsAskQuestionMode)
                {
                    // Plain chat mode
                    var answer = await _client.AskQuestionAsync(prompt, currentName, currentId, similar);
                    // Update rich text box, need to manually handle line breaks to avoid extra spacing
                    QuestionAnswerBox.Document.Blocks.Clear();
                    Paragraph p = new Paragraph
                    {
                        Margin = new Thickness(0)
                    };
                    foreach (string line in answer.Split('\n'))
                    {
                        p.Inlines.Add(new Run(line));
                        p.Inlines.Add(new LineBreak());
                    }
                    QuestionAnswerBox.Document.Blocks.Add(p);
                    ApplyButton.IsEnabled = false;
                    // END
                    return;
                }

                if (IsBalanceMode)
                {
                    // TODO
                    ApplyButton.IsEnabled = false;
                    return;
                }

                StatusTextBlock.Text = "Running AI algorithm...";
                var result = await _client.GenerateSpellAsync(prompt, currentName, currentId, IsModifySpell, similar);
                _lastResult = result;

                DisplayJson(result.RawContent ?? string.Empty);

                if (result.Definition != null)
                {
                    SummaryNameTextBlock.Text = result.Definition.Name ?? "(unchanged)";
                    SummaryDescriptionTextBlock.Text = result.Definition.Description ?? "(unchanged)";
                    SummaryRangeTextBlock.Text = result.Definition.RangeYards?.ToString() ?? "(unchanged)";
                    SummaryDamageTextBlock.Text = result.Definition.DirectDamage?.ToString() ?? "(unchanged)";
                    UpdateIconPreview(result.Definition);

                    ApplyButton.IsEnabled = true;
                }
                else
                {
                    SummaryNameTextBlock.Text = "Failed to parse JSON.";
                    SummaryDescriptionTextBlock.Text = string.Empty;
                    SummaryRangeTextBlock.Text = string.Empty;
                    SummaryDamageTextBlock.Text = string.Empty;
                    ApplyButton.IsEnabled = false;
                }

                StatusTextBlock.Text = "Response received. Review and click 'Apply Changes' if happy.";
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during OpenAI spell generation.");
                StatusTextBlock.Text = "Error: " + ex.Message;
                ApplyButton.IsEnabled = false;
            }
            finally
            {
                GenerateButton.IsEnabled = true;
                ShowSpinner(false);
            }
        }

        private void UpdateIconPreview(AiSpellDefinition def)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(def.Icon))
                {
                    SummaryIconImage.Source = null;
                    return;
                }

                // Use any icon ID already chosen, or pick one and remember it
                uint iconId = def.IconId ?? AiSpellMapper.PickBestIconId(def.Icon);
                def.IconId = iconId;

                if (iconId == 0)
                {
                    SummaryIconImage.Source = null;
                    return;
                }

                var iconDbc = Sources.DBC.DBCManager
                    .GetInstance()
                    .FindDbcForBinding("SpellIcon") as Sources.DBC.SpellIconDBC;
                if (iconDbc == null)
                    return;

                string iconName = iconDbc.GetIconPath(iconId) + ".blp";
                SummaryIconImage.Source = BlpManager.GetInstance().GetImageSourceFromBlpPath(iconName);
            }
            catch
            {
                SummaryIconImage.Source = null;
            }
        }



        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastResult == null || _lastResult.Definition == null)
            {
                StatusTextBlock.Text = "Nothing to apply.";
                return;
            }

            try
            {
                if (RadioModifyExisting.IsChecked == true)
                {
                    _mainWindow.ModifyExistingSpellFromAi(_lastResult.Definition, true);
                    StatusTextBlock.Text = "Existing spell updated.";
                }
                else if (RadioCreateNew.IsChecked == true)
                {
                    var newId = _mainWindow.CreateNewSpellFromAi(_lastResult.Definition);
                    StatusTextBlock.Text = $"New spell created with ID {newId}.";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Error: " + ex.Message;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _client?.Dispose();
        }

        private string FormatJson(string json)
        {
            try
            {
                // Parse → pretty print
                var parsed = Newtonsoft.Json.Linq.JToken.Parse(json);
                return parsed.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch
            {
                // If parse fails, return unmodified
                return json;
            }
        }

        private void DisplayJson(string json)
        {
            json = FormatJson(json);
            JsonOutputBox.Document.Blocks.Clear();

            // Create a single paragraph
            Paragraph paragraph = new Paragraph
            {
                Margin = new Thickness(0)
            };

            // Regex-based tokenizer (strings, numbers, braces, colons, commas)
            var tokens = Regex.Matches(
                json,
                "\".*?\"|\\d+|\\{|\\}|\\[|\\]|:|,|\\s+"
            );

            foreach (Match token in tokens)
            {
                string text = token.Value;
                Run run = new Run(text);

                // Apply color rules
                if (text.StartsWith("\"") && text.EndsWith("\""))
                {
                    run.Foreground = Brushes.Orange;          // JSON strings
                }
                else if (Regex.IsMatch(text, "^\\d+$"))
                {
                    run.Foreground = Brushes.LightGreen;      // Numbers
                }
                else if (text == "{" || text == "}" || text == "[" || text == "]")
                {
                    run.Foreground = Brushes.CadetBlue;       // Braces
                }
                else if (text == ":")
                {
                    run.Foreground = Brushes.Gray;            // colon
                }
                else if (text == ",")
                {
                    run.Foreground = Brushes.White;           // comma
                }
                else
                {
                    run.Foreground = Brushes.White;           // whitespace or other
                }

                paragraph.Inlines.Add(run);
            }

            JsonOutputBox.Document.Blocks.Add(paragraph);
        }

        private void ShowSpinner(bool show)
        {
            if (!IsAskQuestionMode)
                LoadingSpinner.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            else
                LoadingSpinner2.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            PromptTextBox.IsEnabled = !show;
            GenerateButton.IsEnabled = !show;
            ApplyButton.IsEnabled = !show;
        }

        private void ModeRadio_Checked(object sender, RoutedEventArgs e)
        {
            UpdateMode();
        }


        private void UpdateMode()
        {
            if (RadioAskQuestion == null)
                return;

            bool ask = RadioAskQuestion.IsChecked != null && RadioAskQuestion.IsChecked == true;

            OutputContainer.Visibility = ask ? Visibility.Collapsed : Visibility.Visible;
            QuestionContainer.Visibility = ask ? Visibility.Visible : Visibility.Collapsed;

            AIPromptLabel.Text = ask ?
                "Ask your question:" :
                "Describe the spell you want to create or modify:";

            ApplyButton.IsEnabled = !ask && !IsBalanceMode;
            GenerateButton.IsEnabled = !IsBalanceMode;
        }

        private string FormatSimilarSpellsForDisplay(List<AiSimilarSpellSummary> list)
        {
            if (list == null || list.Count == 0)
                return "No similar spells found or disabled.";

            var sb = new StringBuilder();

            int index = 1;
            foreach (var s in list)
            {
                sb.AppendLine("=== Similar Spell " + index + " ===");
                sb.AppendLine(s.SummaryText.Trim());
                sb.AppendLine();
                index++;
            }

            return sb.ToString();
        }
    }
}
