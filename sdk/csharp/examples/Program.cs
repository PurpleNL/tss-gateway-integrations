using TSS.Gateway.Sdk;
using TSS.Play.Shared.Events;
using TSS.Play.Shared.Requests;

// Example integration: connects to a TSS gateway, lists the player's stories, starts one,
// queries the story status while it plays, and stops it again. Each step prints what happened,
// so running this against a live player shows the full request > player > event round trip.

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- \\");
    Console.WriteLine("  --host <gateway host> --user <username> --password <password> \\");
    Console.WriteLine("  --location <location uuid> --setup <setup uuid> \\");
    Console.WriteLine("  [--port 5672] [--screen 0] [--story <story uuid, defaults to the first available>]");

    return 1;
}

GatewayClientOptions options = new GatewayClientOptions
{
    Host = Arg("host"),
    Port = int.Parse(Arg("port", "5672")),
    UserName = Arg("user"),
    Password = Arg("password"),
    ClientId = "sdk-example",
    LocationUuid = Arg("location"),
    SetupUuid = Arg("setup"),
    ScreenId = int.Parse(Arg("screen", "0")),
    OnDiagnosticMessage = message => Console.WriteLine($"[diagnostic] {message}")
};

await using GatewayClient client = await GatewayClient.ConnectAsync(options);

Console.WriteLine($"Connected to the gateway at {options.Host}.");

AvailableStoriesResponse stories = await client.Queries.GetAvailableStoriesAsync(new AvailableStoriesRequest {Limit = 50});

Console.WriteLine($"The player has {stories.Count} stories:");

foreach (AvailableStory availableStory in stories.Stories)
{
    Console.WriteLine($"  {availableStory.Name} ({availableStory.Status})");
}

if (stories.Count == 0)
{
    Console.WriteLine("Nothing to start, exiting.");

    return 1;
}

// A player that is already running a story ignores a start request, so stop it first.
StoryStatusResponse initialStatus = await client.Queries.GetStoryStatusAsync();

if (initialStatus.Status != State.None)
{
    Task alreadyRunningStoppedTask = client.Events.WaitForStoryStoppedAsync(30000);

    await client.Requests.StopStoryAsync();

    Console.WriteLine($"The player was already running \"{initialStatus.StoryTitle}\", requested it to stop first, waiting...");

    await alreadyRunningStoppedTask;

    // The stopped event precedes the unload, and a start request during the unload is
    // ignored, so give the player a moment.
    await Task.Delay(TimeSpan.FromSeconds(3));
}

string storyUuid = Arg("story", stories.Stories[0].Uuid);

// The wait is registered before the request is published. The player answers with a
// story.started event, and registering first means it cannot slip by between the two calls.
Task<StoryEvent> startedTask = client.Events.WaitForStoryStartedAsync(storyUuid, 30000);

await client.Requests.StartStoryAsync(storyUuid);

Console.WriteLine($"Requested the player to start story {storyUuid}, waiting for it to report back...");

await startedTask;

Console.WriteLine("The player started the story.");

StoryStatusResponse status = await client.Queries.GetStoryStatusAsync();

Console.WriteLine($"Story status: \"{status.StoryTitle}\" version {status.StoryVersion}, state {status.Status}.");
Console.WriteLine("Letting it play for five seconds...");

await Task.Delay(TimeSpan.FromSeconds(5));

Task stoppedTask = client.Events.WaitForStoryStoppedAsync(30000);

await client.Requests.StopStoryAsync();

Console.WriteLine("Requested the player to stop the story, waiting for it to report back...");

await stoppedTask;

Console.WriteLine("The player stopped the story. Done.");

return 0;

string Arg(string name, string? fallback = null)
{
    int index = Array.IndexOf(args, $"--{name}");

    if (index >= 0 && index + 1 < args.Length)
    {
        return args[index + 1];
    }

    if (fallback != null)
    {
        return fallback;
    }

    Console.Error.WriteLine($"Missing required argument --{name}.");
    Environment.Exit(1);

    return string.Empty;
}