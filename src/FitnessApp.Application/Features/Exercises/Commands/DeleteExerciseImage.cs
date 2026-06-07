using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Exercises;
using FitnessApp.Application.Interfaces;
using FitnessApp.Application.Storage;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace FitnessApp.Application.Features.Exercises.Commands;

public record DeleteExerciseImageCommand(int Id) : IRequest<Result<ExerciseDto>>;

public class DeleteExerciseImageCommandHandler : IRequestHandler<DeleteExerciseImageCommand, Result<ExerciseDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _files;
    private readonly StorageSettings _storage;
    private readonly IWebHostEnvironment _env;

    public DeleteExerciseImageCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService files,
        IOptions<StorageSettings> storage,
        IWebHostEnvironment env)
    {
        _db = db;
        _currentUser = currentUser;
        _files = files;
        _storage = storage.Value;
        _env = env;
    }

    public async Task<Result<ExerciseDto>> Handle(DeleteExerciseImageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var e = await _db.Exercises.FindAsync(new object?[] { request.Id }, cancellationToken);
        if (e == null) return Result<ExerciseDto>.NotFound();
        if (e.CreatedByUserId != userId) return Result<ExerciseDto>.Forbidden();

        var folderPath = _storage.ResolveExerciseImagesPath(_env.ContentRootPath);
        _files.DeleteByUrl(e.ImageUrl, folderPath, _storage.ExerciseImagesUrl);
        e.ImageUrl = null;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<ExerciseDto>.Success(e.ToDto(userId));
    }
}
