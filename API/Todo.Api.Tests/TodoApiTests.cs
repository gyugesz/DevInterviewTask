using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Todo.Application.Contracts;

namespace Todo.Api.Tests;

public class TodoApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    public TodoApiTests(ApiFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task Create_List_Done_Flow_Works()
    {
        var create = new CreateTodoRequest("Pro teszt", "leírás", 2);
        var createResp = await _client.PostAsJsonAsync("/api/todo", create);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var listed = await _client.GetFromJsonAsync<List<TodoResponse>>("/api/todo?undoneOnly=true");
        listed.Should().NotBeNull().And.OnlyContain(x => x.IsDone == false);
        listed!.Any(x => x.Name == "Pro teszt").Should().BeTrue();

        var id = listed!.First(x => x.Name == "Pro teszt").Id;

        var doneResp = await _client.PatchAsync($"/api/todo/{id}/done", null);
        doneResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        
        var listed2 = await _client.GetFromJsonAsync<List<TodoResponse>>("/api/todo?undoneOnly=true");
        listed2!.Any(x => x.Id == id).Should().BeFalse();
    }

    [Fact]
    public async Task Create_Invalid_Returns_400_With_ProblemDetails()
    {
        var bad = new { name = "", description = new string('x', 300), priority = 9 };
        var resp = await _client.PostAsJsonAsync("/api/todo", bad);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        problem.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrPatch_UnknownId_Returns_404()
    {
        var getResp = await _client.GetAsync("/api/todo/99999");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var patchResp = await _client.PatchAsync("/api/todo/99999/done", null);
        patchResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_Sorted_By_Priority_Desc_Then_Date_Asc()
    {
        
        await _client.PostAsJsonAsync("/api/todo", new CreateTodoRequest("A", null, 2));
        await Task.Delay(50); // hogy createdAt tényleg különbözzön
        await _client.PostAsJsonAsync("/api/todo", new CreateTodoRequest("B", null, 3));
        await Task.Delay(50);
        await _client.PostAsJsonAsync("/api/todo", new CreateTodoRequest("C", null, 2));

        var list = await _client.GetFromJsonAsync<List<TodoResponse>>("/api/todo");
        list!.Should().NotBeEmpty();

        // Elvárt: priority 3 (B) az elsõ, utána priority 2-k idõrendben (A, majd C)
        var names = list!.Select(x => x.Name).ToList();
        names.Should().ContainInOrder("B", "A", "C");
    }
}
