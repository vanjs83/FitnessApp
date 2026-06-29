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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/nutrition-plans")]
public class NutritionPlansController : ApiControllerBase
{
    private readonly ISender _sender;

    public NutritionPlansController(ISender sender) => _sender = sender;

    /// <summary>The trainer's nutrition plans (paged).</summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<PagedResult<NutritionPlanListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NutritionPlanListItemDto>>> GetMyPlans([FromQuery] int page = 1)
        => Ok(await _sender.Send(new GetMyNutritionPlansQuery(page)));

    /// <summary>Nutrition plans for a specific client (paged).</summary>
    [HttpGet("client/{clientId}")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<PagedResult<NutritionPlanListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<NutritionPlanListItemDto>>> GetForClient(string clientId, [FromQuery] int page = 1)
        => HandleResult(await _sender.Send(new GetNutritionPlansForClientQuery(clientId, page)));

    /// <summary>A nutrition plan with its days, meals and items.</summary>
    [HttpGet("{id:int}")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<NutritionPlanDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NutritionPlanDetailDto>> GetById(int id)
        => HandleResult(await _sender.Send(new GetNutritionPlanByIdQuery(id)));

    /// <summary>Create a nutrition plan for a client.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<NutritionPlanDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NutritionPlanDetailDto>> Create(CreateNutritionPlanRequest request)
        => HandleCreated(await _sender.Send(new CreateNutritionPlanCommand(
            request.ClientId, request.Name, request.StartDate, request.EndDate, request.Notes, request.Price, request.Currency)));

    /// <summary>Update a nutrition plan's header.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateNutritionPlanRequest request)
        => HandleResult(await _sender.Send(new UpdateNutritionPlanCommand(
            id, request.Name, request.StartDate, request.EndDate, request.Notes, request.Price, request.Currency)));

    /// <summary>Client marks a plan as paid (pending trainer approval).</summary>
    [HttpPost("{id:int}/claim-payment")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType<PaymentStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentStatusResponse>> ClaimPayment(int id)
        => HandleResult(await _sender.Send(new ClaimNutritionPaymentCommand(id)));

    /// <summary>Trainer approves a claimed payment.</summary>
    [HttpPost("{id:int}/approve-payment")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<PaymentStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentStatusResponse>> ApprovePayment(int id)
        => HandleResult(await _sender.Send(new ApproveNutritionPaymentCommand(id)));

    /// <summary>Trainer revokes a previously approved payment.</summary>
    [HttpPost("{id:int}/revoke-approval")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<PaymentStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentStatusResponse>> RevokeApproval(int id)
        => HandleResult(await _sender.Send(new RevokeNutritionApprovalCommand(id)));

    /// <summary>Delete a nutrition plan.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await _sender.Send(new DeleteNutritionPlanCommand(id)));

    // ===== Templates =====

    /// <summary>The trainer's nutrition templates.</summary>
    [HttpGet("templates")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "Reference")]
    [ProducesResponseType<IReadOnlyList<NutritionTemplateListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NutritionTemplateListItemDto>>> GetTemplates()
        => Ok(await _sender.Send(new GetNutritionTemplatesQuery()));

    /// <summary>Create a nutrition template.</summary>
    [HttpPost("templates")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<NutritionPlanDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NutritionPlanDetailDto>> CreateTemplate(CreateNutritionTemplateRequest request)
        => HandleCreated(await _sender.Send(new CreateNutritionTemplateCommand(request.Name, request.Notes)));

    /// <summary>Update a nutrition template's header.</summary>
    [HttpPut("templates/{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(int id, CreateNutritionTemplateRequest request)
        => HandleResult(await _sender.Send(new UpdateNutritionTemplateCommand(id, request.Name, request.Notes)));

    /// <summary>Clone a template into a plan for a client.</summary>
    [HttpPost("templates/{templateId:int}/clone")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<NutritionPlanDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NutritionPlanDetailDto>> CloneTemplate(int templateId, CloneNutritionTemplateRequest request)
        => HandleCreated(await _sender.Send(new CloneNutritionTemplateCommand(
            templateId, request.ClientId, request.Name, request.StartDate, request.EndDate, request.Notes, request.Price, request.Currency)));

    /// <summary>Save an existing plan as a reusable template.</summary>
    [HttpPost("{planId:int}/save-as-template")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<NutritionPlanDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NutritionPlanDetailDto>> SaveAsTemplate(int planId, CreateNutritionTemplateRequest request)
        => HandleCreated(await _sender.Send(new SaveNutritionPlanAsTemplateCommand(planId, request.Name, request.Notes)));

    // ===== Days =====

    /// <summary>Add a day to a plan.</summary>
    [HttpPost("{planId:int}/days")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<NutritionDayDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NutritionDayDto>> AddDay(int planId, AddNutritionDayRequest request)
        => HandleCreated(await _sender.Send(new AddNutritionDayCommand(
            planId, request.DayOfWeek, request.Label, request.TotalCaloriesTarget, request.Notes)));

    /// <summary>Delete a day.</summary>
    [HttpDelete("days/{dayId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDay(int dayId)
        => HandleResult(await _sender.Send(new DeleteNutritionDayCommand(dayId)));

    // ===== Meals =====

    /// <summary>Add a meal to a day.</summary>
    [HttpPost("days/{dayId:int}/meals")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<MealDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MealDto>> AddMeal(int dayId, AddMealRequest request)
        => HandleCreated(await _sender.Send(new AddMealCommand(dayId, request.MealType, request.Time, request.Notes)));

    /// <summary>Delete a meal.</summary>
    [HttpDelete("meals/{mealId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMeal(int mealId)
        => HandleResult(await _sender.Send(new DeleteMealCommand(mealId)));

    // ===== Meal items =====

    /// <summary>Add an item to a meal.</summary>
    [HttpPost("meals/{mealId:int}/items")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<MealItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MealItemDto>> AddMealItem(int mealId, AddMealItemRequest request)
        => HandleCreated(await _sender.Send(new AddMealItemCommand(
            mealId, request.Description, request.Quantity, request.Calories, request.ProteinG, request.CarbsG, request.FatG)));

    /// <summary>Delete a meal item.</summary>
    [HttpDelete("items/{itemId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMealItem(int itemId)
        => HandleResult(await _sender.Send(new DeleteMealItemCommand(itemId)));

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
        var result = await _sender.Send(new GetNutritionPlanPdfQuery(id));
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

    /// <summary>Public download of a shared plan PDF via token.</summary>
    [HttpGet("share/{token}/pdf")]
    [AllowAnonymous]
    [ResponseCache(CacheProfileName = "PublicShare")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
