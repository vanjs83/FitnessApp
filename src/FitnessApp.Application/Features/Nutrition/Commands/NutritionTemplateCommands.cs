using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Nutrition;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Nutrition.Commands;

// ===== Create empty template =====
public record CreateNutritionTemplateCommand(string Name, string? Notes) : IRequest<Result<NutritionPlanDetailDto>>;

public class CreateNutritionTemplateCommandHandler : IRequestHandler<CreateNutritionTemplateCommand, Result<NutritionPlanDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateNutritionTemplateCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<NutritionPlanDetailDto>> Handle(CreateNutritionTemplateCommand request, CancellationToken cancellationToken)
    {
        var tpl = new NutritionPlan
        {
            TrainerId = _currentUser.UserId,
            ClientId = null,
            Name = request.Name,
            IsTemplate = true,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date,
            Notes = request.Notes
        };
        _db.NutritionPlans.Add(tpl);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<NutritionPlanDetailDto>.Success(NutritionMapping.MapDetail(tpl, "", false));
    }
}

// ===== Update template name/notes =====
public record UpdateNutritionTemplateCommand(int Id, string Name, string? Notes) : IRequest<Result>;

public class UpdateNutritionTemplateCommandHandler : IRequestHandler<UpdateNutritionTemplateCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateNutritionTemplateCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateNutritionTemplateCommand request, CancellationToken cancellationToken)
    {
        var tpl = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (tpl == null) return Result.NotFound();
        if (tpl.TrainerId != _currentUser.UserId) return Result.Forbidden();
        if (!tpl.IsTemplate) return Result.Fail(ResultError.Validation, "Not a template.");

        tpl.Name = request.Name;
        tpl.Notes = request.Notes;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ===== Clone template to a client plan =====
public record CloneNutritionTemplateCommand(
    int TemplateId,
    string ClientId,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string? Notes,
    decimal Price,
    string Currency) : IRequest<Result<NutritionPlanDetailDto>>;

public class CloneNutritionTemplateCommandHandler : IRequestHandler<CloneNutritionTemplateCommand, Result<NutritionPlanDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public CloneNutritionTemplateCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<NutritionPlanDetailDto>> Handle(CloneNutritionTemplateCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate < request.StartDate)
            return Result<NutritionPlanDetailDto>.Fail(ResultError.Validation, "End date must be after start date.");

        var tpl = await _db.NutritionPlans
            .Include(p => p.Days).ThenInclude(d => d.Meals).ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == request.TemplateId, cancellationToken);
        if (tpl == null) return Result<NutritionPlanDetailDto>.NotFound();
        if (tpl.TrainerId != _currentUser.UserId) return Result<NutritionPlanDetailDto>.Forbidden();
        if (!tpl.IsTemplate) return Result<NutritionPlanDetailDto>.Fail(ResultError.Validation, "Plan is not a template.");

        var client = await _users.FindAsync(request.ClientId, cancellationToken);
        if (client == null) return Result<NutritionPlanDetailDto>.Fail(ResultError.Validation, "Client not found.");
        if (client.TrainerId != _currentUser.UserId) return Result<NutritionPlanDetailDto>.Forbidden();

        var plan = new NutritionPlan
        {
            TrainerId = _currentUser.UserId,
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
        NutritionMapping.CopyDaysInto(tpl, plan);

        _db.NutritionPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<NutritionPlanDetailDto>.Success(NutritionMapping.MapDetail(plan, client.DisplayName, false));
    }
}

// ===== Save an existing plan as a template =====
public record SaveNutritionPlanAsTemplateCommand(int PlanId, string Name, string? Notes) : IRequest<Result<NutritionPlanDetailDto>>;

public class SaveNutritionPlanAsTemplateCommandHandler : IRequestHandler<SaveNutritionPlanAsTemplateCommand, Result<NutritionPlanDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SaveNutritionPlanAsTemplateCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<NutritionPlanDetailDto>> Handle(SaveNutritionPlanAsTemplateCommand request, CancellationToken cancellationToken)
    {
        var source = await _db.NutritionPlans
            .Include(p => p.Days).ThenInclude(d => d.Meals).ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (source == null) return Result<NutritionPlanDetailDto>.NotFound();
        if (source.TrainerId != _currentUser.UserId) return Result<NutritionPlanDetailDto>.Forbidden();

        var tpl = new NutritionPlan
        {
            TrainerId = _currentUser.UserId,
            ClientId = null,
            Name = request.Name,
            IsTemplate = true,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date,
            Notes = request.Notes ?? source.Notes
        };
        NutritionMapping.CopyDaysInto(source, tpl);

        _db.NutritionPlans.Add(tpl);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<NutritionPlanDetailDto>.Success(NutritionMapping.MapDetail(tpl, "", false));
    }
}
