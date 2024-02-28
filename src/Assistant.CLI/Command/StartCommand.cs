// Copyright (ic) LittleLittleCloud. Some rights reserved.
// StartCommand.cs

using System.ComponentModel;
using Assistant.CLI.Component;
using Assistant.Core;
using Assistant.Core.Agent;
using AutoGen;
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
        ILLMFactory llmFactoryForAssistant = configuration.Assistant.ModelType switch
        {
            LLMModelType.GPT3_5 => new OpenAIGPTFactory(configuration.GPT3_5.ApiKey ?? throw new ArgumentNullException(), configuration.GPT3_5.ModelName!),
            LLMModelType.GPT4 => new OpenAIGPTFactory(configuration.GPT4.ApiKey ?? throw new ArgumentNullException(), configuration.GPT4.ModelName!),
            LLMModelType.AZURE_GPT3_5 => new AzureOpenAIGPTFactory(configuration.AzureGPT3_5?.Endpoint ?? throw new ArgumentNullException(), configuration.AzureGPT3_5.ApiKey ?? throw new ArgumentNullException(), configuration.AzureGPT3_5.DeployName ?? throw new ArgumentNullException()),
            LLMModelType.AZURE_GPT4 => new AzureOpenAIGPTFactory(configuration.AzureGPT4?.Endpoint ?? throw new ArgumentNullException(), configuration.AzureGPT4.ApiKey ?? throw new ArgumentNullException(), configuration.AzureGPT4.DeployName ?? throw new ArgumentNullException()),
            _ => throw new ArgumentOutOfRangeException(),
        };
        var userAgent = new UserProxyAgent("user", humanInputMode: ConversableAgent.HumanInputMode.ALWAYS);
        var assistant = new DotnetCoder(configuration.Assistant.Name, llmFactoryForAssistant)
            .RegisterPrintFormatMessageHook();

        await assistant.SendAsync(userAgent, "Hello, I'm Assistant. How can I help you today?");
        return 0;
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
