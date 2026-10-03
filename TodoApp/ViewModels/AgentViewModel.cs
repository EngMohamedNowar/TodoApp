using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Windows.Input;
using TodoApp.Models;
using TodoApp.Repositories;
using TodoApp.Services;

namespace TodoApp.ViewModels
{
    /// <summary>
    /// View model backing the AI Agent chat window. Keeps the running conversation
    /// and exposes commands to send messages, test the connection, and save settings.
    /// </summary>
    public class AgentViewModel : ViewModelBase
    {
        private readonly AiAgentService _agent;
        private readonly List<AiAgentService.ReqMsg> _history = new();

        private string _input = string.Empty;
        private bool _isBusy;
        private string _endpoint;
        private string _model;
        private string _statusText = "Ready. Start LM Studio → Developer → Start Server first (no API key needed).";

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public string Input
        {
            get => _input;
            set => SetField(ref _input, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetField(ref _isBusy, value);
        }

        public string Endpoint
        {
            get => _endpoint;
            set => SetField(ref _endpoint, value);
        }

        public string Model
        {
            get => _model;
            set => SetField(ref _model, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        public ICommand SendCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand TestConnectionCommand { get; }

        public AgentViewModel(ITodoRepository repo, Func<System.Threading.Tasks.Task>? onTasksChanged = null)
        {
            _agent = new AiAgentService(repo, onTasksChanged);
            _endpoint = _agent.Endpoint;
            _model = _agent.Model;

            SendCommand = new RelayCommand(
                _ => SendAsync(),
                _ => !IsBusy && !string.IsNullOrWhiteSpace(Input));
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
            TestConnectionCommand = new RelayCommand(_ => TestConnectionAsync());

            _history.Add(new AiAgentService.ReqMsg
            {
                Role = "system",
                Content = "You are a helpful task-management assistant for the 'My Tasks' app, a local offline todo list. " +
                          "You can manage the user's tasks by calling the available tools. " +
                          "When the user asks to create, list, complete, delete, or search tasks, use the appropriate tool. " +
                          "After using a tool, briefly tell the user what you did in their language (Arabic or English). " +
                          "Never invent task ids; use the ids returned by list_tasks/search_tasks."
            });
        }

        private async System.Threading.Tasks.Task SendAsync()
        {
            var text = Input.Trim();
            if (string.IsNullOrWhiteSpace(text) || IsBusy) return;

            Input = string.Empty;
            IsBusy = true;
            StatusText = "Thinking...";

            Messages.Add(new ChatMessage { Role = "user", Text = text });
            ScrollToEndRequested?.Invoke(this, EventArgs.Empty);

            try
            {
                _agent.ApplySettings(Endpoint, Model);

                var reply = await _agent.RunAsync(text, _history, onToolUsed: tool =>
                {
                    Messages.Add(new ChatMessage { Role = "tool", Text = tool });
                    ScrollToEndRequested?.Invoke(this, EventArgs.Empty);
                });

                if (Model != _agent.Model)
                    Model = _agent.Model;

                Messages.Add(new ChatMessage { Role = "assistant", Text = reply });
                StatusText = "Ready.";
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage { Role = "assistant", Text = $"⚠ Error: {ex.Message}" });
                StatusText = "Error.";
            }
            finally
            {
                IsBusy = false;
                ScrollToEndRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SaveSettings()
        {
            _agent.ApplySettings(Endpoint, Model);
            var prefs = SettingsStore.Load();
            prefs.AiEndpoint = string.IsNullOrWhiteSpace(Endpoint) ? "http://localhost:1234/v1" : Endpoint.Trim();
            prefs.AiModel = string.IsNullOrWhiteSpace(Model) ? "local-model" : Model.Trim();
            SettingsStore.Save(prefs);
            StatusText = "Settings saved.";
        }

        private async System.Threading.Tasks.Task TestConnectionAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Testing connection...";

            try
            {
                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                var url = Endpoint.TrimEnd('/') + "/models";
                var resp = await http.GetAsync(url);
                if (resp.IsSuccessStatusCode)
                {
                    var models = await _agent.GetModelsAsync();
                    if (models.Count == 0)
                    {
                        StatusText = "✅ Connected, but no model is loaded. Load one in LM Studio first.";
                    }
                    else
                    {
                        var wanted = (Model ?? string.Empty).Trim();
                        var picked = models.FirstOrDefault(m =>
                            !string.IsNullOrEmpty(wanted) &&
                            m.Equals(wanted, StringComparison.OrdinalIgnoreCase));

                        if (picked == null) picked = models[0];

                        if (picked != wanted)
                        {
                            Model = picked;
                            _agent.ApplySettings(Endpoint, picked);
                            var prefs = SettingsStore.Load();
                            prefs.AiModel = picked;
                            SettingsStore.Save(prefs);
                        }

                        StatusText = models.Count == 1
                            ? $"✅ Connected. Using model {picked}."
                            : $"✅ Connected. {models.Count} models loaded, using {picked}.";
                    }
                }
                else
                {
                    StatusText = $"⚠ Server responded with {resp.StatusCode}.";
                }
            }
            catch (Exception)
            {
                StatusText = "⚠ Cannot reach the server. Start LM Studio → Developer → Start Server, " +
                             "then press \"Test connection\" again.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event EventHandler? ScrollToEndRequested;
    }

    public class ChatMessage : ViewModelBase
    {
        private string _role = string.Empty;
        private string _text = string.Empty;

        public string Role
        {
            get => _role;
            set
            {
                if (SetField(ref _role, value))
                {
                    OnPropertyChanged(nameof(IsUser));
                    OnPropertyChanged(nameof(IsTool));
                }
            }
        }

        public string Text
        {
            get => _text;
            set => SetField(ref _text, value);
        }

        public bool IsUser => Role == "user";
        public bool IsTool => Role == "tool";
    }
}
