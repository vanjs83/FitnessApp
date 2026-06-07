using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.Features.Auth.Queries;
using Moq;
using Xunit;

namespace FitnessApp.UnitTests.Features.Auth;

public class AuthQueryHandlerTests
{
    private readonly Mock<IAuthService> _auth = new();
    private readonly Mock<IUserProfileService> _profiles = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    public AuthQueryHandlerTests()
    {
        _currentUser.SetupGet(x => x.UserId).Returns("user-1");
    }

    [Fact]
    public async Task GetGoogleConfig_ReturnsConfiguredClientId()
    {
        _auth.SetupGet(x => x.GoogleClientId).Returns("client-123.apps.googleusercontent.com");
        var handler = new GetGoogleConfigQueryHandler(_auth.Object);

        var result = await handler.Handle(new GetGoogleConfigQuery(), default);

        Assert.Equal("client-123.apps.googleusercontent.com", result);
    }

    [Fact]
    public async Task GetMe_ReturnsCurrentUserSummary()
    {
        var me = new MeResponse { Id = "user-1", Email = "a@b.com", Role = "Client" };
        _auth.Setup(x => x.GetMeAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(me);
        var handler = new GetMeQueryHandler(_auth.Object, _currentUser.Object);

        var result = await handler.Handle(new GetMeQuery(), default);

        Assert.Same(me, result);
    }

    [Fact]
    public async Task GetMe_UnknownUser_ReturnsNull()
    {
        _auth.Setup(x => x.GetMeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((MeResponse?)null);
        var handler = new GetMeQueryHandler(_auth.Object, _currentUser.Object);

        var result = await handler.Handle(new GetMeQuery(), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPersonalProfile_ReturnsProfileForCurrentUser()
    {
        var profile = new PersonalProfileDto { Email = "a@b.com", Role = "Client" };
        _profiles.Setup(x => x.GetProfileAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        var handler = new GetPersonalProfileQueryHandler(_profiles.Object, _currentUser.Object);

        var result = await handler.Handle(new GetPersonalProfileQuery(), default);

        Assert.Same(profile, result);
    }
}
