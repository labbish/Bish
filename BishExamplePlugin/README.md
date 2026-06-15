# Bish Example Plugin

This is a simple example for C# plugin development for Bish.

Your plugin should contain exactly one struct (or class), which implements `BishRuntime.IModule`. It requires you to implement a static getter-only property `Exports`, whose signature is below:

```csharp
static abstract BishObject Exports { get; }
```

This will be the value returned by `import('plugin.dll')`.

You can also use `IModule.ExportsFrom` to simplify your code. See `Example.cs` for its usage.

After that, you can build the project into a single `.dll` library.  Your plugin should not contain `BishRuntime.dll`, and it should not reference other projects in `Bish`, as it would cause cycle referencing.

See [HZZcode/BishGL](https://github.com/HZZcode/BishGL) for a more complicated example. (Note: this might not be up-to-date.)