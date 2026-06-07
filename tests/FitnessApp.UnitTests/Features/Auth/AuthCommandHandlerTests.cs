using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.Features.Auth.Commands;
using Moq;
using Xunit;

namespace FitnessApp.UnitTests.Features.Auth;

public class AuthCommandHandlerTests
{
    private readonly Mock<IAuthService> _auth = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private static readonly IReadOnlyList<string> NoErrors = Array.Empty<string>();

    public AuthCommandHandlerTests()
    {
        _currentUser.SetupGet(x => x.UserId).Returns("user-1");
    }

    // ── Register ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_InvalidRole_FailsValidation_WithoutCallingService()
    {
        var handler = new RegisterCommandHandler(_auth.Object);

        var result = await handler.Handle(
            new RegisterCommand("a@b.com", "Test User", "Test123!", "SuperAdmin"), default);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultError.Validation, result.Error);
        _auth.Verify(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _auth.Verify(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_DuplicateEmail_FailsValidation()
    {
        _auth.Setup(x => x.EmailExistsAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new RegisterCommandHandler(_auth.Object);

        var result = await handler.Handle(new RegisterCommand("a@b.com", null, "Test123!", "Client"), default);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultError.Validation, result.Error);
        _auth.Verify(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_Valid_ReturnsAuthResponse()
    {
        var response = new AuthResponse { Token = "jwt", Email = "a@b.com", Role = "Client" };
        _auth.Setup(x => x.EmailExistsAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _auth.Setup(x => x.RegisterAsync("a@b.com", "Test User", "Test123!", "Client", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, NoErrors, response));
        var handler = new RegisterCommandHandler(_auth.Object);

        var result = await handler.Handle(new RegisterCommand("a@b.com", "Test User", "Test123!", "Client"), default);

        Assert.True(result.Succeeded);
        Assert.Same(response, result.Value);
    }

    [Fact]
    public async Task Register_IdentityFailure_FailsValidation()
    {
        _auth.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _auth.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, new[] { "Password too weak" }, (AuthResponse?)null));
        var handler = new RegisterCommandHandler(_auth.Object);

        var result = await handler.Handle(new RegisterCommand("a@b.com", null, "weak", "Client"), default);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultError.Validation, result.Error);
        Assert.Contains("Password too weak", result.Message);
    }

    // ── Login ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_Success_ReturnsResponse()
    {
        var response = new AuthResponse { Token = "jwt", Email = "a@b.com", Role = "Client" };
        _auth.Setup(x => x.LoginAsync("a@b.com", "Test123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginResultCode.Success, response));
        var handler = new LoginCommandHandler(_auth.Object);

        var result = await handler.Handle(new LoginCommand("a@b.com", "Test123!"), default);

        Assert.Equal(LoginResultCode.Success, result.Code);
        Assert.Same(response, result.Response);
    }

    [Theory]
    [InlineData(LoginResultCode.UserNotFound)]
    [InlineData(LoginResultCode.WrongPassword)]
    public async Task Login_Failure_PropagatesCodeWithNoResponse(LoginResultCode code)
    {
        _auth.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((code, (AuthResponse?)null));
        var handler = new LoginCommandHandler(_auth.Object);

        var result = await handler.Handle(new LoginCommand("x@y.com", "nope"), default);

        Assert.Equal(code, result.Code);
        Assert.Null(result.Response);
    }

    // ── ChangePassword ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_Success_ReturnsSuccess()
    {
        _auth.Setup(x => x.ChangePasswordAsync("user-1", "old", "new", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, NoErrors));
        var handler = new ChangePasswordCommandHandler(_auth.Object, _currentUser.Object);

        var result = await handler.Handle(new ChangePasswordCommand("old", "new"), default);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ChangePassword_Failure_FailsValidationWithMessage()
    {
        _auth.Setup(x => x.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, new[] { "Incorrect password." }));
        var handler = new ChangePasswordCommandHandler(_auth.Object, _currentUser.Object);

        var result = await handler.Handle(new ChangePasswordCommand("bad", "new"), default);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultError.Validation, result.Error);
        Assert.Contains("Incorrect password.", result.Message);
    }

    // ── DisconnectTrainer ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DisconnectTrainer_WithTrainerId_IsRejected()
    {
        var handler = new DisconnectTrainerCommandHandler(_auth.Object, _currentUser.Object);

        var result = await handler.Handle(new DisconnectTrainerCommand("trainer-9"), default);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultError.Validation, result.Error);
        _auth.Verify(x => x.DisconnectTrainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DisconnectTrainer_WithoutTrainerId_Disconnects()
    {
        _auth.Setup(x => x.DisconnectTrainerAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((true, NoErrors));
        var handler = new DisconnectTrainerCommandHandler(_auth.Object, _currentUser.Object);

        var result = await handler.Handle(new DisconnectTrainerCommand(null), default);

        Assert.True(result.Succeeded);
        _auth.Verify(x => x.DisconnectTrainerAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── DeleteAccount ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(DeleteAccountResultCode.Success, true, ResultError.None)]
    [InlineData(DeleteAccountResultCode.NotFound, false, ResultError.NotFound)]
    [InlineData(DeleteAccountResultCode.IsAdmin, false, ResultError.Validation)]
    public async Task DeleteAccount_MapsOutcomeToResult(DeleteAccountResultCode outcome, bool succeeded, ResultError error)
    {
        _auth.Setup(x => x.DeleteAccountAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(outcome);
        var handler = new DeleteAccountCommandHandler(_auth.Object, _currentUser.Object);

        var result = await handler.Handle(new DeleteAccountCommand(), default);

        Assert.Equal(succeeded, result.Succeeded);
        Assert.Equal(error, result.Error);
    }
}
