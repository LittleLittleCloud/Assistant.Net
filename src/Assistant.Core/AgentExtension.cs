// Copyright (ic) LittleLittleCloud. Some rights reserved.
// AgentExtension.cs

using System.Text.Json;
using AutoGen;
using Json.Schema;
using Json.Schema.Generation;

namespace Assistant.Core;

public static class AgentExtension
{
    public static IAgent FormatAsJsonAsync<T>(this IAgent agent, int maxRetry = 5, IAgent? formatAgent = null)
    {
        var jsonSchemaBuilder = new JsonSchemaBuilder().FromType<T>();
        var jsonSchema = jsonSchemaBuilder.Build();
        return agent.RegisterMiddleware(async (messages, option, innerAgent, ct) =>
        {
            var reply = await innerAgent.GenerateReplyAsync(messages, option, ct);
            while (maxRetry > 0)
            {
                if (reply.Content is string content)
                {
                    try
                    {
                        var _ = JsonSerializer.Deserialize<T>(content);
                        return reply;
                    }
                    catch (JsonException ex)
                    {
                        var prompt = $"""
                        Fix the error and convert the text into Json object according to the schema below.
                        
                        ```plaintext
                        {content}
                        ```

                        ```schema
                        {JsonSerializer.Serialize(jsonSchema)}
                        ```

                        ```error
                        {ex.Message}
                        ```

                        return json directly, don't wrap it inside any block or include any other text.
                        """;
                        formatAgent ??= innerAgent;
                        reply = await formatAgent.SendAsync(prompt);
                        maxRetry--;
                        continue;
                    }
                }

                throw new JsonException("Failed to format the message as JSON");
            }

            throw new JsonException("Failed to format the message as JSON");
        });
    }
}
