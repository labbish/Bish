using BishLSP;
using OmniSharp.Extensions.LanguageServer.Server;

var server = await LanguageServer.From(options =>
        options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            .OnInitialize((server, request, token) => Task.CompletedTask)
            .WithHandler<TextDocumentHandler>()
);

await server.WaitForExit;