// Copyright (ic) LittleLittleCloud. Some rights reserved.
// Program.cs
using Assistant.CLI;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<StartCommand>("start")
        .WithDescription("Start the group chat with a list of assistants");
});

return app.Run(args);
