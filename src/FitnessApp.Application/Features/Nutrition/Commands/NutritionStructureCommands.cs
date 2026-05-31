using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Nutrition;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Nutrition.Commands;

// ===== Days =====
public record AddNutritionDayCommand(
    int PlanId,
    DayOfWeek DayOfWeek,
    string Label,
    int? TotalCaloriesTarget,
    string? Notes) : IRequest<Result<NutritionDayDto>>;

public class AddNutritionDayCommandHandler : IRequestHandler<AddNutritionDayCommand, Result<NutritionDayDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddNutritionDayCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<NutritionDayDto>> Handle(AddNutritionDayCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan == null) return Result<NutritionDayDto>.NotFound();
        if (plan.TrainerId != _currentUser.UserId) return Result<NutritionDayDto>.Forbidden();

        var day = new NutritionDay
        {
            NutritionPlanId = request.PlanId,
            DayOfWeek = request.DayOfWeek,
            Label = request.Label,
            TotalCaloriesTarget = request.TotalCaloriesTarget,
            Notes = request.Notes
        };
        _db.NutritionDays.Add(day);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<NutritionDayDto>.Success(new NutritionDayDto
        {
            Id = day.Id,
            DayOfWeek = day.DayOfWeek,
            Label = day.Label,
            TotalCaloriesTarget = day.TotalCaloriesTarget,
            Notes = day.Notes,
            Meals = new()
        });
    }
}

public record DeleteNutritionDayCommand(int DayId) : IRequest<Result>;

public class DeleteNutritionDayCommandHandler : IRequestHandler<DeleteNutritionDayCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteNutritionDayCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteNutritionDayCommand request, CancellationToken cancellationToken)
    {
        var day = await _db.NutritionDays
            .Include(d => d.NutritionPlan)
            .FirstOrDefaultAsync(d => d.Id == request.DayId, cancellationToken);
        if (day == null) return Result.NotFound();
        if (day.NutritionPlan.TrainerId != _currentUser.UserId) return Result.Forbidden();

        _db.NutritionDays.Remove(day);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ===== Meals =====
public record AddMealCommand(int DayId, MealType MealType, string? Time, string? Notes) : IRequest<Result<MealDto>>;

public class AddMealCommandHandler : IRequestHandler<AddMealCommand, Result<MealDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddMealCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<MealDto>> Handle(AddMealCommand request, CancellationToken cancellationToken)
    {
        var day = await _db.NutritionDays
            .Include(d => d.NutritionPlan)
            .Include(d => d.Meals)
            .FirstOrDefaultAsync(d => d.Id == request.DayId, cancellationToken);
        if (day == null) return Result<MealDto>.NotFound();
        if (day.NutritionPlan.TrainerId != _currentUser.UserId) return Result<MealDto>.Forbidden();

        TimeOnly? time = null;
        if (!string.IsNullOrWhiteSpace(request.Time) && TimeOnly.TryParse(request.Time, out var t))
            time = t;

        var meal = new Meal
        {
            NutritionDayId = request.DayId,
            MealType = request.MealType,
            Time = time,
            Order = day.Meals.Count + 1,
            Notes = request.Notes
        };
        _db.Meals.Add(meal);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<MealDto>.Success(new MealDto
        {
            Id = meal.Id,
            MealType = meal.MealType,
            Time = meal.Time?.ToString("HH:mm"),
            Order = meal.Order,
            Notes = meal.Notes,
            Items = new()
        });
    }
}

public record DeleteMealCommand(int MealId) : IRequest<Result>;

public class DeleteMealCommandHandler : IRequestHandler<DeleteMealCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteMealCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteMealCommand request, CancellationToken cancellationToken)
    {
        var meal = await _db.Meals
            .Include(m => m.NutritionDay).ThenInclude(d => d.NutritionPlan)
            .FirstOrDefaultAsync(m => m.Id == request.MealId, cancellationToken);
        if (meal == null) return Result.NotFound();
        if (meal.NutritionDay.NutritionPlan.TrainerId != _currentUser.UserId) return Result.Forbidden();

        _db.Meals.Remove(meal);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ===== Meal items =====
public record AddMealItemCommand(
    int MealId,
    string Description,
    string? Quantity,
    int? Calories,
    decimal? ProteinG,
    decimal? CarbsG,
    decimal? FatG) : IRequest<Result<MealItemDto>>;

public class AddMealItemCommandHandler : IRequestHandler<AddMealItemCommand, Result<MealItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddMealItemCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<MealItemDto>> Handle(AddMealItemCommand request, CancellationToken cancellationToken)
    {
        var meal = await _db.Meals
            .Include(m => m.NutritionDay).ThenInclude(d => d.NutritionPlan)
            .Include(m => m.Items)
            .FirstOrDefaultAsync(m => m.Id == request.MealId, cancellationToken);
        if (meal == null) return Result<MealItemDto>.NotFound();
        if (meal.NutritionDay.NutritionPlan.TrainerId != _currentUser.UserId) return Result<MealItemDto>.Forbidden();

        var item = new MealItem
        {
            MealId = request.MealId,
            Order = meal.Items.Count + 1,
            Description = request.Description,
            Quantity = request.Quantity,
            Calories = request.Calories,
            ProteinG = request.ProteinG,
            CarbsG = request.CarbsG,
            FatG = request.FatG
        };
        _db.MealItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<MealItemDto>.Success(new MealItemDto
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
}

public record DeleteMealItemCommand(int ItemId) : IRequest<Result>;

public class DeleteMealItemCommandHandler : IRequestHandler<DeleteMealItemCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteMealItemCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteMealItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.MealItems
            .Include(i => i.Meal).ThenInclude(m => m.NutritionDay).ThenInclude(d => d.NutritionPlan)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken);
        if (item == null) return Result.NotFound();
        if (item.Meal.NutritionDay.NutritionPlan.TrainerId != _currentUser.UserId) return Result.Forbidden();

        _db.MealItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
