# Bish
The Bish Language version 3.0.

## Parts & Tech Stack

This repository consists of 9 parts:

* **Bish** - The entry point, containing an REPL and the Language Server.
  - [CommandLineParser](https://github.com/commandlineparser/commandline) - A clean and concise command line parser.
  - [Omnisharp LSP](https://github.com/OmniSharp/csharp-language-server-protocol) - A C# Language Server Protocal Implementation.
* **BishSdk** - The SDK for C# plugins.
* **BishExamplePlugin** - A plugin example. See its [README](BishExamplePlugin/README.md) for more information.
* **BishCompiler** - The compiler which transforms Bish code into bytecode.
  - [ANTLR4](https://www.antlr.org/) - A powerful parser generator.
* **BishLib** - Builtin Libraries.
* **BishRuntime** - The VM and object model.
* **BishRuntimeGenerators** - Code generators for **BishRuntime**.
* **BishUtils** - Utilities for concurrent containers.
* **BishTest** - Unit tests.
  - [xUnit](https://xunit.net) - A free, open source, community-focused unit testing tool for .NET.