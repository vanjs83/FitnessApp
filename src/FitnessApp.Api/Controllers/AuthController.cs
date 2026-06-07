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

[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    private string PublicBaseUrl() => $"{Request.Scheme}://{Request.Host}";

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        => HandleResult(await _sender.Send(new RegisterCommand(request.Email, request.FullName, request.Password, request.Role)));

    [HttpPost("login")]
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

    [HttpGet("google-config")]
    public async Task<ActionResult<object>> GoogleConfig()
        => Ok(new { clientId = await _sender.Send(new GetGoogleConfigQuery()) });

    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request)
    {
        var result = await _sender.Send(new GoogleLoginCommand(request.Credential, request.Role));
        if (result.Ok) return Ok(result.Response);
        return result.Unauthorized
            ? Unauthorized(new { message = result.Error })
            : BadRequest(new { message = result.Error });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var me = await _sender.Send(new GetMeQuery());
        return me == null ? NotFound() : Ok(me);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var result = await _sender.Send(new UpdateProfileCommand(request.FullName));
        return result.Succeeded ? Ok(new { fullName = result.Value }) : MapError(result);
    }

    [HttpPut("trainer")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> ChangeTrainer(ChangeTrainerRequest request)
    {
        var result = await _sender.Send(new DisconnectTrainerCommand(request.TrainerId));
        return result.Succeeded ? Ok(new { trainerId = (string?)null }) : MapError(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var result = await _sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword));
        return result.Succeeded ? Ok(new { message = "Password changed successfully." }) : MapError(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _sender.Send(new ForgotPasswordCommand(request.Email, request.Language, PublicBaseUrl()));
        return Ok(new { message = "If an account exists for this email, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var result = await _sender.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword, request.Language));
        return result.Succeeded ? Ok(new { message = "Password has been reset." }) : MapError(result);
    }

    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> DeleteMyAccount()
        => HandleResult(await _sender.Send(new DeleteAccountCommand()));

    [HttpGet("personal-profile")]
    [Authorize]
    public async Task<ActionResult<PersonalProfileDto>> GetPersonalProfile()
    {
        var profile = await _sender.Send(new GetPersonalProfileQuery());
        return profile == null ? NotFound() : Ok(profile);
    }

    [HttpPut("personal-profile")]
    [Authorize]
    public async Task<IActionResult> UpdatePersonalProfile(UpdatePersonalProfileRequest request)
    {
        var result = await _sender.Send(new UpdatePersonalProfileCommand(request));
        return result.Succeeded ? Ok(new { message = "Profile saved." }) : MapError(result);
    }

    [HttpPost("profile-image")]
    [Authorize]
    [RequestSizeLimit(ProfileImageUpload.MaxImageBytes + 1024)]
    public async Task<IActionResult> UploadProfileImage(IFormFile file)
    {
        var result = await _sender.Send(new UploadProfileImageCommand(file));
        return result.Succeeded ? Ok(new { profileImageUrl = result.Value }) : MapError(result);
    }

    [HttpDelete("profile-image")]
    [Authorize]
    public async Task<IActionResult> DeleteProfileImage()
    {
        var result = await _sender.Send(new DeleteProfileImageCommand());
        return result.Succeeded ? Ok(new { message = "Image removed." }) : MapError(result);
    }
}
