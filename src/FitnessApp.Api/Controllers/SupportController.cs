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
    private readonly EmailTemplateService _emailTemplates;
    private readonly IConfiguration _config;
    private readonly ILogger<SupportController> _logger;

    public SupportController(
        UserManager<ApplicationUser> userManager,
        EmailService email,
        EmailTemplateService emailTemplates,
        IConfiguration config,
        ILogger<SupportController> logger)
    {
        _userManager = userManager;
        _email = email;
        _emailTemplates = emailTemplates;
        _config = config;
        _logger = logger;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("status")]
    public ActionResult<SupportStatusDto> GetStatus() =>
        Ok(new SupportStatusDto { Configured = _email.IsConfigured && !string.IsNullOrWhiteSpace(SupportEmail) });

    [HttpPost("contact")]
    public async Task<IActionResult> Contact(SupportContactRequest request)
    {
        if (!_email.IsConfigured)
            return BadRequest(new { message = "SMTP is not configured." });

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

        string html;
        try
        {
            html = _emailTemplates.Render("support-message", lang, new Dictionary<string, string>
            {
                ["UserName"] = userName,
                ["UserEmail"] = user.Email!,
                ["UserRole"] = role,
                ["Subject"] = request.Subject,
                ["Body"] = request.Body
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render support-message email template (lang={Lang}).", lang);
            return BadRequest(new { message = "Failed to prepare the message." });
        }

        var subjectPrefix = lang == "en" ? "[FitnessApp support]" : "[FitnessApp podrška]";

        try
        {
            await _email.SendAsync(
                toEmail: supportEmail,
                subject: $"{subjectPrefix} {request.Subject}",
                body: html,
                replyTo: user.Email,
                replyToName: userName);
            return Ok(new { sent = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send support message from {UserEmail}.", user.Email);
            return BadRequest(new { message = "Sending the message failed. Try again later." });
        }
    }

    private string? SupportEmail => _config["Smtp:FromEmail"];
}
