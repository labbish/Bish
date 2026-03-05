# Bish Example Plugin

This is a simple example for C# plugin development for Bish.

Your plugin should contain at least one class, which implements `IPlugin` in **BishSDK**, and can be constructed with 0 arguments.

Your plugin should not contain `BishSdk.dll` and `BishRuntime.dll`, and it should not reference other projects (especially **BishCompiler**, as it would cause cycle referencing).

The `IPlugin` interface requires you to implement `Initialize` method, whose signature is above:

```csharp
public void Initialize(PluginExports exports)
```

Use `exports.Exports.Add("name", value)` to add an exported symbol. After that, you can build the project into a single `.dll` library, and access with `import("plugin.dll").name`.

See [HZZcode/BishGL](https://github.com/HZZcode/BishGL) for a more complicated example.