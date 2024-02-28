// Copyright (ic) LittleLittleCloud. Some rights reserved.
// Logger.cs

namespace Assistant.CLI.Component;

public interface ILogger
{
    void Info(string message);

    void Debug(string message);

    void Error(string message, Exception? exception = null);
}
