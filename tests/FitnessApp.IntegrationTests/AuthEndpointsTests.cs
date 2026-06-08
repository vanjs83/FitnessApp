using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Auth;
using FluentAssertions;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class AuthEndpointsTests : IntegrationTestBase
{
    public AuthEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_then_login_returns_a_jwt_token()
    {
        var client = Factory.CreateClient();
        var email = UniqueEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Test123!",
            fullName = "Ana Trener",
            role = "Trainer"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Test123!" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
        auth.Email.Should().Be(email);
        auth.Role.Should().Be("Trainer");
    }

    [Fact]
    public async Task Register_with_an_existing_email_returns_400()
    {
        var client = Factory.CreateClient();
        var email = UniqueEmail();
        var payload = new { email, password = "Test123!", fullName = "Dup", role = "Client" };

        var first = await client.PostAsJsonAsync("/api/auth/register", payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync("/api/auth/register", payload);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_with_the_wrong_password_returns_401()
    {
        var client = Factory.CreateClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Test123!",
            fullName = "Marko",
            role = "Client"
        });

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPass1!" });
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
