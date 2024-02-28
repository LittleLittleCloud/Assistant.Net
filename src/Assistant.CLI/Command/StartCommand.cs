// Copyright (ic) LittleLittleCloud. Some rights reserved.
// StartCommand.cs

using System.ComponentModel;
using Assistant.CLI.Component;
using Assistant.Core;
using Assistant.Core.Agent;
using Assistant.Core.Workflow;
using AutoGen;
using AutoGen.DotnetInteractive;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Assistant.CLI;

public class StartCommand : AsyncCommand<StartCommand.Settings>
{
    private readonly ILogger? _logger;

    public StartCommand(ILogger? logger = null)
        : base()
    {
        _logger = logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        _logger?.Debug($"Loading configuration from {settings.ConfigFullPath}");

        var configuration = new Configuration();
        ILLMFactory llmFactoryForAssistant = GetLLMFactory(configuration.Assistant.ModelType, configuration);

        var userAgent = new UserProxyAgent("user", humanInputMode: ConversableAgent.HumanInputMode.ALWAYS);
        var dotnetCoder = new DotnetCoder(configuration.Assistant.Name, llmFactoryForAssistant)
            .RegisterPrintFormatMessageHook();

        var llmFactoryForCodeInterpreterPlanner = GetLLMFactory(configuration.CodeInterpreter.PlannerModelType, configuration);
        var codeInterpreterPlanner = new Planner("planner", llmFactoryForCodeInterpreterPlanner, CodeInterpreterWorkflow.Steps)
            .RegisterPrintFormatMessageHook();

        var llmFactoryForCodeInterpreterCoder = GetLLMFactory(configuration.CodeInterpreter.CoderModelType, configuration);
        var codeInterpreterCoder = new DotnetCoder("dotnet-coder", llmFactoryForCodeInterpreterCoder)
            .RegisterPrintFormatMessageHook();
        var workingDirectory = configuration.CodeInterpreter.WorkingDirectory;
        if (!System.IO.Directory.Exists(workingDirectory))
        {
            System.IO.Directory.CreateDirectory(workingDirectory);
        }

        using var service = new InteractiveService(workingDirectory);
        await service.StartAsync(workingDirectory);
        var dotnetRunner = new DotnetCodeRunner("dotnet-runner", service, llmFactoryForCodeInterpreterCoder)
            .RegisterPrintFormatMessageHook();
        var codeInterpreterAgent = new CodeInterpreterWorkflow(configuration.CodeInterpreter.Name, codeInterpreterPlanner, dotnetRunner, codeInterpreterCoder, userAgent, configuration.CodeInterpreter.MaxTurn);

        var codeInterpreterAgentToUserTransition = Transition.Create(codeInterpreterAgent, userAgent);
        var userToCodeInterpreterAgentTransition = Transition.Create(userAgent, codeInterpreterAgent);
        var assistantAgentToUserTransition = Transition.Create(dotnetCoder, userAgent);
        var userToAssistantAgentTransition = Transition.Create(userAgent, dotnetCoder);
        var workflow = new Workflow(
            [
                codeInterpreterAgentToUserTransition,
                userToCodeInterpreterAgentTransition,
                assistantAgentToUserTransition,
                userToAssistantAgentTransition,
            ]);

        var groupChatAdminLLMFactory = GetLLMFactory(configuration.GroupChat.ModelType, configuration);
        var groupAdmin = groupChatAdminLLMFactory.Create(configuration.GroupChat.Name, "You are the group admin, you can manage the group chat");
        var groupChat = new GroupChat(
            [
                codeInterpreterAgent,
                dotnetCoder,
                userAgent,
            ],
            admin: groupAdmin,
            workflow: workflow);
        var groupChatManager = new GroupChatManager(groupChat);
        await codeInterpreterAgent.SendAsync(userAgent, "Hello, I'm code interpreter. How can I help you today?", maxRound: configuration.GroupChat.MaxTurn);
        return 0;
    }

    private ILLMFactory GetLLMFactory(LLMModelType type, Configuration configuration)
    {
        return type switch
        {
            LLMModelType.GPT3_5 => new OpenAIGPTFactory(configuration.GPT3_5.ApiKey ?? throw new ArgumentNullException(), configuration.GPT3_5.ModelName!),
            LLMModelType.GPT4 => new OpenAIGPTFactory(configuration.GPT4.ApiKey ?? throw new ArgumentNullException(), configuration.GPT4.ModelName!),
            LLMModelType.AZURE_GPT3_5 => new AzureOpenAIGPTFactory(configuration.AzureGPT3_5?.Endpoint ?? throw new ArgumentNullException(), configuration.AzureGPT3_5.ApiKey ?? throw new ArgumentNullException(), configuration.AzureGPT3_5.DeployName ?? throw new ArgumentNullException()),
            LLMModelType.AZURE_GPT4 => new AzureOpenAIGPTFactory(configuration.AzureGPT4?.Endpoint ?? throw new ArgumentNullException(), configuration.AzureGPT4.ApiKey ?? throw new ArgumentNullException(), configuration.AzureGPT4.DeployName ?? throw new ArgumentNullException()),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public class Settings : CommandSettings
    {
        [Description("path to configuration file, default to config.json")]
        [CommandOption("-c|--config")]
        [DefaultValue("config.json")]
        public string ConfigPath { get; set; } = "config.json";

        [Description("log level, default to info, available options: debug, info")]
        [CommandOption("-l|--log-level")]
        [DefaultValue("info")]
        public string LogLevel { get; set; } = "info";

        public string ConfigFullPath => System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ConfigPath);

        public override ValidationResult Validate()
        {
            if (LogLevel != "info" && LogLevel != "debug")
            {
                return ValidationResult.Error("Invalid log level");
            }

            //var cwd = System.IO.Directory.GetCurrentDirectory();
            //if (!System.IO.File.Exists(System.IO.Path.Combine(cwd, ConfigPath)))
            //{
            //    return ValidationResult.Error("Config file not found");
            //}

            return ValidationResult.Success();
        }
    }
}
