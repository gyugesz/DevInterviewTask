using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TodoWpfClient.Models;

namespace TodoWpfClient.Services
{
    public class TodoApiClient
    {
        private readonly HttpClient _http = new HttpClient();
        public string BaseUrl { get; private set; }

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public TodoApiClient(string baseUrl)
        {
            BaseUrl = baseUrl.TrimEnd('/');
            _http.Timeout = TimeSpan.FromSeconds(15);
        }

        public void UpdateBaseUrl(string baseUrl) => BaseUrl = baseUrl.TrimEnd('/');

        private Uri U(string path) => new Uri($"{BaseUrl}{(path.StartsWith("/") ? path : "/" + path)}");

        public async Task<List<TodoItem>> GetTodosAsync()
        {
            // A te API-d: /api/Todo
            var routes = new[] { "/api/Todo", "/todos" };
            foreach (var route in routes)
            {
                try
                {
                    var resp = await _http.GetAsync(U(route));
                    if (!resp.IsSuccessStatusCode) continue;

                    var json = await resp.Content.ReadAsStringAsync();
                    var list = ParseTodos(json);
                    return list; // lehet üres is – ez OK
                }
                catch { /* próbáljuk a következőt */ }
            }

            throw new Exception("Nem sikerült lekérdezni a teendőket. Ellenőrizd az API Base URL-t és az elérési utat (/api/Todo).");
        }

        public async Task<TodoItem> CreateTodoAsync(string name, string? description, int priority)
        {
            // Mindig a teljes modellt küldjük az API-nak
            var payload = new
            {
                Name = name,
                Description = description,
                Priority = priority
            };

            var resp = await _http.PostAsJsonAsync(U("/api/Todo"), payload, _jsonOptions);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                throw new Exception($"POST /api/Todo -> {(int)resp.StatusCode} {resp.ReasonPhrase}. Válasz: {body}");
            }

            var json = await resp.Content.ReadAsStringAsync();
            var created = ParseTodo(json);

            // Optimista visszatöltés, ha az API nem küld vissza mindent
            return created ?? new TodoItem
            {
                Title = name,
                Description = description ?? "",
                Priority = priority
            };
        }

        public async Task<bool> CompleteTodoAsync(TodoItem item)
        {
            // A Swaggered szerint PATCH /api/Todo/{id}/done
            var routes = new[] { "/api/Todo", "/todos" };

            // 0) PATCH /{id}/done
            foreach (var baseRoute in routes)
            {
                try
                {
                    var req = new HttpRequestMessage(new HttpMethod("PATCH"), U($"{baseRoute}/{item.Id}/done"));
                    var resp = await _http.SendAsync(req);
                    if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 300) return true;
                }
                catch { /* next */ }
            }

            // 1) PUT /{id}/complete
            foreach (var baseRoute in routes)
            {
                try
                {
                    var resp = await _http.PutAsync(U($"{baseRoute}/{item.Id}/complete"), null);
                    if (resp.IsSuccessStatusCode) return true;
                }
                catch { /* next */ }
            }

            // 2) PATCH /{id}  isCompleted=true
            var patch = new StringContent(JsonSerializer.Serialize(new { isCompleted = true }), Encoding.UTF8, "application/json");
            foreach (var baseRoute in routes)
            {
                try
                {
                    var req = new HttpRequestMessage(new HttpMethod("PATCH"), U($"{baseRoute}/{item.Id}")) { Content = patch };
                    var resp = await _http.SendAsync(req);
                    if (resp.IsSuccessStatusCode) return true;
                }
                catch { /* next */ }
            }

            // 3) PUT teljes entitás
            var full = JsonSerializer.Serialize(new { id = item.Id, title = item.Title, isCompleted = true });
            foreach (var baseRoute in routes)
            {
                try
                {
                    var resp = await _http.PutAsync(U($"{baseRoute}/{item.Id}"),
                        new StringContent(full, Encoding.UTF8, "application/json"));
                    if (resp.IsSuccessStatusCode) return true;
                }
                catch { /* next */ }
            }

