using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Appointments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Appointments.Queries;

public record GetAppointmentByIdQuery(int Id) : IRequest<Result<AppointmentDto>>;

public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, Result<AppointmentDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetAppointmentByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<AppointmentDto>> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (appointment == null) return Result<AppointmentDto>.NotFound();
        if (appointment.TrainerId != userId && appointment.ClientId != userId)
            return Result<AppointmentDto>.Forbidden();

        return Result<AppointmentDto>.Success(await AppointmentResolver.ToDtoAsync(appointment, userId, _users, cancellationToken));
    }
}
