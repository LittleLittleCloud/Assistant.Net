// Copyright (ic) LittleLittleCloud. Some rights reserved.
// LLMFactory.cs

using AutoGen;
using AutoGen.OpenAI;

namespace Assistant.Core;

public interface ILLMFactory
{
    Task<IAgent> CreateAsync(string name, string systemMessage);

    IAgent Create(string name, string systemMessage);
}

public class OpenAIGPTFactory(string apiKey, string modelName) : ILLMFactory
{
    public IAgent Create(string name, string systemMessage)
    {
        var openAIConfig = new OpenAIConfig(apiKey, modelName);

        return new GPTAgent(name, systemMessage, openAIConfig);
    }

    public Task<IAgent> CreateAsync(string name, string systemMessage)
    {
        return Task.FromResult(Create(name, systemMessage));
    }
}

public class AzureOpenAIGPTFactory(string endpoint, string apiKey, string deployName) : ILLMFactory
{
    public IAgent Create(string name, string systemMessage)
    {
        var openAIConfig = new AzureOpenAIConfig(endpoint, deployName, apiKey);

        return new GPTAgent(name, systemMessage, openAIConfig);
    }

    public Task<IAgent> CreateAsync(string name, string systemMessage)
    {
        return Task.FromResult(Create(name, systemMessage));
    }
}
