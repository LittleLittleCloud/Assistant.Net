// Copyright (ic) LittleLittleCloud. Some rights reserved.
// DotnetCoderAgentTest.cs

using Assistant.Core.Agent;
using AutoGen;
using FluentAssertions;

namespace Assistant.Core.Test;

public partial class DotnetCoderAgentTest
{
    [Function]
    public async Task<string> ReviewDotnetCode(bool hasExactlyOneDotnetCodeBlock)
    {
        if (!hasExactlyOneDotnetCodeBlock)
        {
            return "[REVIEW] The code should have exactly one dotnet code block";
        }
        else
        {
            return "[REVIEW] The code is correct";
        }
    }

    [ApiKeyFact("AZURE_OPENAI_API_KEY", "AZURE_OPENAI_ENDPOINT")]
    public async Task TestDotnetCoderAsync()
    {
        var llmFactory = Utils.CreateAzureOpenAIGPTFactory();
        var coder = new DotnetCoder("dotnet-coder", llmFactory);

        coder.Name.Should().Be("dotnet-coder");

        var quanDanZhang = llmFactory.Create("reviewer", "You review dotnet code");
        var code = await coder.SendAsync("What's the 100th prime number? Print the result to console");

        var review = await quanDanZhang.GenerateReplyAsync([code], new GenerateReplyOptions
        {
            Functions = [this.ReviewDotnetCodeFunction],
        });

        review.FunctionName.Should().Be(nameof(ReviewDotnetCode));
        var result = await this.ReviewDotnetCodeWrapper(review.FunctionArguments);
        result.Should().Be("[REVIEW] The code is correct");
    }
}
