// Copyright (ic) LittleLittleCloud. Some rights reserved.
// PlannerAgentTest.cs

using System.Text.Json;
using Assistant.Core.Agent;
using Assistant.Core.Workflow;
using AutoGen;
using FluentAssertions;

namespace Assistant.Core.Test;

public class PlannerAgentTest
{
    [ApiKeyFact("AZURE_OPENAI_API_KEY", "AZURE_OPENAI_ENDPOINT")]
    public async Task TestPlannerAsync()
    {
        var llmFactory = Utils.CreateAzureOpenAIGPT35Factory();
        var steps = new[]
        {
            new Step
            {
                Name = "NeedInfo",
                Description = "Ask for more information",
            },
            new Step
            {
                Name = "WriteCode",
                Description = "Write code to solve the problem",
            },
        };

        var planner = new Planner("planner", llmFactory, steps);

        var reply = await planner.SendAsync("What's 100th prime number? Resolve it using C#");
        var step = JsonSerializer.Deserialize<Step>(reply.Content!);
        step.Name.Should().Be("WriteCode");
    }

    [ApiKeyFact("AZURE_OPENAI_API_KEY", "AZURE_OPENAI_ENDPOINT")]
    public async Task CodeInterpreterWorkflowStepTest()
    {
        var llmFactory = Utils.CreateAzureOpenAIGPT4Factory();
        var steps = CodeInterpreterWorkflow.Steps;

        var planner = new Planner("planner", llmFactory, steps)
            .RegisterMiddleware(async (messages, option, next, ct) =>
            {
                var fewshotExamples = CodeInterpreterWorkflow.CreateFewshotExampleMessagesForPlanner();
                var msgs = new List<Message>(fewshotExamples);
                msgs.AddRange(messages);

                var reply = await next.GenerateReplyAsync(msgs, option, ct);
                return reply;
            });


        var reply = await planner.SendAsync("What's 100th prime number? Resolve it using C#");
        var step = JsonSerializer.Deserialize<Step>(reply.Content!);
        step.Name.Should().Be(CodeInterpreterWorkflow.WriteCode.Name);

        reply = await planner.SendAsync("""
            ```csharp
            Console.WriteLine("Hello, World!");
            ```

            The code writes "Hello, World!" to the console
            """);

        step = JsonSerializer.Deserialize<Step>(reply.Content!);
        step.Name.Should().Be(CodeInterpreterWorkflow.RunCode.Name);

        reply = await planner.SendAsync("### Output\n\nHello, World!");
        step = JsonSerializer.Deserialize<Step>(reply.Content!);
        step.Name.Should().Be(CodeInterpreterWorkflow.Succeed.Name);

        reply = await planner.SendAsync("### Error: CS1002\n\n; expected");
        step = JsonSerializer.Deserialize<Step>(reply.Content!);
        step.Name.Should().Be(CodeInterpreterWorkflow.FixError.Name);

        reply = await planner.SendAsync("hey, I have a task to resolve");
        step = JsonSerializer.Deserialize<Step>(reply.Content!);
        step.Name.Should().Be(CodeInterpreterWorkflow.MoreInformation.Name);
    }
}
