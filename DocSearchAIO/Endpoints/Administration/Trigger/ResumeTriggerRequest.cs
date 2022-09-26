using System.Text.Json.Serialization;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Trigger;

[Record]
public sealed record ResumeTriggerRequest([property: JsonPropertyName("triggerId")] string TriggerId,
    [property: JsonPropertyName("groupId")] string GroupId);