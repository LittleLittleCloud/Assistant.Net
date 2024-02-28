// Copyright (ic) LittleLittleCloud. Some rights reserved.
// CodeInterpreterWorkflowTest.cs

using System.Text.Json;
using Assistant.Core.Agent;
using Assistant.Core.Workflow;
using AutoGen;
using AutoGen.DotnetInteractive;
using FluentAssertions;
using Xunit.Abstractions;

namespace Assistant.Core.Test;

public class CodeInterpreterWorkflowTest
{
    private readonly ITestOutputHelper _output;

    public CodeInterpreterWorkflowTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [ApiKeyFact("AZURE_OPENAI_API_KEY", "AZURE_OPENAI_ENDPOINT")]
    public async Task CodeInterpreterBasicTestAsync()
    {
        var gpt35Factory = Utils.CreateAzureOpenAIGPT35Factory();
        var gpt4Factory = Utils.CreateAzureOpenAIGPT4Factory();
        var planner = new Planner("planner", gpt4Factory, CodeInterpreterWorkflow.Steps)
            .WriteToTestOutput(this._output);
        var dotnetCoder = new DotnetCoder("dotnet-coder", gpt35Factory)
            .WriteToTestOutput(this._output);
        var workingDirectory = Path.Join(Path.GetTempPath(), nameof(CodeRunnerAgentTest));
        if (!Directory.Exists(workingDirectory))
        {
            Directory.CreateDirectory(workingDirectory);
        }

        using var service = new InteractiveService(workingDirectory);
        await service.StartAsync(workingDirectory);
        var dotnetRunner = new DotnetCodeRunner("dotnet-runner", service, gpt35Factory)
            .WriteToTestOutput(this._output);

        var userAgent = gpt4Factory.Create("user", """
            You are the user, you want to use code interpreter to solve your problem.
            If you are asked to approve the code, you will approve it.
            """)
            .WriteToTestOutput(this._output);

        var workflowAgent = new CodeInterpreterWorkflow("code-interpreter", planner, dotnetRunner, dotnetCoder, userAgent, 10);

        string[] tasks = [
            "Retrieve the most recent PR from MLNet repo",
            "Calculate the 39th Fibonacci",
            "What's 100th prime number?",
            ];

        foreach (var task in tasks)
        {
            Message[] chatHistory = [new Message(Role.User, task, from: "user")];

            var reply = await workflowAgent.GenerateReplyAsync(chatHistory);
            var step = JsonSerializer.Deserialize<Step>(reply.Content!);
            step.Name.Should().Be(CodeInterpreterWorkflow.Succeed.Name);
        }
    }
}
