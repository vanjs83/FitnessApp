using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Admin;
using MediatR;

namespace FitnessApp.Application.Features.Admin.Commands;

public record CreateTrainerCommand(string Email, string? FullName, string Password) : IRequest<Result<TrainerAdminDto>>;

public class CreateTrainerCommandHandler : IRequestHandler<CreateTrainerCommand, Result<TrainerAdminDto>>
{
    private readonly IIdentityAdminService _identity;

    public CreateTrainerCommandHandler(IIdentityAdminService identity) => _identity = identity;

    public async Task<Result<TrainerAdminDto>> Handle(CreateTrainerCommand request, CancellationToken cancellationToken)
    {
        if (await _identity.EmailExistsAsync(request.Email, cancellationToken))
            return Result<TrainerAdminDto>.Fail(ResultError.Validation, "A user with this email already exists.");

        var (succeeded, user, errors) = await _identity.CreateTrainerAsync(
            request.Email, request.FullName, request.Password, cancellationToken);


        if (!succeeded || user == null)
            return Result<TrainerAdminDto>.Fail(ResultError.Validation, string.Join(", ", errors));

        return Result<TrainerAdminDto>.Success(new TrainerAdminDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt,
            ClientCount = 0
        });
    }
}
