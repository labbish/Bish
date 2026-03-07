using OmniSharp.Extensions.LanguageServer.Server;

namespace BishLSP;

public static class Server
{
    public static async Task RunAsync()
    {
        var server = await LanguageServer.From(options =>
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .OnInitialize((server, request, token) => Task.CompletedTask)
                    .WithHandler<TextDocumentHandler>()
        );
        
        await server.WaitForExit;
    }
}