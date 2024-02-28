// Copyright (ic) LittleLittleCloud. Some rights reserved.
// AgentExtension.cs

using AutoGen;
using Xunit.Abstractions;

namespace Assistant.Core.Test;

internal static class AgentExtension
{
    public static IAgent WriteToTestOutput(this IAgent agent, ITestOutputHelper output)
    {
        return agent.RegisterMiddleware(async (msgs, option, next, ct) =>
        {
            var reply = await next.GenerateReplyAsync(msgs, option, ct);
            var formatString = reply.FormatMessage();
            output.WriteLine(formatString);

            return reply;
        });
    }
}
