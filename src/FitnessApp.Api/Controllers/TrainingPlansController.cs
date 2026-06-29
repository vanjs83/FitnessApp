using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FitnessApp.Application.Common;
using FitnessApp.Application.DTOs.Stats;
using FitnessApp.Application.DTOs.TrainingPlans;
using FitnessApp.Application.Features.TrainingPlans.Commands;
using FitnessApp.Application.Features.TrainingPlans.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/training-plans")]
public class TrainingPlansController : ApiControllerBase
{
    private readonly ISender _sender;

    public TrainingPlansController(ISender sender) => _sender = sender;

    /// <summary>The trainer's training plans (paged).</summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<PagedResult<TrainingPlanListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TrainingPlanListItemDto>>> GetMyPlans([FromQuery] int page = 1)
        => Ok(await _sender.Send(new GetMyTrainingPlansQuery(page)));

    /// <summary>Training plans for a specific client (paged).</summary>
    [HttpGet("client/{clientId}")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<PagedResult<TrainingPlanListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<TrainingPlanListItemDto>>> GetForClient(string clientId, [FromQuery] int page = 1)
        => HandleResult(await _sender.Send(new GetTrainingPlansForClientQuery(clientId, page)));

    /// <summary>A training plan with its days and planned exercises.</summary>
    [HttpGet("{id:int}")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<TrainingPlanDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingPlanDetailDto>> GetById(int id)
        => HandleResult(await _sender.Send(new GetTrainingPlanByIdQuery(id)));

    /// <summary>Create a training plan for a client.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<TrainingPlanDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingPlanDetailDto>> Create(CreateTrainingPlanRequest request)
        => HandleCreated(await _sender.Send(new CreateTrainingPlanCommand(
            request.ClientId, request.Name, request.StartDate, request.EndDate,
            request.TrainerExpectations, request.Price, request.Currency)));

    /// <summary>Update a training plan's header.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateTrainingPlanRequest request)
        => HandleResult(await _sender.Send(new UpdateTrainingPlanCommand(
            id, request.Name, request.StartDate, request.EndDate,
            request.TrainerExpectations, request.Price, request.Currency)));

    /// <summary>Client marks a plan as paid (pending trainer approval).</summary>
    [HttpPost("{id:int}/claim-payment")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType<PaymentStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentStatusResponse>> ClaimPayment(int id)
        => HandleResult(await _sender.Send(new ClaimTrainingPaymentCommand(id)));

    /// <summary>Trainer approves a claimed payment.</summary>
    [HttpPost("{id:int}/approve-payment")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<PaymentStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentStatusResponse>> ApprovePayment(int id)
        => HandleResult(await _sender.Send(new ApproveTrainingPaymentCommand(id)));

    /// <summary>Trainer revokes a previously approved payment.</summary>
    [HttpPost("{id:int}/revoke-approval")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<PaymentStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentStatusResponse>> RevokeApproval(int id)
        => HandleResult(await _sender.Send(new RevokeTrainingApprovalCommand(id)));

    /// <summary>Delete a training plan.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await _sender.Send(new DeleteTrainingPlanCommand(id)));

    // ===== Templates =====

    /// <summary>The trainer's training templates.</summary>
    [HttpGet("templates")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "Reference")]
    [ProducesResponseType<IReadOnlyList<TrainingPlanTemplateListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrainingPlanTemplateListItemDto>>> GetMyTemplates()
        => Ok(await _sender.Send(new GetTrainingTemplatesQuery()));

    /// <summary>Create a training template.</summary>
    [HttpPost("templates")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<TrainingPlanDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TrainingPlanDetailDto>> CreateTemplate(CreateTrainingPlanTemplateRequest request)
        => HandleCreated(await _sender.Send(new CreateTrainingTemplateCommand(request.Name, request.TrainerExpectations)));

    /// <summary>Update a training template's header.</summary>
    [HttpPut("templates/{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(int id, UpdateTrainingPlanTemplateRequest request)
        => HandleResult(await _sender.Send(new UpdateTrainingTemplateCommand(id, request.Name, request.TrainerExpectations)));

    /// <summary>Clone a template into a plan for a client.</summary>
    [HttpPost("templates/{templateId:int}/clone")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<TrainingPlanDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingPlanDetailDto>> CloneTemplateToClient(int templateId, CloneTemplateToClientRequest request)
        => HandleCreated(await _sender.Send(new CloneTrainingTemplateToClientCommand(
            templateId, request.ClientId, request.Name, request.StartDate, request.EndDate,
            request.TrainerExpectations, request.Price, request.Currency)));

    /// <summary>Save an existing plan as a reusable template.</summary>
    [HttpPost("{planId:int}/save-as-template")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<TrainingPlanDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingPlanDetailDto>> SaveAsTemplate(int planId, CreateTrainingPlanTemplateRequest request)
        => HandleCreated(await _sender.Send(new SaveTrainingPlanAsTemplateCommand(planId, request.Name, request.TrainerExpectations)));

    // ===== Days =====

    /// <summary>Add a day to a plan.</summary>
    [HttpPost("{planId:int}/days")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<TrainingDayDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingDayDto>> AddDay(int planId, AddTrainingDayRequest request)
        => HandleCreated(await _sender.Send(new AddTrainingDayCommand(planId, request.DayOfWeek, request.Label, request.Notes)));

    /// <summary>Delete a day.</summary>
    [HttpDelete("days/{dayId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDay(int dayId)
        => HandleResult(await _sender.Send(new DeleteTrainingDayCommand(dayId)));

    // ===== Planned exercises =====

    /// <summary>Add a planned exercise to a day.</summary>
    [HttpPost("days/{dayId:int}/exercises")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<PlannedExerciseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlannedExerciseDto>> AddPlannedExercise(int dayId, AddPlannedExerciseRequest request)
        => HandleCreated(await _sender.Send(new AddPlannedExerciseCommand(
            dayId, request.ExerciseId, request.Order, request.TargetSets, request.TargetReps,
            request.TargetWeightKg, request.TargetDurationSeconds, request.RestSeconds, request.Notes)));

    /// <summary>Reorder a planned exercise within its day.</summary>
    [HttpPut("exercises/{plannedExerciseId:int}/move")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MovePlannedExercise(int plannedExerciseId, [FromQuery] string direction)
        => HandleResult(await _sender.Send(new MovePlannedExerciseCommand(plannedExerciseId, direction)));

    /// <summary>Delete a planned exercise.</summary>
    [HttpDelete("exercises/{plannedExerciseId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlannedExercise(int plannedExerciseId)
        => HandleResult(await _sender.Send(new DeletePlannedExerciseCommand(plannedExerciseId)));

    // ===== Completions =====

    /// <summary>Client toggles today's completion for a whole day.</summary>
    [HttpPost("days/{dayId:int}/toggle-today")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType<CompletionToggleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompletionToggleResponse>> ToggleDayCompletion(int dayId)
        => HandleResult(await _sender.Send(new ToggleDayCompletionCommand(dayId)));

    /// <summary>Client toggles today's completion for one planned exercise.</summary>
    [HttpPost("exercises/{plannedExerciseId:int}/toggle-today")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType<CompletionToggleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompletionToggleResponse>> ToggleTodayCompletion(int plannedExerciseId)
        => HandleResult(await _sender.Send(new ToggleExerciseTodayCompletionCommand(plannedExerciseId)));

    // ===== Performed sets =====

    /// <summary>Sets the client logged against a planned exercise.</summary>
    [HttpGet("exercises/{plannedExerciseId:int}/performed-sets")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<IReadOnlyList<PerformedSetDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PerformedSetDto>>> GetPerformedSets(int plannedExerciseId)
        => HandleResult(await _sender.Send(new GetPerformedSetsQuery(plannedExerciseId)));

    /// <summary>Client logs a performed set.</summary>
    [HttpPost("exercises/{plannedExerciseId:int}/performed-sets")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType<PerformedSetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerformedSetDto>> LogPerformedSet(int plannedExerciseId, LogPerformedSetRequest request)
        => HandleResult(await _sender.Send(new LogPerformedSetCommand(
            plannedExerciseId, request.SetNumber, request.ActualReps, request.ActualWeightKg, request.Notes)));

    /// <summary>Delete a performed set.</summary>
    [HttpDelete("performed-sets/{id:int}")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePerformedSet(int id)
        => HandleResult(await _sender.Send(new DeletePerformedSetCommand(id)));

    // ===== Progression =====

    /// <summary>Per-exercise weight progression for a plan.</summary>
    [HttpGet("{id:int}/weight-progression")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<IReadOnlyList<PlanExerciseProgressionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PlanExerciseProgressionDto>>> GetWeightProgression(int id)
        => HandleResult(await _sender.Send(new GetTrainingPlanWeightProgressionQuery(id)));

    // ===== PDF / sharing =====

    /// <summary>Download the plan as a PDF.</summary>
    [HttpGet("{id:int}/pdf")]
    [ResponseCache(CacheProfileName = "UserData")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var result = await _sender.Send(new GetTrainingPlanPdfQuery(id));
        if (!result.Succeeded) return MapError(result);
        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>Create a public, time-limited share link (with QR) for the plan PDF.</summary>
    [HttpPost("{id:int}/share")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> CreateShareLink(int id)
    {
        var result = await _sender.Send(new CreateTrainingShareTokenCommand(id));
        if (!result.Succeeded) return MapError(result);

        var shareUrl = $"{BuildPublicBaseUrl()}/api/training-plans/share/{result.Value}/pdf";

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(shareUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var pngBytes = qrCode.GetGraphic(20);

        return Ok(new
        {
            url = shareUrl,
            qrPngBase64 = Convert.ToBase64String(pngBytes),
            expiresInHours = 24
        });
    }

    /// <summary>Public download of a shared plan PDF via token.</summary>
    [HttpGet("share/{token}/pdf")]
    [AllowAnonymous]
    [ResponseCache(CacheProfileName = "PublicShare")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadSharedPdf(string token)
    {
        var result = await _sender.Send(new GetSharedTrainingPlanPdfQuery(token));
        if (!result.Succeeded) return MapError(result);
        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    private string BuildPublicBaseUrl()
    {
        var host = Request.Host.Host;
        var port = Request.Host.Port;
        var scheme = Request.Scheme;

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || host == "127.0.0.1"
            || host == "::1"
            || host == "0.0.0.0")
        {
            var lanIp = GetLocalLanIp();
            if (lanIp != null) host = lanIp;
        }

        return port.HasValue ? $"{scheme}://{host}:{port}" : $"{scheme}://{host}";
    }

    private static string? GetLocalLanIp()
    {
        try
        {
            var candidates = new List<string>();
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                var desc = (ni.Description ?? "") + " " + (ni.Name ?? "");
                if (desc.Contains("WSL", StringComparison.OrdinalIgnoreCase)
                    || desc.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase)
                    || desc.Contains("Virtual", StringComparison.OrdinalIgnoreCase)
                    || desc.Contains("VMware", StringComparison.OrdinalIgnoreCase)
                    || desc.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase))
                    continue;
                var ipProps = ni.GetIPProperties();
                if (ipProps.GatewayAddresses.Count == 0) continue;
                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork
                        && !IPAddress.IsLoopback(addr.Address))
                    {
                        candidates.Add(addr.Address.ToString());
                    }
                }
            }

            return candidates
                .OrderBy(ip => ip.StartsWith("192.168.") ? 0
                            : ip.StartsWith("10.") ? 1
                            : 2)
                .FirstOrDefault();
        }
        catch { /* ignore — fall back to caller default */ }
        return null;
    }
}
