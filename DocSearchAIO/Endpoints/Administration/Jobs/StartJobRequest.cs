using System.Text.Json.Serialization;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Jobs;

[Record]
public sealed record StartJobRequest([property: JsonPropertyName("jobName")] string JobName,
    [property: JsonPropertyName("groupId")] string GroupId);