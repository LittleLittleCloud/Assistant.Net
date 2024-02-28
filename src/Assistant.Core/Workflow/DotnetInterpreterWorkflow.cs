// Copyright (ic) LittleLittleCloud. Some rights reserved.
// DotnetInterpreterWorkflow.cs

using System.Text.Json;
using Assistant.Core.Agent;
using AutoGen;
namespace Assistant.Core.Workflow;

public interface IAssistantWorkflow : IAgent
{
}

public class CodeInterpreterWorkflow : IAgent
{
    private readonly IAgent _plannerAgent;
    private readonly IAgent _runnerAgent;
    private readonly IAgent _codeAgent;
    private readonly IAgent _userAgent;
    private readonly int _maxRound;

    public static readonly Step NeedInfo = new Step
    {
        Name = "NeedInfo",
        Description = "Ask for more information when details are not enough or unclear",
    };

    public static readonly Step Approval = new Step
    {
        Name = "Approval",
        Description = "Ask for approval before running code",
    };

    public static readonly Step WriteCode = new Step
    {
        Name = "WriteCode",
        Description = "Write code to resolve the task",
    };

    public static readonly Step RunCode = new Step
    {
        Name = "RunCode",
        Description = "Run the code when the code is available",
    };

    public static readonly Step FixError = new Step
    {
        Name = "FixError",
        Description = "Fix the error in the code",
    };

    public static readonly Step Succeed = new Step
    {
        Name = "Succeed",
        Description = "The task has been resolved",
    };

    public static readonly Step Fail = new Step
    {
        Name = "Fail",
        Description = "The task has not been resolved",
    };

    private bool hasCoderBeenInvolved = false;

    public string? Name { get; }

    public static IEnumerable<Step> Steps => new[]
    {
        NeedInfo,
        Approval,
        WriteCode,
        RunCode,
        FixError,
        Succeed,
    };

    public static IEnumerable<Message> CreateFewshotExampleMessagesForPlanner()
    {
        var writeCodeMessage = new Message(role: Role.User, "What's the 100th prime number?");
        var writeCodeStep = new Step
        {
            Name = WriteCode.Name,
            Description = "Write code to solve the problem",
            Argument = "task: What's the 100th prime number?",
            Reason = "The user has asked for the 100th prime number",
        };
        var writeCodeStepMessage = CreateMessageFromStep(writeCodeStep);

        var runCodeMessage = new Message(role: Role.User, """
            ```csharp
            Console.WriteLine("Hello, World!");
            ```

            The code writes "Hello, World!" to the console
            """);

        var runCodeStep = new Step
        {
            Name = RunCode.Name,
            Description = "Run the code",
            Reason = "The code is available",
        };
        var runCodeStepMessage = CreateMessageFromStep(runCodeStep);

        var codeResultMessage = new Message(role: Role.User, "### Output\n\nHello, World!");
        var codeResultStep = new Step
        {
            Name = Succeed.Name,
            Description = "The task has been resolved",
            Reason = "The code has been run successfully",
        };
        var codeResultStepMessage = CreateMessageFromStep(codeResultStep);

        var needInfoMessage = new Message(role: Role.User, "hey, help me create a folder");
        var needInfoStep = new Step
        {
            Name = NeedInfo.Name,
            Argument = "ask user: what's the name of the folder?",
            Reason = "The user has asked for help to create a folder but the folder name is not provided",
        };
        var needInfoStepMessage = CreateMessageFromStep(needInfoStep);

        var needAuthorizeMessage = new Message(role: Role.User, "### Output\n\n 403 Forbidden");
        var needAuthorizenStep = new Step
        {
            Name = NeedInfo.Name,
            Argument = "ask user: please provide necessary information for authorization",
            Reason = "The code returns 403 Forbidden, which means the user needs to provide necessary information for authorization",
        };
        var needAuthorizeStepMessage = CreateMessageFromStep(needAuthorizenStep);
        return [
            needInfoMessage,
            needInfoStepMessage,
            writeCodeMessage,
            writeCodeStepMessage,
            runCodeMessage,
            runCodeStepMessage,
            needAuthorizeMessage,
            needAuthorizeStepMessage,
            codeResultMessage,
            codeResultStepMessage,
            ];
    }

    private static Message CreateMessageFromStep(Step step)
    {
        var stepJson = JsonSerializer.Serialize(step);
        return new Message(role: Role.Assistant, content: stepJson);
    }

