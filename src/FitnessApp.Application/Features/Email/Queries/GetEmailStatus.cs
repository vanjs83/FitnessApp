using FitnessApp.Application.DTOs.Email;
using FitnessApp.Application.Interfaces;
using MediatR;

namespace FitnessApp.Application.Features.Email.Queries;

public record GetEmailStatusQuery : IRequest<EmailStatusDto>;

public class GetEmailStatusQueryHandler : IRequestHandler<GetEmailStatusQuery, EmailStatusDto>
{
    private readonly IEmailService _email;

    public GetEmailStatusQueryHandler(IEmailService email) => _email = email;

    public Task<EmailStatusDto> Handle(GetEmailStatusQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new EmailStatusDto { Configured = _email.IsConfigured });
}
