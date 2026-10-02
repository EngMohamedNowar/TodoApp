using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TodoApp.Models;
using TodoApp.Repositories;

namespace TodoApp.Services
{
    /// <summary>
    /// Talks to a local LM Studio server (OpenAI-compatible /v1 API) and runs a
    /// small task-management agent. The model can call tools that read and mutate
    /// the todo database through <see cref="ITodoRepository"/>.
    /// </summary>
    public class AiAgentService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(2) };
        private readonly ITodoRepository _repo;
        private readonly Func<Task>? _onTasksChanged;

        public string Endpoint { get; set; }
        public string Model { get; set; }

        public AiAgentService(ITodoRepository repo, Func<Task>? onTasksChanged = null)
        {
            _repo = repo;
            _onTasksChanged = onTasksChanged;

            var prefs = SettingsStore.Load();
            Endpoint = prefs.AiEndpoint;
            Model = string.IsNullOrWhiteSpace(prefs.AiModel) ? "local-model" : prefs.AiModel;
        }

        public void ApplySettings(string endpoint, string model)
        {
            Endpoint = string.IsNullOrWhiteSpace(endpoint) ? "http://localhost:1234/v1" : endpoint.Trim();
            Model = string.IsNullOrWhiteSpace(model) ? "local-model" : model;
        }

        /// <summary>Runs the agent on a user message using the running conversation history.</summary>
        public async Task<string> RunAsync(
            string userMessage,
            List<ReqMsg> history,
            Action<string>? onToolUsed = null,
            CancellationToken ct = default)
        {
            history.Add(new ReqMsg { Role = "user", Content = userMessage });

            for (int step = 0; step < 8; step++)
            {
                var request = new ChatRequest
                {
                    Model = Model,
                    Messages = history,
                    Tools = BuildTools()
                };

                var url = Endpoint.TrimEnd('/') + "/chat/completions";
                var payload = JsonSerializer.Serialize(request, JsonOptions);
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await _http.PostAsync(url, content, ct);
                }
                catch (Exception ex)
                {
                    history.RemoveAt(history.Count - 1);
                    return $"⚠ Could not reach the AI server at {Endpoint}. Is LM Studio running with the Local Server started?\n\n({ex.Message})";
                }

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync(ct);
                    history.RemoveAt(history.Count - 1);
                    return $"⚠ The AI server returned {response.StatusCode}.\n\n{err}";
                }

                var body = await response.Content.ReadAsStringAsync(ct);
                var parsed = JsonSerializer.Deserialize<ChatResponse>(body, JsonOptions);
                var message = parsed?.Choices?.FirstOrDefault()?.Message;

                if (message == null)
                    return "⚠ The AI server returned an empty response.";

                if (message.ToolCalls != null && message.ToolCalls.Count > 0)
                {
                    history.Add(new ReqMsg
                    {
                        Role = "assistant",
                        Content = message.Content,
                        ToolCalls = message.ToolCalls.Select(t => new ReqToolCall
                        {
                            Id = t.Id,
                            Type = t.Type,
                            Function = new ReqFunction { Name = t.Function.Name, Arguments = t.Function.Arguments }
                        }).ToList()
                    });

                    foreach (var call in message.ToolCalls)
                    {
                        var result = await ExecuteToolAsync(call.Function.Name, call.Function.Arguments, ct);
                        onToolUsed?.Invoke($"🔧 {call.Function.Name}");
                        history.Add(new ReqMsg
                        {
                            Role = "tool",
                            ToolCallId = call.Id,
                            Content = result
                        });
                    }

                    continue;
                }

                var final = message.Content ?? "(no response)";
                history.Add(new ReqMsg { Role = "assistant", Content = final });
                return final;
            }

            return "⚠ The agent took too many steps and stopped. Try a simpler request.";
        }

        private async Task<string> ExecuteToolAsync(string name, string arguments, CancellationToken ct)
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments);
            var root = doc.RootElement;

            string Str(string key) =>
                root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : string.Empty;

            switch (name)
            {
                case "list_tasks":
                {
                    var all = await _repo.GetAllAsync(ct);
                    var filter = Str("filter").ToLowerInvariant();
                    var search = Str("search");
                    var q = all.AsEnumerable();
                    if (filter == "active") q = q.Where(t => !t.IsCompleted);
                    else if (filter == "completed") q = q.Where(t => t.IsCompleted);
                    if (!string.IsNullOrWhiteSpace(search))
                        q = q.Where(t => (t.Title ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                                      || (t.Description ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));
                    return JsonSerializer.Serialize(q.Select(t => new
                    {
                        id = t.Id,
                        title = t.Title,
                        priority = t.Priority.ToString(),
                        due = t.DueDate?.ToString("yyyy-MM-dd"),
                        category = t.Category,
                        completed = t.IsCompleted,
                        tags = t.Tags
                    }), JsonOptions);
                }

                case "search_tasks":
                {
                    var query = Str("query");
                    var all = await _repo.GetAllAsync(ct);
                    var matches = all.Where(t =>
                        (t.Title ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (t.Description ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (t.Tags ?? "").Contains(query, StringComparison.OrdinalIgnoreCase));
                    return JsonSerializer.Serialize(matches.Select(t => new
                    {
                        id = t.Id,
                        title = t.Title,
                        priority = t.Priority.ToString(),
                        due = t.DueDate?.ToString("yyyy-MM-dd"),
                        category = t.Category
                    }), JsonOptions);
                }

                case "add_task":
                {
                    var item = new TodoItem
                    {
                        Title = Str("title"),
                        Description = Str("description"),
                        Category = Str("category"),
                        Priority = ParsePriority(Str("priority")),
                        DueDate = ParseDate(Str("due_date") ?? Str("date")),
                        Tags = Str("tags"),
                        Recurrence = ParseRecurrence(Str("recurrence")),
                        SortOrder = -1,
                        CreatedAt = DateTime.Now
                    };
                    if (string.IsNullOrWhiteSpace(item.Title))
                        item.Title = "(no title)";

                    await _repo.AddAsync(item, ct);
                    await _repo.SaveChangesAsync(ct);
                    if (_onTasksChanged != null) await _onTasksChanged();
                    return $"Created task #{item.Id}: {item.Title}";
                }

                case "complete_task":
                {
                    if (!root.TryGetProperty("id", out var idEl) || !idEl.TryGetInt32(out var id))
                        return "Missing or invalid 'id'.";
                    var task = await _repo.GetByIdAsync(id, ct);
                    if (task == null) return $"Task #{id} not found.";
                    task.IsCompleted = true;
                    task.CompletedAt = DateTime.Now;
                    await _repo.UpdateAsync(task, ct);
                    await _repo.SaveChangesAsync(ct);
                    if (_onTasksChanged != null) await _onTasksChanged();
                    return $"Completed task #{id}: {task.Title}";
                }

                case "delete_task":
                {
                    if (!root.TryGetProperty("id", out var idEl) || !idEl.TryGetInt32(out var id))
                        return "Missing or invalid 'id'.";
                    var task = await _repo.GetByIdAsync(id, ct);
                    if (task == null) return $"Task #{id} not found.";
                    await _repo.DeleteAsync(task, ct);
                    await _repo.SaveChangesAsync(ct);
                    if (_onTasksChanged != null) await _onTasksChanged();
                    return $"Deleted task #{id}: {task.Title}";
                }

                default:
                    return $"Unknown tool: {name}";
            }
        }

        private static PriorityLevel ParsePriority(string value) => value?.ToLowerInvariant() switch
        {
            "low" => PriorityLevel.Low,
            "high" => PriorityLevel.High,
            _ => PriorityLevel.Medium
        };

        private static RecurrenceType ParseRecurrence(string value) => value?.ToLowerInvariant() switch
        {
            "daily" => RecurrenceType.Daily,
            "weekly" => RecurrenceType.Weekly,
            "monthly" => RecurrenceType.Monthly,
            _ => RecurrenceType.None
        };

        private static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (DateTime.TryParse(value, out var dt)) return dt;
            return null;
        }

        private static List<ToolDef> BuildTools()
        {
            return new List<ToolDef>
            {
                new()
                {
                    Function = new ToolFunc
                    {
                        Name = "list_tasks",
                        Description = "List existing tasks. Optionally filter by 'active', 'completed', or 'all', and/or search by text.",
                        Parameters = Schema(
                            ("filter", "One of: active, completed, all. Defaults to all.", "string", false),
                            ("search", "Optional text to search in titles and descriptions.", "string", false))
                    }
                },
                new()
                {
                    Function = new ToolFunc
                    {
                        Name = "search_tasks",
                        Description = "Search tasks by a query string and return matching tasks.",
                        Parameters = Schema(
                            ("query", "The search term.", "string", true))
                    }
                },
                new()
                {
                    Function = new ToolFunc
                    {
                        Name = "add_task",
                        Description = "Create a new task in the user's todo list.",
                        Parameters = Schema(
                            ("title", "Short task title.", "string", true),
                            ("description", "Optional longer description.", "string", false),
                            ("category", "Optional category name.", "string", false),
                            ("priority", "One of: low, medium, high. Defaults to medium.", "string", false),
                            ("due_date", "Optional due date as yyyy-MM-dd.", "string", false),
                            ("tags", "Optional comma separated tags.", "string", false),
                            ("recurrence", "Optional: none, daily, weekly, monthly.", "string", false))
                    }
                },
                new()
                {
                    Function = new ToolFunc
                    {
                        Name = "complete_task",
                        Description = "Mark a task as completed by its id.",
                        Parameters = Schema(
                            ("id", "The numeric task id.", "integer", true))
                    }
                },
                new()
                {
                    Function = new ToolFunc
                    {
                        Name = "delete_task",
                        Description = "Delete a task by its id.",
                        Parameters = Schema(
                            ("id", "The numeric task id.", "integer", true))
                    }
                }
            };
        }

        private static JsonNode Schema(params (string name, string desc, string type, bool required)[] props)
        {
            var properties = new JsonObject();
            var required = new JsonArray();
            foreach (var p in props)
            {
                properties[p.name] = new JsonObject
                {
                    ["type"] = p.type,
                    ["description"] = p.desc
                };
                if (p.required) required.Add(p.name);
            }

            var schema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = properties
            };
            if (required.Count > 0)
                schema["required"] = required;

            return schema;
        }

        // ---- Request / response DTOs ----

        public class ChatRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("model")]
            public string Model { get; set; } = "local-model";

            [System.Text.Json.Serialization.JsonPropertyName("messages")]
            public List<ReqMsg> Messages { get; set; } = new();

            [System.Text.Json.Serialization.JsonPropertyName("tools")]
            public List<ToolDef>? Tools { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("temperature")]
            public double Temperature { get; set; } = 0.7;
        }

        public class ReqMsg
        {
            [System.Text.Json.Serialization.JsonPropertyName("role")]
            public string Role { get; set; } = "user";

            [System.Text.Json.Serialization.JsonPropertyName("content")]
            public string? Content { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("tool_calls")]
            public List<ReqToolCall>? ToolCalls { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("tool_call_id")]
            public string? ToolCallId { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string? Name { get; set; }
        }

        public class ReqToolCall
        {
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("type")]
            public string Type { get; set; } = "function";

            [System.Text.Json.Serialization.JsonPropertyName("function")]
            public ReqFunction Function { get; set; } = new();
        }

        public class ReqFunction
        {
            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("arguments")]
            public string Arguments { get; set; } = "{}";
        }

        public class ToolDef
        {
            [System.Text.Json.Serialization.JsonPropertyName("type")]
            public string Type { get; set; } = "function";

            [System.Text.Json.Serialization.JsonPropertyName("function")]
            public ToolFunc Function { get; set; } = new();
        }

        public class ToolFunc
        {
            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("parameters")]
            public JsonNode? Parameters { get; set; }
        }

        public class ChatResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("choices")]
            public List<Choice>? Choices { get; set; }
        }

        public class Choice
        {
            [System.Text.Json.Serialization.JsonPropertyName("message")]
            public ResMsg? Message { get; set; }
        }

        public class ResMsg
        {
            [System.Text.Json.Serialization.JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("content")]
            public string? Content { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("tool_calls")]
            public List<ResToolCall>? ToolCalls { get; set; }
        }

        public class ResToolCall
        {
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("type")]
            public string Type { get; set; } = "function";

            [System.Text.Json.Serialization.JsonPropertyName("function")]
            public ResFunction Function { get; set; } = new();
        }

        public class ResFunction
        {
            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("arguments")]
            public string Arguments { get; set; } = "{}";
        }
    }
}
