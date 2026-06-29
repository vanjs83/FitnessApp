using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.Features.Auth.Commands;
using FitnessApp.Application.Features.Auth.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Produces("application/json")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    private string PublicBaseUrl() => $"{Request.Scheme}://{Request.Host}";

    /// <summary>Register a new account and return auth tokens.</summary>
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        => HandleResult(await _sender.Send(new RegisterCommand(request.Email, request.FullName, request.Password, request.Role)));

    /// <summary>Log in with email and password.</summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await _sender.Send(new LoginCommand(request.Email, request.Password));
        return result.Code switch
        {
            LoginResultCode.Success => Ok(result.Response),
            LoginResultCode.UserNotFound => Unauthorized(new { message = "No account found with this email address.", code = "user_not_found" }),
            _ => Unauthorized(new { message = "Incorrect password.", code = "wrong_password" })
        };
    }

    /// <summary>Exchange a refresh token for a new access token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request)
    {
        var result = await _sender.Send(new RefreshTokenCommand(request.RefreshToken));
        return result.Ok
            ? Ok(result.Response)
            : Unauthorized(new { message = "Invalid or expired refresh token.", code = "invalid_refresh" });
    }

    /// <summary>Invalidate a refresh token.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(RefreshTokenRequest request)
    {
        await _sender.Send(new LogoutCommand(request.RefreshToken));
        return Ok(new { message = "Logged out." });
    }

    /// <summary>The Google sign-in client id for the front-end.</summary>
    [HttpGet("google-config")]
    [ResponseCache(CacheProfileName = "Reference")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GoogleConfig()
        => Ok(new { clientId = await _sender.Send(new GetGoogleConfigQuery()) });

    /// <summary>Sign in (or up) with a Google credential.</summary>
    [HttpPost("google")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request)
    {
        var result = await _sender.Send(new GoogleLoginCommand(request.Credential, request.Role));
        if (result.Ok) return Ok(result.Response);
        return result.Unauthorized
            ? Unauthorized(new { message = result.Error })
            : BadRequest(new { message = result.Error });
    }

    /// <summary>The current user's account summary.</summary>
    [HttpGet("me")]
    [Authorize]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var me = await _sender.Send(new GetMeQuery());
        return me == null ? NotFound() : Ok(me);
    }

    /// <summary>Update the caller's display name.</summary>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var result = await _sender.Send(new UpdateProfileCommand(request.FullName));
        return result.Succeeded ? Ok(new { fullName = result.Value }) : MapError(result);
    }

    /// <summary>Client disconnects from their current trainer.</summary>
    [HttpPut("trainer")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeTrainer(ChangeTrainerRequest request)
    {
        var result = await _sender.Send(new DisconnectTrainerCommand(request.TrainerId));
        return result.Succeeded ? Ok(new { trainerId = (string?)null }) : MapError(result);
    }

    /// <summary>Change the caller's password.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var result = await _sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword));
        return result.Succeeded ? Ok(new { message = "Password changed successfully." }) : MapError(result);
    }

    /// <summary>Request a password-reset email (always 200 to avoid account enumeration).</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _sender.Send(new ForgotPasswordCommand(request.Email, request.Language, PublicBaseUrl()));
        return Ok(new { message = "If an account exists for this email, a reset link has been sent." });
    }

    /// <summary>Reset a password using a token from the reset email.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var result = await _sender.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword, request.Language));
        return result.Succeeded ? Ok(new { message = "Password has been reset." }) : MapError(result);
    }

    /// <summary>Delete the caller's account.</summary>
    [HttpDelete("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMyAccount()
        => HandleResult(await _sender.Send(new DeleteAccountCommand()));

    /// <summary>The caller's personal/profile details.</summary>
    [HttpGet("personal-profile")]
    [Authorize]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<PersonalProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonalProfileDto>> GetPersonalProfile()
    {
        var profile = await _sender.Send(new GetPersonalProfileQuery());
        return profile == null ? NotFound() : Ok(profile);
    }

    /// <summary>Update the caller's personal/profile details.</summary>
    [HttpPut("personal-profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePersonalProfile(UpdatePersonalProfileRequest request)
    {
        var result = await _sender.Send(new UpdatePersonalProfileCommand(request));
        return result.Succeeded ? Ok(new { message = "Profile saved." }) : MapError(result);
    }

    /// <summary>Upload the caller's profile image.</summary>
    [HttpPost("profile-image")]
    [Authorize]
    [RequestSizeLimit(ProfileImageUpload.MaxImageBytes + 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadProfileImage(IFormFile file)
    {
        var result = await _sender.Send(new UploadProfileImageCommand(file));
        return result.Succeeded ? Ok(new { profileImageUrl = result.Value }) : MapError(result);
    }

    /// <summary>Remove the caller's profile image.</summary>
    [HttpDelete("profile-image")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteProfileImage()
    {
        var result = await _sender.Send(new DeleteProfileImageCommand());
        return result.Succeeded ? Ok(new { message = "Image removed." }) : MapError(result);
    }
}
