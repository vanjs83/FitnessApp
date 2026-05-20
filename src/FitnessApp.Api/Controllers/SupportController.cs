using System.Security.Claims;
using FitnessApp.Api.Services;
using FitnessApp.Application.DTOs.Support;
using FitnessApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/support")]
public class SupportController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EmailService _email;
    private readonly IConfiguration _config;

    public SupportController(
        UserManager<ApplicationUser> userManager,
        EmailService email,
        IConfiguration config)
    {
        _userManager = userManager;
        _email = email;
        _config = config;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("status")]
    public ActionResult<SupportStatusDto> GetStatus() =>
        Ok(new SupportStatusDto { Configured = _email.IsConfigured && !string.IsNullOrWhiteSpace(SupportEmail) });

    [HttpPost("contact")]
    public async Task<IActionResult> Contact(SupportContactRequest request)
    {
        var supportEmail = SupportEmail;
        if (string.IsNullOrWhiteSpace(supportEmail))
            return BadRequest(new { message = "Customer support is not configured yet." });

        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return BadRequest(new { message = "Your account email is not available." });

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "User";
        var userName = user.FullName ?? user.Email!;
        var lang = (request.Language ?? "hr").ToLowerInvariant();
        var subjectPrefix = lang == "en" ? "[FitnessApp support]" : "[FitnessApp podrška]";

        var (ok, error) = await _email.SendTemplatedAsync(
            toEmail: supportEmail,
            subject: $"{subjectPrefix} {request.Subject}",
            templateKey: "support-message",
            language: lang,
            placeholders: new Dictionary<string, string>
            {
                ["UserName"] = userName,
                ["UserEmail"] = user.Email!,
                ["UserRole"] = role,
                ["Subject"] = request.Subject,
                ["Body"] = request.Body
            },
            replyTo: user.Email,
            replyToName: userName);

        if (!ok)
            return BadRequest(new { message = error ?? "Sending the message failed. Try again later." });

        return Ok(new { sent = true });
    }

    private string? SupportEmail => _config["Smtp:FromEmail"];
}
