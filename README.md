# Bish
The Bish Language.

## Parts & Tech Stack

This repository consists of 6 parts:

* **Bish** - The entry point, containing an REPL.
  - [CommandLineParser](https://github.com/commandlineparser/commandline) - A clean and concise command line parser.
* **BishLSP** - The Language Server of Bish, and a VSCode extension based on it.
  - [Omnisharp LSP](https://github.com/OmniSharp/csharp-language-server-protocol) - A C# Language Server Protocal Implementation.
* **BishCompiler** - The compiler which transforms Bish code into bytecode.
  - [ANTLR4](https://www.antlr.org/) - A powerful parser generator.
* **BishBytecode** - To run bytecode and interact with the VM.
* **BishRuntime** - The VM and object model.
* **BishTest** - Unit tests.
  - [xUnit](https://xunit.net) - A free, open source, community-focused unit testing tool for .NET.
  - [Fluent Assertions](https://fluentassertions.com) - A natural way to specify the expected outcome of unit tests.