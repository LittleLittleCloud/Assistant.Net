// Copyright (ic) LittleLittleCloud. Some rights reserved.
// DotnetCoder.cs

using System.Text.Json;
using System.Text.Json.Serialization;
using AutoGen;
using Json.Schema.Generation;

namespace Assistant.Core.Agent;

public struct ReviewResult
{
    [Description("review result, must be APPROVE or REJECT")]
    [JsonPropertyName("result")]
    public string Result { get; set; }

    [Description("the reason why you reject the code. You don't need to provide reason if you approve the code.")]
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class DotnetCoder : IAgent
{
    private readonly IAgent _coderAgent;
    private readonly IAgent _reviewerAgent;
    private readonly int _maxRetry = 3;

    private readonly string _coderSystemMessage = """
        You act as dotnet coder, you write dotnet code to resolve task. Once you finish writing code, ask runner to run the code for you.
        
        Here're some rules to follow on writing dotnet code:
        - put code between ```csharp and ```
        - When creating http client, use `var httpClient = new HttpClient()`. Don't use `using var httpClient = new HttpClient()` because it will cause error when running the code.
        - Try to use `var` instead of explicit type.
        - Try avoid using external library, use .NET Core library instead.
        - Use top level statement to write code.
        - Always print out the result to console. Don't write code that doesn't print out anything.
        
        If you need to install nuget packages, put nuget packages in the following format:
        ```nuget
        nuget_package_name
        ```
        
        If your code is incorrect, Fix the error and send the code again.
        """;

    private readonly string _reviewerSystemMessage = """
        You are a code reviewer who reviews code from coder. You need to check if the code satisfy the following conditions:
        - The reply from coder contains at least one code block, e.g ```csharp and ```
        - There's only one code block and it's csharp code block
        - The code block is not inside a main function. a.k.a top level statement
        - The code block is not using declaration when creating http client
        
        You don't check the code style, only check if the code satisfy the above conditions.

        Your reply needs to be a json object which contains the following fields:
        - "result": APPROVE or REJECT
        - "reason": the reason why you reject the code. You don't need to provide reason if you approve the code.

        Here are a few examples of the reply:
        Example 1:
        {
            "result": "APPROVE"
        }

        Example 2:
        {
            "result": "REJECT",
            "reason": "The code should have exactly one dotnet code block, but found 2"
        }
        """;
    public DotnetCoder(string name, ILLMFactory llmFactory, int maxRetry = 3)
    {
        _coderAgent = llmFactory.Create(name, _coderSystemMessage);
        _reviewerAgent = llmFactory.Create("_reviewer", _reviewerSystemMessage)
            .FormatAsJsonAsync<ReviewResult>();
        this._maxRetry = maxRetry;
    }

    public string? Name => _coderAgent.Name;

    public async Task<Message> GenerateReplyAsync(IEnumerable<Message> messages, GenerateReplyOptions? options = null, CancellationToken cancellationToken = default)
    {
        var maxRetry = _maxRetry;
        while (maxRetry > 0)
        {
            var reply = await _coderAgent.GenerateReplyAsync(messages, options, cancellationToken);
            var review = await _reviewerAgent.SendAsync(reply, ct: cancellationToken);
            var jsonResult = JsonSerializer.Deserialize<ReviewResult>(review.Content!);
            if (jsonResult.Result == "APPROVE")
            {
                return reply;
            }
            else
            {
                messages = messages.Append(review);
                maxRetry--;
            }
        }

        throw new JsonException("Failed to get approved code");
    }
}
