using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.TrainingPlans;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Commands;

// ===== Create empty template =====
public record CreateTrainingTemplateCommand(string Name, string? TrainerExpectations)
    : IRequest<Result<TrainingPlanDetailDto>>;

public class CreateTrainingTemplateCommandHandler : IRequestHandler<CreateTrainingTemplateCommand, Result<TrainingPlanDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateTrainingTemplateCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<TrainingPlanDetailDto>> Handle(CreateTrainingTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = new TrainingPlan
        {
            TrainerId = _currentUser.UserId,
            ClientId = null,
            Name = request.Name,
            IsTemplate = true,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date,
            TrainerExpectations = request.TrainerExpectations,
            Price = 0,
            Currency = "EUR",
            PaymentStatus = PaymentStatus.Approved
        };
        _db.TrainingPlans.Add(template);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<TrainingPlanDetailDto>.Success(TrainingMapping.MapDetail(template, "", false));
    }
}

// ===== Update template name/expectations =====
public record UpdateTrainingTemplateCommand(int Id, string Name, string? TrainerExpectations) : IRequest<Result>;

public class UpdateTrainingTemplateCommandHandler : IRequestHandler<UpdateTrainingTemplateCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateTrainingTemplateCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateTrainingTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (template == null) return Result.NotFound();
        if (template.TrainerId != _currentUser.UserId) return Result.Forbidden();
        if (!template.IsTemplate) return Result.Fail(ResultError.Validation, "Not a template.");

        template.Name = request.Name;
        template.TrainerExpectations = request.TrainerExpectations;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ===== Clone template to a client plan =====
public record CloneTrainingTemplateToClientCommand(
    int TemplateId,
    string ClientId,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string? TrainerExpectations,
    decimal Price,
    string Currency) : IRequest<Result<TrainingPlanDetailDto>>;

public class CloneTrainingTemplateToClientCommandHandler
    : IRequestHandler<CloneTrainingTemplateToClientCommand, Result<TrainingPlanDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public CloneTrainingTemplateToClientCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<TrainingPlanDetailDto>> Handle(CloneTrainingTemplateToClientCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate < request.StartDate)
            return Result<TrainingPlanDetailDto>.Fail(ResultError.Validation, "End date must be after start date.");

        var template = await _db.TrainingPlans
            .Include(p => p.Days).ThenInclude(d => d.Exercises)
            .FirstOrDefaultAsync(p => p.Id == request.TemplateId, cancellationToken);
        if (template == null) return Result<TrainingPlanDetailDto>.NotFound();
        if (template.TrainerId != _currentUser.UserId) return Result<TrainingPlanDetailDto>.Forbidden();
        if (!template.IsTemplate) return Result<TrainingPlanDetailDto>.Fail(ResultError.Validation, "Plan is not a template.");

        var client = await _users.FindAsync(request.ClientId, cancellationToken);
        if (client == null) return Result<TrainingPlanDetailDto>.Fail(ResultError.Validation, "Client not found.");
        if (client.TrainerId != _currentUser.UserId) return Result<TrainingPlanDetailDto>.Forbidden();

        var newPlan = new TrainingPlan
        {
            TrainerId = _currentUser.UserId,
            ClientId = request.ClientId,
            Name = request.Name,
            IsTemplate = false,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TrainerExpectations = request.TrainerExpectations ?? template.TrainerExpectations,
            Price = request.Price,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency,
            PaymentStatus = request.Price > 0 ? PaymentStatus.Pending : PaymentStatus.Approved
        };
        if (newPlan.PaymentStatus == PaymentStatus.Approved)
            newPlan.ApprovedAt = DateTime.UtcNow;

        TrainingMapping.CopyDaysInto(template, newPlan);

        _db.TrainingPlans.Add(newPlan);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<TrainingPlanDetailDto>.Success(TrainingMapping.MapDetail(newPlan, client.DisplayName, false));
    }
}

// ===== Save an existing plan as a template =====
public record SaveTrainingPlanAsTemplateCommand(int PlanId, string Name, string? TrainerExpectations)
    : IRequest<Result<TrainingPlanDetailDto>>;

public class SaveTrainingPlanAsTemplateCommandHandler
    : IRequestHandler<SaveTrainingPlanAsTemplateCommand, Result<TrainingPlanDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SaveTrainingPlanAsTemplateCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<TrainingPlanDetailDto>> Handle(SaveTrainingPlanAsTemplateCommand request, CancellationToken cancellationToken)
    {
        var source = await _db.TrainingPlans
            .Include(p => p.Days).ThenInclude(d => d.Exercises)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (source == null) return Result<TrainingPlanDetailDto>.NotFound();
        if (source.TrainerId != _currentUser.UserId) return Result<TrainingPlanDetailDto>.Forbidden();

        var template = new TrainingPlan
        {
            TrainerId = _currentUser.UserId,
            ClientId = null,
            Name = request.Name,
            IsTemplate = true,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date,
            TrainerExpectations = request.TrainerExpectations ?? source.TrainerExpectations,
            Price = 0,
            Currency = "EUR",
            PaymentStatus = PaymentStatus.Approved
        };
        TrainingMapping.CopyDaysInto(source, template);

        _db.TrainingPlans.Add(template);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<TrainingPlanDetailDto>.Success(TrainingMapping.MapDetail(template, "", false));
    }
}
