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
        ModelName = "gpt-4-turbo",
    };

    [JsonPropertyName("azure_gpt_3_5")]
    public AzureOpenAIConfiguration? AzureGPT3_5 { get; set; }

    [JsonPropertyName("azure_gpt_4")]
    public AzureOpenAIConfiguration? AzureGPT4 { get; set; }

    [JsonPropertyName("assistant")]
    public AssistantAgentConfiguration Assistant { get; set; } = new AssistantAgentConfiguration();

}
