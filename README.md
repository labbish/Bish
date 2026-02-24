# Bish
The Bish Language Compiler & Runtime.

## Parts

This repository consists of 5 parts:

* **Bish** - The entry point, containing an REPL.
* **BishCompiler** - The compiler which transforms Bish code into bytecode.
* **BishBytecode** - To run bytecode and interact with the VM.
* **BishRuntime** - The VM and object model.
* **BishTest** - Unit tests.

## Tech Stack
* [ANTLR4](https://github.com/antlr/antlr4) - A powerful parser generator.
* [xUnit](https://xunit.net) - A free, open source, community-focused unit testing tool for .NET.
* [Fluent Assertions](https://fluentassertions.com) - A natural way to specify the expected outcome of unit tests.