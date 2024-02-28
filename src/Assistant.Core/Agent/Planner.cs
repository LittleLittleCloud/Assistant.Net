// Copyright (ic) LittleLittleCloud. Some rights reserved.
// PlannerAgent.cs

using System.Text;
using System.Text.Json.Serialization;
using AutoGen;
using Json.Schema.Generation;

namespace Assistant.Core.Agent;

public struct Step
{
    [JsonPropertyName("name")]
    [Description("the name of the step")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    [Description("the description of the step")]
    public string Description { get; set; }

    [JsonPropertyName("argument")]
    [Description("the argument of the step")]
    public string? Argument { get; set; }

    [JsonPropertyName("reason")]
    [Description("the brief reason of why you create step")]
    public string? Reason { get; set; }
}

public class Planner : IAgent
{
    private readonly IAgent _innerAgent;
    private readonly IAgent _formatAgent;
    private readonly IEnumerable<Step> _steps;

    public Planner(
        string name,
        ILLMFactory factory,
        IEnumerable<Step> steps,
        int maxRetry = 3)
    {
        this.Name = name;
        this._steps = steps;
        this._formatAgent = factory.Create(name, "You are a helpful AI assistant.");
        _innerAgent = factory.Create(name, this.CreatePrompt(steps))
            .FormatAsJsonAsync<Step>(maxRetry, this._formatAgent);
    }

    public string? Name { get; }

    public IEnumerable<Step> Steps => _steps;

    public async Task<Message> GenerateReplyAsync(IEnumerable<Message> messages, GenerateReplyOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await _innerAgent.GenerateReplyAsync(messages, options, cancellationToken);
    }

    private string CreatePrompt(IEnumerable<Step> steps)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are responsible for returning the next step from the available steps below.");
        sb.AppendLine();
        sb.AppendLine("### Available Steps");

        foreach (var step in steps)
        {
            var stepPrompt = $"- {step.Name}: {step.Description}";
            sb.AppendLine(stepPrompt);
        }

        sb.AppendLine("""
Your response must be a valid Json object that contain the following fields:
- name: the name of the step you choose, which must be one of the available steps
- input: the input of the step
- reason: the brief reason of why you choose the step

Below is an example of the response:
```json
{
    "name": "step1",
    "input": "input for step1",
    "reason": "brief reason"
}
```
""");

        return sb.ToString();
    }
}