    public CodeInterpreterWorkflow(
        string name,
        IAgent plannerAgent,
        IAgent runnerAgent,
        IAgent codeAgent,
        IAgent userAgent,
        int maxRound = 10)
    {
        Name = name;
        _plannerAgent = plannerAgent;
        _runnerAgent = runnerAgent;
        _codeAgent = codeAgent;
        _userAgent = userAgent;
        _maxRound = maxRound;
    }

    public async Task<Message> GenerateReplyAsync(
        IEnumerable<Message> messages,
        GenerateReplyOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        hasCoderBeenInvolved = false;
        Step currentStep = default;
        var runnerAgent = _runnerAgent.RegisterMiddleware(async (messages, option, innerAgent, ct) =>
        {
            if (hasCoderBeenInvolved)
            {
                var lastCoderMessage = messages.Last(m => m.From == _codeAgent.Name);

                hasCoderBeenInvolved = false;
                return await innerAgent.GenerateReplyAsync(new[] { lastCoderMessage }, option, ct);
            }

            throw new InvalidOperationException("runner agent should not be called without coder agent");
        });

        var coderAgent = _codeAgent.RegisterMiddleware(async (messages, option, innerAgent, ct) =>
        {
            var reply = await innerAgent.GenerateReplyAsync(messages, option, ct);
            hasCoderBeenInvolved = true;
            return reply;
        });

        var userToRunnerTransition = Transition.Create(this._userAgent, runnerAgent, async (from, to, messages) =>
        {
            return currentStep.Name == RunCode.Name && hasCoderBeenInvolved;
        });

        var coderToRunnerTransition = Transition.Create(coderAgent, runnerAgent, async (from, to, messages) =>
        {
            return currentStep.Name == RunCode.Name;
        });

        var userToCoderTransition = Transition.Create(this._userAgent, coderAgent, async (from, to, messages) =>
        {
            return currentStep.Name == WriteCode.Name || currentStep.Name == FixError.Name;
        });

        var runnerToCoderTransition = Transition.Create(runnerAgent, coderAgent, async (from, to, messages) =>
        {
            return currentStep.Name == FixError.Name;
        });

        var userToUserTransition = Transition.Create(this._userAgent, this._userAgent, async (from, to, messages) =>
        {
            return currentStep.Name == NeedInfo.Name;
        });
        var coderToUserTransition = Transition.Create(coderAgent, this._userAgent, async (from, to, messages) =>
        {
            return currentStep.Name == NeedInfo.Name;
        });

        var runnerToUserTransition = Transition.Create(runnerAgent, this._userAgent, async (from, to, messages) =>
        {
            return currentStep.Name == Approval.Name || currentStep.Name == NeedInfo.Name;
        });

        var workflow = new AutoGen.Workflow(
            transitions: [
                coderToRunnerTransition,
                userToCoderTransition,
                runnerToCoderTransition,
                userToUserTransition,
                coderToUserTransition,
                runnerToUserTransition,
                ]
            );

        var groupChat = new GroupChat(
            members: [
                this._userAgent,
                coderAgent,
                runnerAgent,
            ],
            admin: null,
            workflow: workflow);

        var chatHistory = messages;
        var examplars = CreateFewshotExampleMessagesForPlanner();
        var previousStepMessages = new List<Message>();
        for (int i = 0; i < _maxRound; i++)
        {
            var lastMessage = chatHistory.Last();
            var chatHistoryToPlanner = examplars.Concat(previousStepMessages).Concat([lastMessage]).TakeLast(5);
            var plannerReply = await _plannerAgent.SendAsync(chatHistory: chatHistoryToPlanner);
            currentStep = JsonSerializer.Deserialize<Step>(plannerReply.Content ?? throw new ArgumentNullException("planner reply content is null"));
            // check exit condition
            if (currentStep.Name == Succeed.Name || currentStep.Name == Fail.Name)
            {
                plannerReply.From = this.Name;
                return plannerReply;
            }

            var reply = await groupChat.CallAsync(chatHistory, maxRound: 1, cancellationToken) ?? throw new ArgumentNullException("reply is null");
            chatHistory = reply;
            previousStepMessages.Add(lastMessage);
            previousStepMessages.Add(CreateMessageFromStep(currentStep));
        }

        var failStep = new Step
        {
            Name = Fail.Name,
            Reason = "The dotnet interpreter workflow has reached the maximum round, but the task has not been resolved.",
        };
        var failStepJson = JsonSerializer.Serialize(failStep);
        return new Message(role: Role.Assistant, from: this.Name, content: failStepJson);
    }
}
