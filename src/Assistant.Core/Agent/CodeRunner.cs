// Copyright (ic) LittleLittleCloud. Some rights reserved.
// CodeRunner.cs

using System.Text;
using System.Text.Json;
using AutoGen;
using AutoGen.DotnetInteractive;

namespace Assistant.Core.Agent;

public class DotnetCodeRunner : IAgent
{
    private readonly IAgent _helperAgent;
    private readonly InteractiveService _service;
    private readonly int _maxLengthToKeep;
    private readonly int _maxRetry;

    private readonly string helperAgentSystemMessage = """
        You are a helpful AI assistant.
        """;

    public DotnetCodeRunner(
        string name,
        InteractiveService service,
        ILLMFactory llmFactory,
        int maxLengthToKeep = 500,
        int maxRetry = 3)
    {
        _helperAgent = llmFactory.Create(name, helperAgentSystemMessage);
        _service = service;
        this.Name = name;
        _maxLengthToKeep = maxLengthToKeep;
        _maxRetry = maxRetry;
    }

    public string? Name { get; }

    public async Task<Message> GenerateReplyAsync(IEnumerable<Message> messages, GenerateReplyOptions? options = null, CancellationToken cancellationToken = default)
    {
        var lastMessage = messages.Last();
        // retrieve nuget packages from ```nuget``` block if exists
        var retrieveNugetPrompt = $"""
            If there's a ```nuget``` block, retrieve the nuget packages from the original reply below and return the packages as a json list.
            Otherwise, return an empty list.

            ```original reply
            {lastMessage.Content}
            ```

            Your reply should be a json string list where each element is a nuget package name.

            For example:
            ["Newtonsoft.Json", "System.Text.Json"]
            """;
        var outputStringBuilder = new StringBuilder();
        var reply = await _helperAgent
            .FormatAsJsonAsync<List<string>>(this._maxRetry)
            .SendAsync(retrieveNugetPrompt, ct: cancellationToken);

        if (JsonSerializer.Deserialize<List<string>>(reply.Content!) is List<string> nugetPackages && nugetPackages.Count > 0)
        {
            var command = string.Join(Environment.NewLine, nugetPackages.Select(p => $"#r \"nuget:{p}\""));
            var nugetInstallOutput = await _service.SubmitCSharpCodeAsync(command, cancellationToken);
            outputStringBuilder.AppendLine("## Installed Nuget packages");
            foreach (var package in nugetPackages)
            {
                outputStringBuilder.AppendLine($"- {package}");
            }
        }

        var retrieveCodePrompt = $"""
            Retrieve the code from the code snippet in the last message and return the code only. Don't include ```csharp and ``` in your reply.

            ```original reply
            {lastMessage.Content}
            ```
            Your reply should be a string.

            Here're a few examples of your reply:
            # Example 1:
            Console.WriteLine(\"Hello, World!\")

            # Example 2:
            using System;
            Console.WriteLine(\"Hello, World!\")
            """;

        reply = await _helperAgent
            .SendAsync(retrieveCodePrompt, ct: cancellationToken);

        if (reply.Content is string code && !string.IsNullOrWhiteSpace(code))
        {
            // if reply starts with ```csharp, remove it
            if (code.StartsWith("```csharp"))
            {
                code = code.Substring(8);
            }

            // if reply ends with ``` remove it
            if (code.EndsWith("```"))
            {
                code = code.Substring(0, code.Length - 3);
            }
            var result = await _service.SubmitCSharpCodeAsync(code, cancellationToken) ?? throw new Exception("Failed to run the code");
            outputStringBuilder.AppendLine("## Output from running the code:");
            if (result.Length > this._maxLengthToKeep)
            {
                result = result.Substring(0, this._maxLengthToKeep);
                outputStringBuilder.AppendLine("## Output is too long, only the first 500 characters are shown:##");
            }
            outputStringBuilder.AppendLine(result);
        }
        else
        {
            outputStringBuilder.AppendLine("No code snippet found");
        }

        return new Message(role: Role.Assistant, from: this.Name, content: outputStringBuilder.ToString());
    }
}
