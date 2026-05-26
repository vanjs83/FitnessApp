using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Claims;
using FitnessApp.Application.DTOs.Nutrition;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/nutrition-plans")]
public class NutritionPlansController : ControllerBase
{
    private const string ShareKind = "nutrition";

    private readonly AppDbContext _db;
    private readonly INutritionPlanPdfService _pdf;
    private readonly IPlanShareTokenService _shareTokens;

    public NutritionPlansController(AppDbContext db, INutritionPlanPdfService pdf, IPlanShareTokenService shareTokens)
    {
        _db = db;
        _pdf = pdf;
        _shareTokens = shareTokens;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool IsTrainer => User.IsInRole(Roles.Trainer);

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IEnumerable<NutritionPlanListItemDto>>> GetMyPlans()
    {
        var plans = await _db.NutritionPlans
            .Where(p => p.TrainerId == UserId && !p.IsTemplate)
            .OrderByDescending(p => p.StartDate)
            .Join(_db.Users,
                p => p.ClientId,
                u => u.Id,
                (p, u) => new NutritionPlanListItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    ClientId = p.ClientId!,
                    ClientName = u.FullName ?? u.Email!,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    DayCount = p.Days.Count,
                    Price = p.Price,
                    Currency = p.Currency,
                    PaymentStatus = p.PaymentStatus
                })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpGet("client/{clientId}")]
    public async Task<ActionResult<IEnumerable<NutritionPlanListItemDto>>> GetForClient(string clientId)
    {
        if (!IsTrainer && clientId != UserId) return Forbid();

        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == clientId);
        if (client == null) return NotFound();
        if (IsTrainer && client.TrainerId != UserId) return Forbid();

        var query = _db.NutritionPlans.Where(p => p.ClientId == clientId && !p.IsTemplate);
        if (!IsTrainer)
        {
            if (string.IsNullOrEmpty(client.TrainerId))
                return Ok(Array.Empty<NutritionPlanListItemDto>());
            query = query.Where(p => p.TrainerId == client.TrainerId);
        }

