// Copyright (ic) LittleLittleCloud. Some rights reserved.
// Utils.cs

namespace Assistant.Core.Test;

internal static class Utils
{
    public static AzureOpenAIGPTFactory CreateAzureOpenAIGPT35Factory()
    {
        var endPoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new ArgumentNullException("AZURE_OPENAI_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? throw new ArgumentNullException("AZURE_OPENAI_API_KEY");
        var deployName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOY_NAME") ?? "gpt-35-turbo-16k";
        return new AzureOpenAIGPTFactory(endPoint, apiKey, deployName);
    }

    public static AzureOpenAIGPTFactory CreateAzureOpenAIGPT4Factory()
    {
        var endPoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new ArgumentNullException("AZURE_OPENAI_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? throw new ArgumentNullException("AZURE_OPENAI_API_KEY");
        var deployName = "gpt-4";
        return new AzureOpenAIGPTFactory(endPoint, apiKey, deployName);
    }
}
