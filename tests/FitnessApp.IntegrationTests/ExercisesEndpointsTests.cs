using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Exercises;
using FluentAssertions;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class ExercisesEndpointsTests : IntegrationTestBase
{
    public ExercisesEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Getting_exercises_without_a_token_returns_401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/exercises");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Creating_an_exercise_returns_201_and_it_appears_in_the_list()
    {
        var client = await CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/exercises", new CreateExerciseRequest
        {
            Name = "Bench Press",
            MuscleGroup = "Chest"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await create.Content.ReadFromJsonAsync<ExerciseDto>(JsonOptions);
        created.Should().NotBeNull();
        created!.Id.Should().BeGreaterThan(0);
        created.Name.Should().Be("Bench Press");

        var list = await client.GetFromJsonAsync<List<ExerciseDto>>("/api/exercises", JsonOptions);
        list.Should().NotBeNull();
        list!.Should().ContainSingle(e => e.Id == created.Id && e.Name == "Bench Press");
    }

    [Fact]
    public async Task Creating_an_exercise_with_an_empty_name_returns_400()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/exercises", new CreateExerciseRequest { Name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Full_crud_lifecycle_create_read_update_delete()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Create
        var create = await client.PostAsJsonAsync("/api/exercises", new CreateExerciseRequest
        {
            Name = "Squat",
            MuscleGroup = "Legs"
        });
        var created = await create.Content.ReadFromJsonAsync<ExerciseDto>(JsonOptions);
        var id = created!.Id;

        // Read by id
        var getById = await client.GetAsync($"/api/exercises/{id}");
        getById.StatusCode.Should().Be(HttpStatusCode.OK);

        // Update
        var update = await client.PutAsJsonAsync($"/api/exercises/{id}", new UpdateExerciseRequest
        {
            Name = "Back Squat",
            MuscleGroup = "Legs"
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<ExerciseDto>(JsonOptions);
        updated!.Name.Should().Be("Back Squat");

        // Delete
        var delete = await client.DeleteAsync($"/api/exercises/{id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Gone
        var getAgain = await client.GetAsync($"/api/exercises/{id}");
        getAgain.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