        var plans = await query
            .OrderByDescending(p => p.StartDate)
            .Select(p => new NutritionPlanListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                ClientId = p.ClientId!,
                ClientName = client.FullName ?? client.Email!,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                DayCount = p.Days.Count,
                Price = p.Price,
                Currency = p.Currency,
                PaymentStatus = p.PaymentStatus
            })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NutritionPlanDetailDto>> GetById(int id)
    {
        var plan = await _db.NutritionPlans
            .Include(p => p.Days).ThenInclude(d => d.Meals).ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();

        if (plan.IsTemplate)
        {
            if (!IsTrainer || plan.TrainerId != UserId) return Forbid();
            return Ok(Map(plan, "", false));
        }

        if (IsTrainer && plan.TrainerId != UserId) return Forbid();
        if (!IsTrainer && plan.ClientId != UserId) return Forbid();

        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == plan.ClientId);
        if (!IsTrainer && client?.TrainerId != plan.TrainerId) return Forbid();

        var isLocked = !IsTrainer && plan.PaymentStatus != PaymentStatus.Approved;
        return Ok(Map(plan, client?.FullName ?? client?.Email ?? "", isLocked));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<NutritionPlanDetailDto>> Create(CreateNutritionPlanRequest request)
    {
        if (request.EndDate < request.StartDate)
            return BadRequest(new { message = "End date must be after start date." });

        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.ClientId);
        if (client == null) return BadRequest(new { message = "Client not found." });
        if (client.TrainerId != UserId) return Forbid();

        var plan = new NutritionPlan
        {
            TrainerId = UserId,
            ClientId = request.ClientId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Notes = request.Notes,
            Price = request.Price,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency,
            PaymentStatus = request.Price > 0 ? PaymentStatus.Pending : PaymentStatus.Approved
        };
        if (plan.PaymentStatus == PaymentStatus.Approved)
            plan.ApprovedAt = DateTime.UtcNow;

        _db.NutritionPlans.Add(plan);
        await _db.SaveChangesAsync();

        return Ok(Map(plan, client.FullName ?? client.Email ?? "", false));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Update(int id, UpdateNutritionPlanRequest request)
    {
        if (request.EndDate < request.StartDate)
            return BadRequest(new { message = "End date must be after start date." });

        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        if (plan.TrainerId != UserId) return Forbid();

        plan.Name = request.Name;
        plan.StartDate = request.StartDate;
        plan.EndDate = request.EndDate;
        plan.Notes = request.Notes;
        plan.Price = request.Price;
        plan.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency;

        if (plan.Price <= 0 && plan.PaymentStatus != PaymentStatus.Approved)
        {
            plan.PaymentStatus = PaymentStatus.Approved;
            plan.ApprovedAt ??= DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/claim-payment")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> ClaimPayment(int id)
    {
        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        if (plan.ClientId != UserId) return Forbid();
        if (plan.PaymentStatus == PaymentStatus.Approved)
            return BadRequest(new { message = "Plan is already approved." });

        plan.PaymentStatus = PaymentStatus.PaymentClaimed;
        plan.PaymentClaimedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { paymentStatus = plan.PaymentStatus.ToString() });
    }

    [HttpPost("{id:int}/approve-payment")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> ApprovePayment(int id)
    {
        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        if (plan.TrainerId != UserId) return Forbid();

        plan.PaymentStatus = PaymentStatus.Approved;
        plan.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { paymentStatus = plan.PaymentStatus.ToString() });
    }

    [HttpPost("{id:int}/revoke-approval")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> RevokeApproval(int id)
    {
        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        if (plan.TrainerId != UserId) return Forbid();
        if (plan.Price <= 0)
            return BadRequest(new { message = "A free plan cannot be locked." });

        plan.PaymentStatus = PaymentStatus.Pending;
        plan.ApprovedAt = null;
        plan.PaymentClaimedAt = null;
        await _db.SaveChangesAsync();
        return Ok(new { paymentStatus = plan.PaymentStatus.ToString() });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Delete(int id)
    {
        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        if (plan.TrainerId != UserId) return Forbid();

        _db.NutritionPlans.Remove(plan);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Templates =====

    [HttpGet("templates")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IEnumerable<NutritionTemplateListItemDto>>> GetTemplates()
    {
        var items = await _db.NutritionPlans
            .Where(p => p.TrainerId == UserId && p.IsTemplate)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new NutritionTemplateListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Notes = p.Notes,
                DayCount = p.Days.Count,
                CreatedAt = p.CreatedAt
            }).ToListAsync();
        return Ok(items);
    }

    [HttpPost("templates")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<NutritionPlanDetailDto>> CreateTemplate(CreateNutritionTemplateRequest request)
    {
        var tpl = new NutritionPlan
        {
            TrainerId = UserId,
            ClientId = null,
            Name = request.Name,
            IsTemplate = true,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date,
            Notes = request.Notes
        };
        _db.NutritionPlans.Add(tpl);
        await _db.SaveChangesAsync();
        return Ok(Map(tpl, "", false));
    }

    [HttpPut("templates/{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> UpdateTemplate(int id, CreateNutritionTemplateRequest request)
    {
        var tpl = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (tpl == null) return NotFound();
        if (tpl.TrainerId != UserId) return Forbid();
        if (!tpl.IsTemplate) return BadRequest(new { message = "Not a template." });

        tpl.Name = request.Name;
        tpl.Notes = request.Notes;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("templates/{templateId:int}/clone")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<NutritionPlanDetailDto>> CloneTemplate(int templateId, CloneNutritionTemplateRequest request)
    {
        if (request.EndDate < request.StartDate)
            return BadRequest(new { message = "End date must be after start date." });

        var tpl = await _db.NutritionPlans
            .Include(p => p.Days).ThenInclude(d => d.Meals).ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == templateId);
        if (tpl == null) return NotFound();
        if (tpl.TrainerId != UserId) return Forbid();
        if (!tpl.IsTemplate) return BadRequest(new { message = "Plan is not a template." });

        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.ClientId);
        if (client == null) return BadRequest(new { message = "Client not found." });
        if (client.TrainerId != UserId) return Forbid();

        var plan = new NutritionPlan
        {
            TrainerId = UserId,
            ClientId = request.ClientId,
            Name = request.Name,
            IsTemplate = false,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Notes = request.Notes ?? tpl.Notes,
            Price = request.Price,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency,
            PaymentStatus = request.Price > 0 ? PaymentStatus.Pending : PaymentStatus.Approved,
            ApprovedAt = request.Price > 0 ? null : DateTime.UtcNow
        };

        foreach (var d in tpl.Days.OrderBy(x => x.DayOfWeek))
        {
            var nd = new NutritionDay
            {
                DayOfWeek = d.DayOfWeek,
                Label = d.Label,
                TotalCaloriesTarget = d.TotalCaloriesTarget,
                Notes = d.Notes
            };
            foreach (var m in d.Meals.OrderBy(x => x.Order))
            {
                var nm = new Meal
                {
                    MealType = m.MealType,
                    Time = m.Time,
                    Order = m.Order,
                    Notes = m.Notes
                };
                foreach (var it in m.Items.OrderBy(x => x.Order))
                {
                    nm.Items.Add(new MealItem
                    {
                        Order = it.Order,
                        Description = it.Description,
                        Quantity = it.Quantity,
                        Calories = it.Calories,
                        ProteinG = it.ProteinG,
                        CarbsG = it.CarbsG,
                        FatG = it.FatG
                    });
                }
                nd.Meals.Add(nm);
            }
            plan.Days.Add(nd);
        }

        _db.NutritionPlans.Add(plan);
        await _db.SaveChangesAsync();
        return Ok(Map(plan, client.FullName ?? client.Email ?? "", false));
    }

    [HttpPost("{planId:int}/save-as-template")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<NutritionPlanDetailDto>> SaveAsTemplate(int planId, CreateNutritionTemplateRequest request)
    {
        var source = await _db.NutritionPlans
            .Include(p => p.Days).ThenInclude(d => d.Meals).ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (source == null) return NotFound();
        if (source.TrainerId != UserId) return Forbid();

        var tpl = new NutritionPlan
        {
            TrainerId = UserId,
            ClientId = null,
            Name = request.Name,
            IsTemplate = true,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date,
            Notes = request.Notes ?? source.Notes
        };

        foreach (var d in source.Days.OrderBy(x => x.DayOfWeek))
        {
            var nd = new NutritionDay
            {
                DayOfWeek = d.DayOfWeek,
                Label = d.Label,
                TotalCaloriesTarget = d.TotalCaloriesTarget,
                Notes = d.Notes
            };
            foreach (var m in d.Meals.OrderBy(x => x.Order))
            {
                var nm = new Meal
                {
                    MealType = m.MealType,
                    Time = m.Time,
                    Order = m.Order,
                    Notes = m.Notes
                };
                foreach (var it in m.Items.OrderBy(x => x.Order))
                {
                    nm.Items.Add(new MealItem
                    {
                        Order = it.Order,
                        Description = it.Description,
                        Quantity = it.Quantity,
                        Calories = it.Calories,
                        ProteinG = it.ProteinG,
                        CarbsG = it.CarbsG,
                        FatG = it.FatG
                    });
                }
                nd.Meals.Add(nm);
            }
            tpl.Days.Add(nd);
        }

        _db.NutritionPlans.Add(tpl);
        await _db.SaveChangesAsync();
        return Ok(Map(tpl, "", false));
    }

    // ===== Days =====

    [HttpPost("{planId:int}/days")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<NutritionDayDto>> AddDay(int planId, AddNutritionDayRequest request)
    {
        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) return NotFound();
        if (plan.TrainerId != UserId) return Forbid();

        var day = new NutritionDay
        {
            NutritionPlanId = planId,
            DayOfWeek = request.DayOfWeek,
            Label = request.Label,
            TotalCaloriesTarget = request.TotalCaloriesTarget,
            Notes = request.Notes
        };
        _db.NutritionDays.Add(day);
        await _db.SaveChangesAsync();

        return Ok(new NutritionDayDto
        {
            Id = day.Id,
            DayOfWeek = day.DayOfWeek,
            Label = day.Label,
            TotalCaloriesTarget = day.TotalCaloriesTarget,
            Notes = day.Notes,
            Meals = new()
        });
    }

    [HttpDelete("days/{dayId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteDay(int dayId)
    {
        var day = await _db.NutritionDays
            .Include(d => d.NutritionPlan)
            .FirstOrDefaultAsync(d => d.Id == dayId);
        if (day == null) return NotFound();
        if (day.NutritionPlan.TrainerId != UserId) return Forbid();

        _db.NutritionDays.Remove(day);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Meals =====

    [HttpPost("days/{dayId:int}/meals")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<MealDto>> AddMeal(int dayId, AddMealRequest request)
    {
        var day = await _db.NutritionDays
            .Include(d => d.NutritionPlan)
            .Include(d => d.Meals)
            .FirstOrDefaultAsync(d => d.Id == dayId);
        if (day == null) return NotFound();
        if (day.NutritionPlan.TrainerId != UserId) return Forbid();

        TimeOnly? time = null;
        if (!string.IsNullOrWhiteSpace(request.Time) && TimeOnly.TryParse(request.Time, out var t))
            time = t;

        var meal = new Meal
        {
            NutritionDayId = dayId,
            MealType = request.MealType,
            Time = time,
            Order = day.Meals.Count + 1,
            Notes = request.Notes
        };
        _db.Meals.Add(meal);
        await _db.SaveChangesAsync();

        return Ok(new MealDto
        {
            Id = meal.Id,
            MealType = meal.MealType,
            Time = meal.Time?.ToString("HH:mm"),
            Order = meal.Order,
            Notes = meal.Notes,
            Items = new()
        });
    }

    [HttpDelete("meals/{mealId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteMeal(int mealId)
    {
        var meal = await _db.Meals
            .Include(m => m.NutritionDay).ThenInclude(d => d.NutritionPlan)
            .FirstOrDefaultAsync(m => m.Id == mealId);
        if (meal == null) return NotFound();
        if (meal.NutritionDay.NutritionPlan.TrainerId != UserId) return Forbid();

        _db.Meals.Remove(meal);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Meal items =====

    [HttpPost("meals/{mealId:int}/items")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<MealItemDto>> AddMealItem(int mealId, AddMealItemRequest request)
    {
        var meal = await _db.Meals
            .Include(m => m.NutritionDay).ThenInclude(d => d.NutritionPlan)
            .Include(m => m.Items)
            .FirstOrDefaultAsync(m => m.Id == mealId);
        if (meal == null) return NotFound();
        if (meal.NutritionDay.NutritionPlan.TrainerId != UserId) return Forbid();

        var item = new MealItem
        {
            MealId = mealId,
            Order = meal.Items.Count + 1,
            Description = request.Description,
            Quantity = request.Quantity,
            Calories = request.Calories,
            ProteinG = request.ProteinG,
            CarbsG = request.CarbsG,
            FatG = request.FatG
        };
        _db.MealItems.Add(item);
        await _db.SaveChangesAsync();

        return Ok(new MealItemDto
        {
            Id = item.Id,
            Order = item.Order,
            Description = item.Description,
            Quantity = item.Quantity,
            Calories = item.Calories,
            ProteinG = item.ProteinG,
            CarbsG = item.CarbsG,
            FatG = item.FatG
        });
    }

    [HttpDelete("items/{itemId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteMealItem(int itemId)
    {
        var item = await _db.MealItems
            .Include(i => i.Meal).ThenInclude(m => m.NutritionDay).ThenInclude(d => d.NutritionPlan)
            .FirstOrDefaultAsync(i => i.Id == itemId);
        if (item == null) return NotFound();
        if (item.Meal.NutritionDay.NutritionPlan.TrainerId != UserId) return Forbid();

        _db.MealItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var plan = await _db.NutritionPlans
            .Include(p => p.Days).ThenInclude(d => d.Meals).ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();

        if (plan.IsTemplate)
        {
            if (!IsTrainer || plan.TrainerId != UserId) return Forbid();
        }
        else
        {
            if (IsTrainer && plan.TrainerId != UserId) return Forbid();
            if (!IsTrainer && plan.ClientId != UserId) return Forbid();
        }

        var users = await _db.Users
            .Where(u => u.Id == plan.ClientId || u.Id == plan.TrainerId)
            .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Email!);

        string? clientName = null;
        if (!string.IsNullOrEmpty(plan.ClientId))
            users.TryGetValue(plan.ClientId, out clientName);
        users.TryGetValue(plan.TrainerId, out var trainerName);

        var bytes = _pdf.Generate(plan, clientName, trainerName);

        var safeName = string.Concat(plan.Name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "prehrana";
        var fileName = $"prehrana_{safeName}_{plan.StartDate:yyyyMMdd}.pdf";

        return File(bytes, "application/pdf", fileName);
    }

    [HttpPost("{id:int}/share")]
    public async Task<ActionResult<object>> CreateShareLink(int id)
    {
        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        if (plan.IsTemplate) return BadRequest(new { message = "Templates cannot be shared." });

        if (IsTrainer && plan.TrainerId != UserId) return Forbid();
        if (!IsTrainer && plan.ClientId != UserId) return Forbid();

        var token = _shareTokens.CreateForKind(ShareKind, plan.Id);
        var baseUrl = BuildPublicBaseUrl();
        var shareUrl = $"{baseUrl}/api/nutrition-plans/share/{token}/pdf";

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
        if (!_shareTokens.TryValidateForKind(ShareKind, token, out var planId))
            return NotFound(new { message = "Link expired or invalid." });

        var plan = await _db.NutritionPlans
            .Include(p => p.Days).ThenInclude(d => d.Meals).ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) return NotFound();
        if (plan.IsTemplate) return NotFound();

        var users = await _db.Users
            .Where(u => u.Id == plan.ClientId || u.Id == plan.TrainerId)
            .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Email!);

        string? clientName = null;
        if (!string.IsNullOrEmpty(plan.ClientId))
            users.TryGetValue(plan.ClientId, out clientName);
        users.TryGetValue(plan.TrainerId, out var trainerName);

        var bytes = _pdf.Generate(plan, clientName, trainerName);

        var safeName = string.Concat(plan.Name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "prehrana";
        return File(bytes, "application/pdf", $"prehrana_{safeName}_{plan.StartDate:yyyyMMdd}.pdf");
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

    private static NutritionPlanDetailDto Map(NutritionPlan p, string clientName, bool isLocked) => new()
    {
        Id = p.Id,
        Name = p.Name,
        ClientId = p.ClientId,
        ClientName = clientName,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        Notes = isLocked ? null : p.Notes,
        IsTemplate = p.IsTemplate,
        Price = p.Price,
        Currency = p.Currency,
        PaymentStatus = p.PaymentStatus,
        PaymentClaimedAt = p.PaymentClaimedAt,
        ApprovedAt = p.ApprovedAt,
        IsLocked = isLocked,
        Days = isLocked ? new() : p.Days
            .OrderBy(d => d.DayOfWeek)
            .Select(d => new NutritionDayDto
            {
                Id = d.Id,
                DayOfWeek = d.DayOfWeek,
                Label = d.Label,
                TotalCaloriesTarget = d.TotalCaloriesTarget,
                Notes = d.Notes,
                Meals = d.Meals.OrderBy(m => m.Order).Select(m => new MealDto
                {
                    Id = m.Id,
                    MealType = m.MealType,
                    Time = m.Time?.ToString("HH:mm"),
                    Order = m.Order,
                    Notes = m.Notes,
                    Items = m.Items.OrderBy(i => i.Order).Select(i => new MealItemDto
                    {
                        Id = i.Id,
                        Order = i.Order,
                        Description = i.Description,
                        Quantity = i.Quantity,
                        Calories = i.Calories,
                        ProteinG = i.ProteinG,
                        CarbsG = i.CarbsG,
                        FatG = i.FatG
                    }).ToList()
                }).ToList()
            }).ToList()
    };
}
