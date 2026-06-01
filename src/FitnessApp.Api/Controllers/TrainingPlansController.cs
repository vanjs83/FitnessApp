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
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Route("api/training-plans")]
public class TrainingPlansController : ApiControllerBase
{
    private readonly ISender _sender;

    public TrainingPlansController(ISender sender) => _sender = sender;

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IReadOnlyList<TrainingPlanListItemDto>>> GetMyPlans()
        => Ok(await _sender.Send(new GetMyTrainingPlansQuery()));

    [HttpGet("client/{clientId}")]
    public async Task<ActionResult<IReadOnlyList<TrainingPlanListItemDto>>> GetForClient(string clientId)
        => HandleResult(await _sender.Send(new GetTrainingPlansForClientQuery(clientId)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TrainingPlanDetailDto>> GetById(int id)
        => HandleResult(await _sender.Send(new GetTrainingPlanByIdQuery(id)));

    [HttpPost]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<TrainingPlanDetailDto>> Create(CreateTrainingPlanRequest request)
        => HandleResult(await _sender.Send(new CreateTrainingPlanCommand(
            request.ClientId, request.Name, request.StartDate, request.EndDate,
            request.TrainerExpectations, request.Price, request.Currency)));

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Update(int id, UpdateTrainingPlanRequest request)
        => HandleResult(await _sender.Send(new UpdateTrainingPlanCommand(
            id, request.Name, request.StartDate, request.EndDate,
            request.TrainerExpectations, request.Price, request.Currency)));

    [HttpPost("{id:int}/claim-payment")]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<PaymentStatusResponse>> ClaimPayment(int id)
        => HandleResult(await _sender.Send(new ClaimTrainingPaymentCommand(id)));

    [HttpPost("{id:int}/approve-payment")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<PaymentStatusResponse>> ApprovePayment(int id)
        => HandleResult(await _sender.Send(new ApproveTrainingPaymentCommand(id)));

    [HttpPost("{id:int}/revoke-approval")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<PaymentStatusResponse>> RevokeApproval(int id)
        => HandleResult(await _sender.Send(new RevokeTrainingApprovalCommand(id)));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await _sender.Send(new DeleteTrainingPlanCommand(id)));

    // ===== Templates =====

    [HttpGet("templates")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IReadOnlyList<TrainingPlanTemplateListItemDto>>> GetMyTemplates()
        => Ok(await _sender.Send(new GetTrainingTemplatesQuery()));

    [HttpPost("templates")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<TrainingPlanDetailDto>> CreateTemplate(CreateTrainingPlanTemplateRequest request)
        => HandleResult(await _sender.Send(new CreateTrainingTemplateCommand(request.Name, request.TrainerExpectations)));

    [HttpPut("templates/{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> UpdateTemplate(int id, UpdateTrainingPlanTemplateRequest request)
        => HandleResult(await _sender.Send(new UpdateTrainingTemplateCommand(id, request.Name, request.TrainerExpectations)));

    [HttpPost("templates/{templateId:int}/clone")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<TrainingPlanDetailDto>> CloneTemplateToClient(int templateId, CloneTemplateToClientRequest request)
        => HandleResult(await _sender.Send(new CloneTrainingTemplateToClientCommand(
            templateId, request.ClientId, request.Name, request.StartDate, request.EndDate,
            request.TrainerExpectations, request.Price, request.Currency)));

    [HttpPost("{planId:int}/save-as-template")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<TrainingPlanDetailDto>> SaveAsTemplate(int planId, CreateTrainingPlanTemplateRequest request)
        => HandleResult(await _sender.Send(new SaveTrainingPlanAsTemplateCommand(planId, request.Name, request.TrainerExpectations)));

    // ===== Days =====

    [HttpPost("{planId:int}/days")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<TrainingDayDto>> AddDay(int planId, AddTrainingDayRequest request)
        => HandleResult(await _sender.Send(new AddTrainingDayCommand(planId, request.DayOfWeek, request.Label, request.Notes)));

    [HttpDelete("days/{dayId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteDay(int dayId)
        => HandleResult(await _sender.Send(new DeleteTrainingDayCommand(dayId)));

    // ===== Planned exercises =====

    [HttpPost("days/{dayId:int}/exercises")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<PlannedExerciseDto>> AddPlannedExercise(int dayId, AddPlannedExerciseRequest request)
        => HandleResult(await _sender.Send(new AddPlannedExerciseCommand(
            dayId, request.ExerciseId, request.Order, request.TargetSets, request.TargetReps,
            request.TargetWeightKg, request.TargetDurationSeconds, request.RestSeconds, request.Notes)));

    [HttpPut("exercises/{plannedExerciseId:int}/move")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> MovePlannedExercise(int plannedExerciseId, [FromQuery] string direction)
        => HandleResult(await _sender.Send(new MovePlannedExerciseCommand(plannedExerciseId, direction)));

    [HttpDelete("exercises/{plannedExerciseId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeletePlannedExercise(int plannedExerciseId)
        => HandleResult(await _sender.Send(new DeletePlannedExerciseCommand(plannedExerciseId)));

    // ===== Completions =====

    [HttpPost("days/{dayId:int}/toggle-today")]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<CompletionToggleResponse>> ToggleDayCompletion(int dayId)
        => HandleResult(await _sender.Send(new ToggleDayCompletionCommand(dayId)));

    [HttpPost("exercises/{plannedExerciseId:int}/toggle-today")]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<CompletionToggleResponse>> ToggleTodayCompletion(int plannedExerciseId)
        => HandleResult(await _sender.Send(new ToggleExerciseTodayCompletionCommand(plannedExerciseId)));

    // ===== Performed sets =====

    [HttpGet("exercises/{plannedExerciseId:int}/performed-sets")]
    public async Task<ActionResult<IReadOnlyList<PerformedSetDto>>> GetPerformedSets(int plannedExerciseId)
        => HandleResult(await _sender.Send(new GetPerformedSetsQuery(plannedExerciseId)));

    [HttpPost("exercises/{plannedExerciseId:int}/performed-sets")]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<PerformedSetDto>> LogPerformedSet(int plannedExerciseId, LogPerformedSetRequest request)
        => HandleResult(await _sender.Send(new LogPerformedSetCommand(
            plannedExerciseId, request.SetNumber, request.ActualReps, request.ActualWeightKg, request.Notes)));

    [HttpDelete("performed-sets/{id:int}")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> DeletePerformedSet(int id)
        => HandleResult(await _sender.Send(new DeletePerformedSetCommand(id)));

    // ===== Progression =====

    [HttpGet("{id:int}/weight-progression")]
    public async Task<ActionResult<IReadOnlyList<PlanExerciseProgressionDto>>> GetWeightProgression(int id)
        => HandleResult(await _sender.Send(new GetTrainingPlanWeightProgressionQuery(id)));

    // ===== PDF / sharing =====

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var result = await _sender.Send(new GetTrainingPlanPdfQuery(id));
        if (!result.Succeeded) return MapError(result);
        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPost("{id:int}/share")]
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

    [HttpGet("share/{token}/pdf")]
    [AllowAnonymous]
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
