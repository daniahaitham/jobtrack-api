using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobTrack.Api.Dtos;
using JobTrack.Api.Models;

namespace JobTrack.Tests;

/// <summary>
/// End-to-end tests: each one sends real HTTP requests to the API (running in memory).
/// IClassFixture = start the API once and share it between the tests in this class.
/// </summary>
public class ApiTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiFactory _factory;

    public ApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // ---- helpers -------------------------------------------------------------

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    /// <summary>Registers a fresh user and returns a client that is already logged in as them.</summary>
    private async Task<HttpClient> CreateLoggedInClientAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = NewEmail(), password = "password123" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private static object NewApplication(string company = "Acme", string role = "Junior Developer") => new
    {
        company,
        role,
        link = "https://example.com/jobs/1",
        status = "Applied",
        notes = "Found on LinkedIn"
    };

    // ---- auth ----------------------------------------------------------------

    [Fact]
    public async Task Register_returns_a_token()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = NewEmail(), password = "password123" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
    }

    [Fact]
    public async Task Register_with_a_taken_email_returns_409()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();

        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "password123" });
        var second = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "password123" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_with_a_short_password_returns_400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = NewEmail(), password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_works_with_the_right_password_and_fails_with_the_wrong_one()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "password123" });

        var good = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "password123" });
        var bad = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
    }

    [Fact]
    public async Task Me_returns_the_logged_in_user()
    {
        var client = await CreateLoggedInClientAsync();
        var me = await client.GetFromJsonAsync<MeResponse>("/api/auth/me", Json);

        Assert.Contains("@example.com", me!.Email);
    }

    // ---- applications ----------------------------------------------------------

    [Fact]
    public async Task Applications_require_a_token()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/applications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_then_list_returns_the_new_application()
    {
        var client = await CreateLoggedInClientAsync();

        var created = await client.PostAsJsonAsync("/api/applications", NewApplication("Zain", "Backend Intern"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await client.GetFromJsonAsync<List<ApplicationResponse>>("/api/applications", Json);
        var item = Assert.Single(list!);
        Assert.Equal("Zain", item.Company);
        Assert.Equal(ApplicationStatus.Applied, item.Status);
    }

    [Fact]
    public async Task Create_without_a_company_returns_400()
    {
        var client = await CreateLoggedInClientAsync();
        var response = await client.PostAsJsonAsync("/api/applications", new { role = "Developer" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_changes_the_status()
    {
        var client = await CreateLoggedInClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/applications", NewApplication()))
            .Content.ReadFromJsonAsync<ApplicationResponse>(Json);

        var update = await client.PutAsJsonAsync($"/api/applications/{created!.Id}", new
        {
            company = "Acme",
            role = "Junior Developer",
            status = "Interview"
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<ApplicationResponse>(Json);
        Assert.Equal(ApplicationStatus.Interview, updated!.Status);
    }

    [Fact]
    public async Task Delete_removes_the_application()
    {
        var client = await CreateLoggedInClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/applications", NewApplication()))
            .Content.ReadFromJsonAsync<ApplicationResponse>(Json);

        var delete = await client.DeleteAsync($"/api/applications/{created!.Id}");
        var afterwards = await client.GetAsync($"/api/applications/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task A_user_cannot_see_or_change_another_users_application()
    {
        var alice = await CreateLoggedInClientAsync();
        var bob = await CreateLoggedInClientAsync();

        var created = await (await alice.PostAsJsonAsync("/api/applications", NewApplication("Alice Corp", "Dev")))
            .Content.ReadFromJsonAsync<ApplicationResponse>(Json);

        var bobGet = await bob.GetAsync($"/api/applications/{created!.Id}");
        var bobDelete = await bob.DeleteAsync($"/api/applications/{created.Id}");
        var bobList = await bob.GetFromJsonAsync<List<ApplicationResponse>>("/api/applications", Json);

        Assert.Equal(HttpStatusCode.NotFound, bobGet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, bobDelete.StatusCode);
        Assert.Empty(bobList!);
    }
}
