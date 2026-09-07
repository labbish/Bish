# Bish
This is a set of projects related to labbish languages.

## Parts & Tech Stack

- Common Runtime
  * **Bish** - The entry point, containing an REPL and the Language Server.
    - [Omnisharp LSP](https://github.com/OmniSharp/csharp-language-server-protocol) - A C# Language Server Protocal Implementation.
  * **BishExamplePlugin** - A plugin example. See its [README](BishExamplePlugin/README.md) for more information.
  * **BishLib** - Builtin Libraries.
  * **BishRuntime** - The VM and object model.
  * **BishRuntimeGenerators** - Code generators for **BishRuntime**.
- Bish Language
  * **BishCompiler** - The compiler which transforms Bish code into bytecode.
    - [ANTLR4](https://www.antlr.org/) - A powerful parser generator.
- Utils
  * **BishUtils** - Utilities for concurrent containers.
- Tests
  * **BishTest** - Unit tests.
    - [xUnit](https://xunit.net) - A free, open source, community-focused unit testing tool for .NET.

## Install

For each architect, there are 2 compressed files in actions output:
- `Bish-[arch].zip`: The common runtime, allowing you to run `.bishc` files and import `.dll` plugins.
- `BishLanguage-[arch].zip`: The Bish language (as a dll plugin), allowing you to run `.bish` files.

You'll need to decompress all parts you need and put them into one directory (that is, the main `.exe` and plugin `.dll` should be in a same directory).