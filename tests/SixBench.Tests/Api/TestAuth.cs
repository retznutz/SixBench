using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SixBench.Tests.Api;

/// <summary>Seeds a known administrator and signs test clients in.</summary>
internal static class TestAuth
{
    public const string AdminUser = "admin";
    public const string AdminPassword = "Test-Admin-1!";

    public static void Configure(IWebHostBuilder builder, string tempRoot)
    {
        builder.UseSetting("Identity:SeedAdmin:UserName", AdminUser);
        builder.UseSetting("Identity:SeedAdmin:Password", AdminPassword);
        builder.UseSetting("DataProtection:KeysPath", Path.Combine(tempRoot, "keys"));
    }

    public static async Task<HttpClient> SignInAsync(this WebApplicationFactory<Program> factory, string user = AdminUser, string password = AdminPassword)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { userName = user, password });
        response.EnsureSuccessStatusCode();
        return client;
    }
}
