// Copyright (ic) LittleLittleCloud. Some rights reserved.
// Configuration.cs

using System.Text.Json.Serialization;

namespace Assistant.CLI;

public class OpenAIConfiguration
{
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; } = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    [JsonPropertyName("modelName")]
    public string? ModelName { get; set; }
}

public class AzureOpenAIConfiguration
{
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("deployName")]
    public string? DeployName { get; set; }
}

public class AssistantAgentConfiguration
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Assistant";

    [JsonPropertyName("model_type")]
    public LLMModelType ModelType { get; set; } = LLMModelType.GPT3_5;
}

public class GroupChatConfiguration
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "GroupAdmin";

    [JsonPropertyName("model_type")]
    public LLMModelType ModelType { get; set; } = LLMModelType.GPT3_5;

    [JsonPropertyName("max_turn")]
    public int MaxTurn { get; set; } = 40;
}

public class CodeInterpreterAgentConfiguration
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "CodeInterpreter";

    [JsonPropertyName("planner_model_type")]
    public LLMModelType PlannerModelType { get; set; } = LLMModelType.GPT4;

    [JsonPropertyName("coder_model_type")]
    public LLMModelType CoderModelType { get; set; } = LLMModelType.GPT3_5;

    [JsonPropertyName("max_turn")]
    public int MaxTurn { get; set; } = 10;

    [JsonPropertyName("working_directory")]
    public string WorkingDirectory { get; set; } = Path.Join(Path.GetTempPath(), "CodeInterpreter");
}

public enum LLMModelType
{
    GPT3_5 = 0,
    GPT4 = 1,
    AZURE_GPT3_5 = 2,
    AZURE_GPT4 = 3,
}

public class Configuration
{
    [JsonPropertyName("gpt_3_5")]
    public OpenAIConfiguration GPT3_5 { get; set; } = new OpenAIConfiguration()
    {
        ModelName = "gpt-3.5-turbo",
    };

    [JsonPropertyName("gpt_4")]
    public OpenAIConfiguration GPT4 { get; set; } = new OpenAIConfiguration()
    {
        ModelName = "gpt-4",
    };

    [JsonPropertyName("azure_gpt_3_5")]
    public AzureOpenAIConfiguration? AzureGPT3_5 { get; set; }

    [JsonPropertyName("azure_gpt_4")]
    public AzureOpenAIConfiguration? AzureGPT4 { get; set; }

    [JsonPropertyName("assistant")]
    public AssistantAgentConfiguration Assistant { get; set; } = new AssistantAgentConfiguration();

    [JsonPropertyName("code_interpreter")]
    public CodeInterpreterAgentConfiguration CodeInterpreter { get; set; } = new CodeInterpreterAgentConfiguration();

    [JsonPropertyName("group_chat")]
    public GroupChatConfiguration GroupChat { get; set; } = new GroupChatConfiguration();
}
