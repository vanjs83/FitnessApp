using FitnessApp.Application.DTOs.Email;

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

/// <summary>Trainer broadcasts a message to all members of a group over the chosen channels.</summary>
public class SendMessageToGroupRequest
{
    public string Subject { get; set; } = string.Empty; // email subject / push title
    public string Body { get; set; } = string.Empty;
    public bool Email { get; set; } = true;
    public bool Push { get; set; } = true;
}

/// <summary>Per-channel delivery outcome; a channel is null when it wasn't requested.</summary>
public class GroupMessageResultDto
{
    public MessageSendResultDto? Email { get; set; }
    public MessageSendResultDto? Push { get; set; }
}
