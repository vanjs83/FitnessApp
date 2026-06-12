using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FitnessApp.Application.Common;
using FitnessApp.Application.DTOs.Nutrition;
using FitnessApp.Application.Features.Nutrition.Commands;
using FitnessApp.Application.Features.Nutrition.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Route("api/nutrition-plans")]
public class NutritionPlansController : ApiControllerBase
{
    private readonly ISender _sender;

    public NutritionPlansController(ISender sender) => _sender = sender;

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<NutritionPlanListItemDto>>> GetMyPlans()
        => Ok(await _sender.Send(new GetMyNutritionPlansQuery()));

    [HttpGet("client/{clientId}")]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<NutritionPlanListItemDto>>> GetForClient(string clientId)
        => HandleResult(await _sender.Send(new GetNutritionPlansForClientQuery(clientId)));

    [HttpGet("{id:int}")]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<NutritionPlanDetailDto>> GetById(int id)
        => HandleResult(await _sender.Send(new GetNutritionPlanByIdQuery(id)));

    [HttpPost]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<NutritionPlanDetailDto>> Create(CreateNutritionPlanRequest request)
        => HandleResult(await _sender.Send(new CreateNutritionPlanCommand(
            request.ClientId, request.Name, request.StartDate, request.EndDate, request.Notes, request.Price, request.Currency)));

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Update(int id, UpdateNutritionPlanRequest request)
        => HandleResult(await _sender.Send(new UpdateNutritionPlanCommand(
            id, request.Name, request.StartDate, request.EndDate, request.Notes, request.Price, request.Currency)));

    [HttpPost("{id:int}/claim-payment")]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<PaymentStatusResponse>> ClaimPayment(int id)
        => HandleResult(await _sender.Send(new ClaimNutritionPaymentCommand(id)));

    [HttpPost("{id:int}/approve-payment")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<PaymentStatusResponse>> ApprovePayment(int id)
        => HandleResult(await _sender.Send(new ApproveNutritionPaymentCommand(id)));

    [HttpPost("{id:int}/revoke-approval")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<PaymentStatusResponse>> RevokeApproval(int id)
        => HandleResult(await _sender.Send(new RevokeNutritionApprovalCommand(id)));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await _sender.Send(new DeleteNutritionPlanCommand(id)));

    // ===== Templates =====

    [HttpGet("templates")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "Reference")]
    public async Task<ActionResult<IReadOnlyList<NutritionTemplateListItemDto>>> GetTemplates()
        => Ok(await _sender.Send(new GetNutritionTemplatesQuery()));

    [HttpPost("templates")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<NutritionPlanDetailDto>> CreateTemplate(CreateNutritionTemplateRequest request)
        => HandleResult(await _sender.Send(new CreateNutritionTemplateCommand(request.Name, request.Notes)));

    [HttpPut("templates/{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> UpdateTemplate(int id, CreateNutritionTemplateRequest request)
        => HandleResult(await _sender.Send(new UpdateNutritionTemplateCommand(id, request.Name, request.Notes)));

    [HttpPost("templates/{templateId:int}/clone")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<NutritionPlanDetailDto>> CloneTemplate(int templateId, CloneNutritionTemplateRequest request)
        => HandleResult(await _sender.Send(new CloneNutritionTemplateCommand(
            templateId, request.ClientId, request.Name, request.StartDate, request.EndDate, request.Notes, request.Price, request.Currency)));

    [HttpPost("{planId:int}/save-as-template")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<NutritionPlanDetailDto>> SaveAsTemplate(int planId, CreateNutritionTemplateRequest request)
        => HandleResult(await _sender.Send(new SaveNutritionPlanAsTemplateCommand(planId, request.Name, request.Notes)));

    // ===== Days =====

    [HttpPost("{planId:int}/days")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<NutritionDayDto>> AddDay(int planId, AddNutritionDayRequest request)
        => HandleResult(await _sender.Send(new AddNutritionDayCommand(
            planId, request.DayOfWeek, request.Label, request.TotalCaloriesTarget, request.Notes)));

    [HttpDelete("days/{dayId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteDay(int dayId)
        => HandleResult(await _sender.Send(new DeleteNutritionDayCommand(dayId)));

    // ===== Meals =====

    [HttpPost("days/{dayId:int}/meals")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<MealDto>> AddMeal(int dayId, AddMealRequest request)
        => HandleResult(await _sender.Send(new AddMealCommand(dayId, request.MealType, request.Time, request.Notes)));

    [HttpDelete("meals/{mealId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteMeal(int mealId)
        => HandleResult(await _sender.Send(new DeleteMealCommand(mealId)));

    // ===== Meal items =====

    [HttpPost("meals/{mealId:int}/items")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<MealItemDto>> AddMealItem(int mealId, AddMealItemRequest request)
        => HandleResult(await _sender.Send(new AddMealItemCommand(
            mealId, request.Description, request.Quantity, request.Calories, request.ProteinG, request.CarbsG, request.FatG)));

    [HttpDelete("items/{itemId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteMealItem(int itemId)
        => HandleResult(await _sender.Send(new DeleteMealItemCommand(itemId)));

    // ===== PDF / sharing =====

    [HttpGet("{id:int}/pdf")]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var result = await _sender.Send(new GetNutritionPlanPdfQuery(id));
        if (!result.Succeeded) return MapError(result);
        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPost("{id:int}/share")]
    public async Task<ActionResult<object>> CreateShareLink(int id)
    {
        var result = await _sender.Send(new CreateNutritionShareTokenCommand(id));
        if (!result.Succeeded) return MapError(result);

        var shareUrl = $"{BuildPublicBaseUrl()}/api/nutrition-plans/share/{result.Value}/pdf";

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
    [ResponseCache(CacheProfileName = "PublicShare")]
    public async Task<IActionResult> DownloadSharedPdf(string token)
    {
        var result = await _sender.Send(new GetSharedNutritionPlanPdfQuery(token));
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
