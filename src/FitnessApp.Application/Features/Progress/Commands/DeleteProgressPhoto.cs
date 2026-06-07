using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Application.Storage;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FitnessApp.Application.Features.Progress.Commands;

public record DeleteProgressPhotoCommand(int Id) : IRequest<Result>;

public class DeleteProgressPhotoCommandHandler : IRequestHandler<DeleteProgressPhotoCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _files;
    private readonly StorageSettings _storage;
    private readonly IWebHostEnvironment _env;

    public DeleteProgressPhotoCommandHandler(
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

    public async Task<Result> Handle(DeleteProgressPhotoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var photo = await _db.ProgressPhotos
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.ClientId == userId, cancellationToken);
        if (photo == null) return Result.NotFound();

        var folderPath = _storage.ResolveProgressImagesPath(_env.ContentRootPath);
        _files.DeleteByUrl(photo.ImagePath, folderPath, _storage.ProgressImagesUrl);

        _db.ProgressPhotos.Remove(photo);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
