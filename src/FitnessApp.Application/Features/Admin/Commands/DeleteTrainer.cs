using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using MediatR;

namespace FitnessApp.Application.Features.Admin.Commands;

public record DeleteTrainerCommand(string Id) : IRequest<Result>;

public class DeleteTrainerCommandHandler : IRequestHandler<DeleteTrainerCommand, Result>
{
    private readonly IIdentityAdminService _identity;

    public DeleteTrainerCommandHandler(IIdentityAdminService identity) => _identity = identity;

    public async Task<Result> Handle(DeleteTrainerCommand request, CancellationToken cancellationToken)
    {
        var outcome = await _identity.DeactivateTrainerAsync(request.Id, cancellationToken);
        return outcome switch
        {
            TrainerDeleteOutcome.NotFound => Result.NotFound(),
            TrainerDeleteOutcome.NotTrainer => Result.Fail(ResultError.Validation, "User is not a trainer."),
            _ => Result.Success()
        };
    }
}
