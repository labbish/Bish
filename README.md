# Bish
The Bish Language version 3.0.

## Code Example

```text
Field := Func.Arg;
func dataclass(..fields) {
    for ([i, field] : fields.iter().entries)
        if (field is of string) fields[i] = Field(field);
    name := 'dataclass({})'.format(fields.iter().map((field) field.name).join(', '));
    t := type(name, []);
    t.hook_init := Func('hook_init', [Field('self'), ..fields], (args) {
        [self, ..args] := args;
        for ([i, {.name}] : fields.iter().entries)
            self.hook_def(name, args[i]);
    });
    t.toString := (self) '{}({})'.format(self.type, fields.iter()
        .map(({.name}) '{}={}'.format(name, self.hook_get(name))).join(', '));
    // You can also define `op_eq` here to overload operator ==
    t
};

class C: dataclass('a', 'b', Field('c').default(0));
c := C(1, 2);
print('c={}\n'.format(c));
```

Output: `c=C(a=1, b=2, c=0)`

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