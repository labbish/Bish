# Bish Example Plugin

This is a simple example for C# plugin development for Bish.

Your plugin should contain at least one class, which implements `IPlugin` in **BishSDK**, and can be constructed with 0 arguments.

Your `.csproj` file will need to contain the following while referencing **BishSdk** and **BishRuntime**:

```xml
<Private>false</Private>
<PrivateAssets>all</PrivateAssets>
<ExcludeAssets>runtime</ExcludeAssets>
```

And it should not reference other projects (especially **BishCompiler**, as it would cause cycle referencing).

The `IPlugin` interface requires you to implement `Initialize` method, whose signature is above:

```csharp
public void Initialize(PluginExports exports)
```

Use `exports.Exports.Add("name", value)` to add an exported symbol. After that, you can build the project into a single `.dll` library, and access with `import("plugin.dll").name`.