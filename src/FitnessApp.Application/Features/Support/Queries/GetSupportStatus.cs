using FitnessApp.Application.DTOs.Support;
using FitnessApp.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace FitnessApp.Application.Features.Support.Queries;

public record GetSupportStatusQuery : IRequest<SupportStatusDto>;

public class GetSupportStatusQueryHandler : IRequestHandler<GetSupportStatusQuery, SupportStatusDto>
{
    private readonly IEmailService _email;
    private readonly IConfiguration _config;

    public GetSupportStatusQueryHandler(IEmailService email, IConfiguration config)
    {
        _email = email;
        _config = config;
    }

    public Task<SupportStatusDto> Handle(GetSupportStatusQuery request, CancellationToken cancellationToken)
    {
        var configured = _email.IsConfigured && !string.IsNullOrWhiteSpace(_config["Smtp:FromEmail"]);
        return Task.FromResult(new SupportStatusDto { Configured = configured });
    }
}
