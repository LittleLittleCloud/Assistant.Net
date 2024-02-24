// Copyright (ic) LittleLittleCloud. Some rights reserved.
// AgentExtension.cs

using System.Text.Json;
using AutoGen;
using Json.Schema;
using Json.Schema.Generation;

namespace Assistant.Core;

public static class AgentExtension
{
    public static IAgent FormatAsJsonAsync<T>(this IAgent agent, int maxRetry = 5)
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
                        Fix the error and format the plain text into Json object. The object must conform to the following schema:

                        ```schema
                        {JsonSerializer.Serialize(jsonSchema)}
                        ```

                        ```plaintext
                        {content}
                        ```

                        ```error
                        {ex.Message}
                        ```

                        return json directly, don't wrap it inside any block or include any other text.
                        """;
                        reply = await innerAgent.SendAsync(prompt);
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
