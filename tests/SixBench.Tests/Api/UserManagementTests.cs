using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SixBench.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class UserManagementTests(ApiTests.Factory factory) : IClassFixture<ApiTests.Factory>
{
    [Fact]
    public async Task Seeded_admin_signs_in_and_must_change_password()
    {
        var anonymous = factory.CreateClient();
        var wrong = await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { userName = TestAuth.AdminUser, password = "nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal("Sign-in failed", (await wrong.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
        Assert.Equal("application/problem+json", wrong.Content.Headers.ContentType?.MediaType);

        var admin = await factory.SignInAsync();
        var me = await admin.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.Equal("Admin", me.GetProperty("role").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await admin.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Admin_manages_users_and_users_cannot()
    {
        var admin = await factory.SignInAsync();

        // Password policy errors are keyed by field.
        var weak = await admin.PostAsJsonAsync("/api/v1/users", new { userName = "bob", password = "short", role = "User" });
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        Assert.True((await weak.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("password", out _));
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("/api/v1/users", new { userName = "bob", password = "Bob-pass-1!", role = "Owner" })).StatusCode);

        var created = await admin.PostAsJsonAsync("/api/v1/users", new { userName = "bob", password = "Bob-pass-1!", role = "User" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var bobId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("/api/v1/users", new { userName = "bob", password = "Bob-pass-1!", role = "User" })).StatusCode);

        // A User can use the app but not manage users or HTTPS.
        var bob = await factory.SignInAsync("bob", "Bob-pass-1!");
        var bobMe = await bob.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.Equal("User", bobMe.GetProperty("role").GetString());
        Assert.True(bobMe.GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await bob.GetAsync("/api/v1/roku-devices")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.GetAsync("/api/v1/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.GetAsync("/api/v1/certificates/status")).StatusCode);

        // Changing the password clears the flag and keeps the session.
        var badCurrent = await bob.PostAsJsonAsync("/api/v1/auth/me/password", new { currentPassword = "wrong", newPassword = "Bob-pass-2!" });
        Assert.True((await badCurrent.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("currentPassword", out _));
        var changed = await bob.PostAsJsonAsync("/api/v1/auth/me/password", new { currentPassword = "Bob-pass-1!", newPassword = "Bob-pass-2!" });
        changed.EnsureSuccessStatusCode();
        Assert.False((await changed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await bob.GetAsync("/api/v1/auth/me")).StatusCode);

        // Last-admin and self-delete guards.
        var adminId = (await admin.GetFromJsonAsync<JsonElement>("/api/v1/auth/me")).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/v1/users/{adminId}", new { role = "User" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"/api/v1/users/{adminId}")).StatusCode);

        var promoted = await admin.PutAsJsonAsync($"/api/v1/users/{bobId}", new { email = "bob@example.com", role = "Admin" });
        Assert.Equal("Admin", (await promoted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());
        var users = await admin.GetFromJsonAsync<JsonElement>("/api/v1/users");
        Assert.Equal(2, users.EnumerateArray().Count(u => u.GetProperty("role").GetString() == "Admin"));

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/users/{bobId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/users/{bobId}")).StatusCode);
    }

    [Fact]
    public async Task Lockout_after_failures_is_cleared_by_password_reset()
    {
        var admin = await factory.SignInAsync();
        var created = await admin.PostAsJsonAsync("/api/v1/users", new { userName = "carol", password = "Carol-pass-1!", role = "User" });
        var carolId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var anonymous = factory.CreateClient();
        for (var i = 0; i < 5; i++)
        {
            await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { userName = "carol", password = "wrong" });
        }

        var locked = await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { userName = "carol", password = "Carol-pass-1!" });
        Assert.Equal("Account locked", (await locked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());

        var reset = await admin.PostAsJsonAsync($"/api/v1/users/{carolId}/password", new { newPassword = "Carol-pass-9!" });
        Assert.False((await reset.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("isLockedOut").GetBoolean());
        await factory.SignInAsync("carol", "Carol-pass-9!");
    }
}
