namespace FitnessApp.Application.DTOs.Groups;

public class TrainingGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public IReadOnlyList<GroupMemberDto> Members { get; set; } = new List<GroupMemberDto>();
}

public class GroupMemberDto
{
    public string ClientId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

/// <summary>Trainer creates a group with an initial (possibly empty) member list.</summary>
public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> ClientIds { get; set; } = new();
}

public class AddGroupMemberRequest
{
    public string ClientId { get; set; } = string.Empty;
}
