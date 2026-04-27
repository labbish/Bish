# Bish
The Bish Language version 3.1.

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

```text
func static(cls) {
    cls.hook_init = (self, ..args)
        throw ArgumentError('static class {} is not constructible'.format(cls.name));
    cls
};

@static
class SimpleRunner {
    queue := [];
    func blocked(task) {
        queue.add(task);
        while (!task.completed) {
            if (queue.length == 0) continue;
            t := del queue[0];
            process(t);
        };
        return task.result;
    };
    func process(task) {
        if (task.cancelled) return task.completed = true;
        if (task.completed) return;
        task.poll(ctx(task));
    };
    func ctx(task) { .waker: Waker(task) };
    class Waker {
        init(self, task) self.task := task;
        func awake(self) queue.add(self.task);
    };
};

func f() async {
    await Task.sleep(1500);
    print('f is done!\n');
    return 'f';
};

func g() async {
    await Task.sleep(1000);
    print('g is done!\n');
    return 'g';
};

x := SimpleRunner.blocked(Task.all(f(), g()));
print('result is {}\n'.format(x));
if (!Runner.started) print('Independent!');
```

Output:
```text
g is done!
f is done!
result is [f, g]
Independent!
```

## Parts & Tech Stack

This repository consists of 8 parts:

* **Bish** - The entry point, containing an REPL and the Language Server.
  - [CommandLineParser](https://github.com/commandlineparser/commandline) - A clean and concise command line parser.
  - [Omnisharp LSP](https://github.com/OmniSharp/csharp-language-server-protocol) - A C# Language Server Protocal Implementation.
* **BishExamplePlugin** - A plugin example. See its [README](BishExamplePlugin/README.md) for more information.
* **BishCompiler** - The compiler which transforms Bish code into bytecode.
  - [ANTLR4](https://www.antlr.org/) - A powerful parser generator.
* **BishLib** - Builtin Libraries.
* **BishRuntime** - The VM and object model.
* **BishRuntimeGenerators** - Code generators for **BishRuntime**.
* **BishUtils** - Utilities for concurrent containers.
* **BishTest** - Unit tests.
  - [xUnit](https://xunit.net) - A free, open source, community-focused unit testing tool for .NET.