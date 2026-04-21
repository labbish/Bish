using JetBrains.Annotations;

namespace BishRuntime;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
#pragma warning disable CS9113
public class BuiltinAttribute(string? prefix = null, string? tag = null) : Attribute;

[AttributeUsage(AttributeTargets.Parameter)]
public class DefaultNullAttribute : Attribute;

[AttributeUsage(AttributeTargets.Parameter)]
public class RestAttribute : Attribute;

public class DefaultNull : BishObject;