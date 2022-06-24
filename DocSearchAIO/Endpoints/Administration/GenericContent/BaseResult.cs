using System.Text.Json.Serialization;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.GenericContent;

[Record]
public record BaseResult([property: JsonPropertyName("result")] bool Result);

[Record]
public record SetGenericContentResult(bool Result) : BaseResult(Result)
{
    public static implicit operator SetGenericContentResult(bool result) => new(result);
}

[Record]
public record PauseTriggerResult(bool Result) : BaseResult(Result)
{
    public static implicit operator PauseTriggerResult(bool result) => new(result);
}

[Record]
public record ResumeTriggerResult(bool Result) : BaseResult(Result)
{
    public static implicit operator ResumeTriggerResult(bool result) => new(result);
}

[Record]
public record TriggerStatusResult(string Result)
{
    public static implicit operator TriggerStatusResult(string result) => new(result);
}

[Record]
public record ReindexAndStartJobResult(bool Result) : BaseResult(Result)
{
    public static implicit operator ReindexAndStartJobResult(bool result) => new(result);
}

[Record]
public record StartJobResult(bool Result) : BaseResult(Result)
{
    public static implicit operator StartJobResult(bool result) => new(result);
}