using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Application.DTOs.Groups;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class GroupsEndpointsTests : IntegrationTestBase
{
    public GroupsEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    private static DateTime Tomorrow(int hour = 9) => DateTime.UtcNow.Date.AddDays(1).AddHours(hour);

    [Fact]
    public async Task Listing_groups_without_a_token_returns_401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/groups");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_client_cannot_access_groups_returns_403()
    {
        var (client, _, _) = await RegisterUserAsync("Client");

        var response = await client.GetAsync("/api/v1/groups");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_trainer_creates_a_group_with_their_client()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Bootcamp", ClientIds = new() { clientId } });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);
        group!.Id.Should().BeGreaterThan(0);
        group.Name.Should().Be("Bootcamp");
        group.MemberCount.Should().Be(1);
        group.Members.Should().ContainSingle(m => m.ClientId == clientId);

        var list = await trainer.GetFromJsonAsync<List<TrainingGroupDto>>("/api/v1/groups", JsonOptions);
        list!.Should().ContainSingle(g => g.Id == group.Id);
    }

    [Fact]
    public async Task Creating_a_group_with_a_client_who_is_not_yours_returns_400()
    {
        var trainer = await CreateAuthenticatedClientAsync("Trainer");

        var response = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Strangers", ClientIds = new() { Guid.NewGuid().ToString() } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_trainer_adds_and_removes_a_member()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Morning", ClientIds = new() });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var add = await trainer.PostAsJsonAsync($"/api/v1/groups/{group!.Id}/members",
            new AddGroupMemberRequest { ClientId = clientId });
        add.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterAdd = await add.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);
        afterAdd!.Members.Should().ContainSingle(m => m.ClientId == clientId);

        var remove = await trainer.DeleteAsync($"/api/v1/groups/{group.Id}/members/{clientId}");
        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await trainer.GetFromJsonAsync<List<TrainingGroupDto>>("/api/v1/groups", JsonOptions);
        list!.Single(g => g.Id == group.Id).MemberCount.Should().Be(0);
    }

    [Fact]
    public async Task A_trainer_cannot_book_a_session_for_another_trainers_group_returns_403()
    {
        var (_, clientId, trainerA, _) = await CreateLinkedClientAndTrainerAsync();
        var trainerB = await CreateAuthenticatedClientAsync("Trainer");

        var create = await trainerA.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "A's group", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var response = await trainerB.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { GroupId = group!.Id, StartsAt = Tomorrow() });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_trainer_books_a_group_session_and_it_appears_in_the_list()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "HIIT", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var session = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { GroupId = group!.Id, StartsAt = Tomorrow(10), DurationMinutes = 45, Type = AppointmentType.Online });
        session.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await session.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);
        created!.Id.Should().BeGreaterThan(0);
        created.IsGroup.Should().BeTrue();
        created.GroupName.Should().Be("HIIT");
        created.MemberCount.Should().Be(1);
        created.Status.Should().Be("Scheduled");
        created.Type.Should().Be("Online");

        // Group sessions surface through the normal calendar feed now.
        var list = await trainer.GetFromJsonAsync<List<AppointmentDto>>("/api/v1/appointments", JsonOptions);
        list!.Should().ContainSingle(s => s.Id == created.Id && s.IsGroup);
    }

    [Fact]
    public async Task Booking_a_group_session_in_the_past_returns_400()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Past", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var response = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { GroupId = group!.Id, StartsAt = DateTime.UtcNow.Date.AddDays(-1) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Two_overlapping_group_sessions_are_rejected_returns_409()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();
        var slot = Tomorrow(14);

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Clash", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var first = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { GroupId = group!.Id, StartsAt = slot, DurationMinutes = 60 });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var clash = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { GroupId = group.Id, StartsAt = slot.AddMinutes(30), DurationMinutes = 60 });
        clash.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ===== Group sessions in the unified calendar feed (GET /appointments, both roles) =====

    [Fact]
    public async Task A_group_member_sees_the_group_session_in_their_calendar_feed()
    {
        var (client, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Calendar group", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var session = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { GroupId = group!.Id, StartsAt = Tomorrow(10) });
        var created = await session.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);

        // The client (a member) reads it via the normal appointments feed the calendar uses.
        var list = await client.GetFromJsonAsync<List<AppointmentDto>>("/api/v1/appointments", JsonOptions);
        list!.Should().ContainSingle(s => s.Id == created!.Id && s.IsGroup && s.GroupName == "Calendar group");
    }

    [Fact]
    public async Task A_client_who_is_not_a_member_does_not_see_the_group_session()
    {
        var (_, memberId, trainer, _) = await CreateLinkedClientAndTrainerAsync();
        var (outsider, _, _) = await RegisterUserAsync("Client");

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Private group", ClientIds = new() { memberId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var session = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { GroupId = group!.Id, StartsAt = Tomorrow(11) });
        var created = await session.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);

        var list = await outsider.GetFromJsonAsync<List<AppointmentDto>>("/api/v1/appointments", JsonOptions);
        list!.Should().NotContain(s => s.Id == created!.Id);
    }

    [Fact]
    public async Task A_client_cannot_book_a_group_session_returns_403()
    {
        var (client, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Trainer only booking", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var response = await client.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { GroupId = group!.Id, StartsAt = Tomorrow() });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ===== Messaging a group (POST /groups/{id}/message — email and/or push to all members) =====

    [Fact]
    public async Task A_trainer_messages_their_group_and_it_reaches_the_members()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Broadcast", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var response = await trainer.PostAsJsonAsync($"/api/v1/groups/{group!.Id}/message",
            new SendMessageToGroupRequest { Subject = "Session moved", Body = "We start at 8.", Email = true, Push = true });

        // The send is queued into the outbox and accepted (204); with the inline scheduler the due-scan
        // runs during the request, so the rows are already dispatched by the time we inspect them.
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messages = db.ScheduledMessages.Where(m => m.GroupId == group.Id).ToList();

        // One row per selected channel, both targeting the group.
        messages.Should().HaveCount(2);
        messages.Should().OnlyContain(m => m.Audience == ScheduledMessageAudience.Group);
        // The email channel delivers via the no-op FakeEmailService, so it lands as Sent.
        messages.Should().Contain(m => m.Channel == ScheduledMessageChannel.Email && m.Status == ScheduledMessageStatus.Sent);
    }

    [Fact]
    public async Task Messaging_a_group_with_no_channel_selected_returns_400()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "No channel", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var response = await trainer.PostAsJsonAsync($"/api/v1/groups/{group!.Id}/message",
            new SendMessageToGroupRequest { Subject = "Hi", Body = "There", Email = false, Push = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Messaging_an_empty_group_returns_400()
    {
        var trainer = await CreateAuthenticatedClientAsync("Trainer");

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Empty", ClientIds = new() });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var response = await trainer.PostAsJsonAsync($"/api/v1/groups/{group!.Id}/message",
            new SendMessageToGroupRequest { Subject = "Anyone?", Body = "Hello", Push = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_trainer_cannot_message_another_trainers_group_returns_403()
    {
        var (_, clientId, trainerA, _) = await CreateLinkedClientAndTrainerAsync();
        var trainerB = await CreateAuthenticatedClientAsync("Trainer");

        var create = await trainerA.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "A's only", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var response = await trainerB.PostAsJsonAsync($"/api/v1/groups/{group!.Id}/message",
            new SendMessageToGroupRequest { Subject = "Sneaky", Body = "Hi", Push = true });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_client_cannot_message_a_group_returns_403()
    {
        var (client, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Trainer only msg", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var response = await client.PostAsJsonAsync($"/api/v1/groups/{group!.Id}/message",
            new SendMessageToGroupRequest { Subject = "Nope", Body = "Hi", Push = true });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ===== Group session attendance (POST/DELETE /appointments/{id}/attend) =====

    /// <summary>Books a future group session for the given trainer/group and returns its id.</summary>
    private async Task<int> BookGroupSessionAsync(HttpClient trainer, int groupId, int hour = 10)
    {
        var session = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { GroupId = groupId, StartsAt = Tomorrow(hour) });
        session.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await session.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);
        return dto!.Id;
    }

    [Fact]
    public async Task A_member_confirms_attendance_and_the_trainer_sees_the_count()
    {
        var (client, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "RSVP", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);
        var sessionId = await BookGroupSessionAsync(trainer, group!.Id);

        var attend = await client.PostAsync($"/api/v1/appointments/{sessionId}/attend", null);
        attend.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Trainer sees one confirmed attendee out of one member.
        var trainerFeed = await trainer.GetFromJsonAsync<List<AppointmentDto>>("/api/v1/appointments", JsonOptions);
        var asTrainer = trainerFeed!.Single(a => a.Id == sessionId);
        asTrainer.ConfirmedCount.Should().Be(1);
        asTrainer.MemberCount.Should().Be(1);

        // The member sees their own confirmed state.
        var clientFeed = await client.GetFromJsonAsync<List<AppointmentDto>>("/api/v1/appointments", JsonOptions);
        clientFeed!.Single(a => a.Id == sessionId).IsAttending.Should().BeTrue();
    }

    [Fact]
    public async Task Confirming_attendance_twice_is_idempotent()
    {
        var (client, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Twice", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);
        var sessionId = await BookGroupSessionAsync(trainer, group!.Id);

        (await client.PostAsync($"/api/v1/appointments/{sessionId}/attend", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/v1/appointments/{sessionId}/attend", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var feed = await trainer.GetFromJsonAsync<List<AppointmentDto>>("/api/v1/appointments", JsonOptions);
        feed!.Single(a => a.Id == sessionId).ConfirmedCount.Should().Be(1);
    }

    [Fact]
    public async Task A_member_withdraws_attendance_and_the_count_drops()
    {
        var (client, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Withdraw", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);
        var sessionId = await BookGroupSessionAsync(trainer, group!.Id);

        await client.PostAsync($"/api/v1/appointments/{sessionId}/attend", null);
        var withdraw = await client.DeleteAsync($"/api/v1/appointments/{sessionId}/attend");
        withdraw.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var feed = await trainer.GetFromJsonAsync<List<AppointmentDto>>("/api/v1/appointments", JsonOptions);
        feed!.Single(a => a.Id == sessionId).ConfirmedCount.Should().Be(0);
        (await client.GetFromJsonAsync<List<AppointmentDto>>("/api/v1/appointments", JsonOptions))!
            .Single(a => a.Id == sessionId).IsAttending.Should().BeFalse();
    }

    [Fact]
    public async Task A_non_member_cannot_confirm_attendance_returns_403()
    {
        var (_, memberId, trainer, _) = await CreateLinkedClientAndTrainerAsync();
        var (outsider, _, _) = await RegisterUserAsync("Client");

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Members only", ClientIds = new() { memberId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);
        var sessionId = await BookGroupSessionAsync(trainer, group!.Id);

        var response = await outsider.PostAsync($"/api/v1/appointments/{sessionId}/attend", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Inserts a group session that already started, straight through the DbContext —
    /// the API rejects booking in the past, so attendance locking can't be exercised via HTTP.
    /// </summary>
    private async Task<int> SeedStartedGroupSessionAsync(string trainerId, int groupId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var appt = new Appointment
        {
            TrainerId = trainerId,
            GroupId = groupId,
            StartsAt = DateTime.UtcNow.AddMinutes(-30),
            DurationMinutes = 60,
            Status = AppointmentStatus.Scheduled
        };
        db.Appointments.Add(appt);
        await db.SaveChangesAsync();
        return appt.Id;
    }

    [Fact]
    public async Task A_member_cannot_confirm_attendance_once_the_session_has_started()
    {
        var (client, clientId, trainer, trainerId) = await CreateLinkedClientAndTrainerAsync();
        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "In progress", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);
        var sessionId = await SeedStartedGroupSessionAsync(trainerId, group!.Id);

        var response = await client.PostAsync($"/api/v1/appointments/{sessionId}/attend", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_member_cannot_withdraw_attendance_once_the_session_has_started()
    {
        var (client, clientId, trainer, trainerId) = await CreateLinkedClientAndTrainerAsync();
        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Locked", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);
        var sessionId = await SeedStartedGroupSessionAsync(trainerId, group!.Id);

        var response = await client.DeleteAsync($"/api/v1/appointments/{sessionId}/attend");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