            return false;
        }

        // ---------- JSON segédek (case-insensitive property-olvasás) ----------

        private static bool TryGetPropertyIgnoreCase(JsonElement el, string name, out JsonElement value)
        {
            foreach (var p in el.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        private static string? TryGetString(JsonElement el, params string[] names)
        {
            foreach (var n in names)
            {
                if (TryGetPropertyIgnoreCase(el, n, out var v))
                {
                    if (v.ValueKind == JsonValueKind.String) return v.GetString();
                    if (v.ValueKind == JsonValueKind.Number) return v.ToString();
                    if (v.ValueKind == JsonValueKind.True) return "true";
                    if (v.ValueKind == JsonValueKind.False) return "false";
                    // Guid-et gyakran stringként kapjuk; ha object, ToString()
                    return v.ToString();
                }
            }
            return null;
        }

        private static bool? TryGetBool(JsonElement el, params string[] names)
        {
            foreach (var n in names)
            {
                if (TryGetPropertyIgnoreCase(el, n, out var v))
                {
                    if (v.ValueKind == JsonValueKind.True) return true;
                    if (v.ValueKind == JsonValueKind.False) return false;
                    if (v.ValueKind == JsonValueKind.String && bool.TryParse(v.GetString(), out var b)) return b;
                    if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i != 0;
                }
            }
            return null;
        }

        private static DateTime? TryGetDateTime(JsonElement el, params string[] names)
        {
            foreach (var n in names)
            {
                if (TryGetPropertyIgnoreCase(el, n, out var v))
                {
                    if (v.ValueKind == JsonValueKind.String &&
                        DateTime.TryParse(v.GetString(), out var dt)) return dt;

                    // epoch másodperc támogatás
                    if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var epoch))
                        return DateTimeOffset.FromUnixTimeSeconds(epoch).LocalDateTime;
                }
            }
            return null;
        }

        // ---------- Parserek ----------

        private List<TodoItem> ParseTodos(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Ha az API egy objektumba csomagolja a listát (pl. { items: [...] })
                if (root.ValueKind == JsonValueKind.Object &&
                    TryGetPropertyIgnoreCase(root, "items", out var items))
                {
                    root = items;
                }

                if (root.ValueKind == JsonValueKind.Array)
                {
                    return root.EnumerateArray()
                               .Select(ParseTodo)
                               .Where(t => t != null)
                               .Select(t => t!)
                               .ToList();
                }

                // Ha egyetlen elem érkezik
                var single = ParseTodo(root);
                if (single != null) return new List<TodoItem> { single };
            }
            catch { /* lenyeljük – visszaadunk üres listát */ }

            return new List<TodoItem>();
        }

        private TodoItem? ParseTodo(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return ParseTodo(doc.RootElement);
            }
            catch { return null; }
        }

        // TodoApiClient.cs

        private TodoItem? ParseTodo(JsonElement el)
        {
            string id =
                TryGetString(el, "id") ?? TryGetString(el, "todoId") ?? TryGetString(el, "guid") ?? "";

            string title =
                TryGetString(el, "title") ?? TryGetString(el, "name") ?? TryGetString(el, "text") ?? "";

            string description =
                TryGetString(el, "description", "desc", "details", "note") ?? "";

            bool isCompleted =
                TryGetBool(el, "isCompleted", "completed", "done") ?? false;

            int priority =
                TryGetInt(el, "priority", "prio") ?? 1;

            DateTime created =
                TryGetDateTime(el, "createdAt", "created") ?? DateTime.MinValue;

            return new TodoItem
            {
                Id = id,
                Title = title,
                Description = description,
                Priority = priority,
                IsCompleted = isCompleted,
                CreatedAt = created
            };
        }

        private static int? TryGetInt(JsonElement el, params string[] names)
        {
            foreach (var n in names)
            {
                if (el.TryGetProperty(n, out var v))
                {
                    if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
                    if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var j)) return j;
                }
            }
            return null;
        }
    }
}
