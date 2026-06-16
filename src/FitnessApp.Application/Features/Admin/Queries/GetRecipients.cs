using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Admin;
using FitnessApp.Domain.Common;
using MediatR;

namespace FitnessApp.Application.Features.Admin.Queries;

/// <summary>
/// All trainers and clients in one flat list for the mail/push recipient picker.
/// Deliberately unpaginated — the picker needs every user to support "select all".
/// </summary>
public record GetRecipientsQuery : IRequest<IReadOnlyList<AdminRecipientDto>>;

public class GetRecipientsQueryHandler : IRequestHandler<GetRecipientsQuery, IReadOnlyList<AdminRecipientDto>>
{
    private readonly IIdentityAdminService _identity;

    public GetRecipientsQueryHandler(IIdentityAdminService identity) => _identity = identity;

    public async Task<IReadOnlyList<AdminRecipientDto>> Handle(GetRecipientsQuery request, CancellationToken cancellationToken)
    {
        var trainers = await _identity.GetUsersInRoleAsync(Roles.Trainer, cancellationToken);
        var clients = await _identity.GetUsersInRoleAsync(Roles.Client, cancellationToken);

        var result = new List<AdminRecipientDto>(trainers.Count + clients.Count);

        result.AddRange(trainers.Select(t => new AdminRecipientDto
        {
            Id = t.Id,
            Email = t.Email,
            FullName = t.FullName,
            Role = Roles.Trainer,
            HasTrainer = true
        }));

        result.AddRange(clients.Select(c => new AdminRecipientDto
        {
            Id = c.Id,
            Email = c.Email,
            FullName = c.FullName,
            Role = Roles.Client,
            HasTrainer = c.TrainerId != null
        }));

        return result;
    }
}
