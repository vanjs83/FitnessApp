using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Groups;
using FitnessApp.Domain.Entities;
using FluentAssertions;
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

        var response = await trainerB.PostAsJsonAsync($"/api/v1/groups/{group!.Id}/sessions",
            new CreateGroupSessionRequest { StartsAt = Tomorrow() });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_trainer_books_a_group_session_and_it_appears_in_the_list()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "HIIT", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var session = await trainer.PostAsJsonAsync($"/api/v1/groups/{group!.Id}/sessions",
            new CreateGroupSessionRequest { StartsAt = Tomorrow(10), DurationMinutes = 45, Type = AppointmentType.Online });
        session.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await session.Content.ReadFromJsonAsync<GroupSessionDto>(JsonOptions);
        created!.Id.Should().BeGreaterThan(0);
        created.GroupName.Should().Be("HIIT");
        created.MemberCount.Should().Be(1);
        created.Status.Should().Be("Scheduled");
        created.Type.Should().Be("Online");

        var list = await trainer.GetFromJsonAsync<List<GroupSessionDto>>("/api/v1/groups/sessions", JsonOptions);
        list!.Should().ContainSingle(s => s.Id == created.Id);
    }

    [Fact]
    public async Task Booking_a_group_session_in_the_past_returns_400()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/groups",
            new CreateGroupRequest { Name = "Past", ClientIds = new() { clientId } });
        var group = await create.Content.ReadFromJsonAsync<TrainingGroupDto>(JsonOptions);

        var response = await trainer.PostAsJsonAsync($"/api/v1/groups/{group!.Id}/sessions",
            new CreateGroupSessionRequest { StartsAt = DateTime.UtcNow.Date.AddDays(-1) });

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

        var first = await trainer.PostAsJsonAsync($"/api/v1/groups/{group!.Id}/sessions",
            new CreateGroupSessionRequest { StartsAt = slot, DurationMinutes = 60 });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var clash = await trainer.PostAsJsonAsync($"/api/v1/groups/{group.Id}/sessions",
            new CreateGroupSessionRequest { StartsAt = slot.AddMinutes(30), DurationMinutes = 60 });
        clash.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
