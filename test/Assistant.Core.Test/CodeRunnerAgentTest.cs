// Copyright (ic) LittleLittleCloud. Some rights reserved.
// CodeRunnerAgentTest.cs

using System.Text.Json;
using System.Text.Json.Serialization;
using Assistant.Core.Agent;
using AutoGen;
using AutoGen.DotnetInteractive;
using FluentAssertions;
using Json.Schema;
using Json.Schema.Generation;

namespace Assistant.Core.Test;

public class CodeRunnerAgentTest
{
    public struct Output
    {
        [Description("The code output")]
        [JsonPropertyName("codeOutput")]
        public string CodeOutput { get; set; }

        [Description("Installed nuget packages if there's any")]
        [JsonPropertyName("installedNugetPackages")]
        public string[] InstalledNugetPackages { get; set; }
    }

    [ApiKeyFact("AZURE_OPENAI_API_KEY", "AZURE_OPENAI_ENDPOINT")]
    public async Task TestDotnetCodeRunnerAsync()
    {
        var llmFactory = Utils.CreateAzureOpenAIGPTFactory();
        var workingDirectory = Path.Join(Path.GetTempPath(), nameof(CodeRunnerAgentTest));
        if (!Directory.Exists(workingDirectory))
        {
            Directory.CreateDirectory(workingDirectory);
        }

        using var service = new InteractiveService(workingDirectory);
        await service.StartAsync(workingDirectory);

        var dotnetRunner = new DotnetCodeRunner("dotnet-runner", service, llmFactory);
        var quanDanZhang = llmFactory.Create("reviewer", "You are a helpful AI assistant");

        var codeSnippet1 = """
            ```csharp
            Console.WriteLine("Hello, World!");
            ```
            """;

        var reply = await dotnetRunner.SendAsync(codeSnippet1);
        var output = await this.FormatDotnetRunnerResponseAsync(quanDanZhang, reply);
        output.CodeOutput.Should().Be("Hello, World!");
        output.InstalledNugetPackages.Should().BeNullOrEmpty();

        var codeSnippet2 = """
            ```nuget
            Newtonsoft.Json
            ```

            ```csharp
            using Newtonsoft.Json;
            Console.WriteLine(JsonConvert.SerializeObject(new { Name = "John", Age = 30 }));
            ```
            """;

        reply = await dotnetRunner.SendAsync(codeSnippet2);
        output = await this.FormatDotnetRunnerResponseAsync(quanDanZhang, reply);
        output.CodeOutput.Should().Be("{\"Name\":\"John\",\"Age\":30}");
        output.InstalledNugetPackages.Should().BeEquivalentTo(["Newtonsoft.Json"]);
    }

    private async Task<Output> FormatDotnetRunnerResponseAsync(IAgent agent, Message response)
    {
        var prompt = $"""
            Format the message below to Json according to the following schema:

            ```schema
            {JsonSerializer.Serialize(new JsonSchemaBuilder().FromType<Output>().Build())}
            ```

            ```message
            {response.Content}
            ```

            Your reply should be a json object. Don't wrap it inside any block or include any other text.
            """;

        var reply = await agent.FormatAsJsonAsync<Output>().SendAsync(prompt);
        return JsonSerializer.Deserialize<Output>(reply.Content!);
    }
}
