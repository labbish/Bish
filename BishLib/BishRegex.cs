using System.Text.RegularExpressions;
using BishRuntime;

namespace BishLib;

public static class BishRegexModule
{
    public static void Initialize() => BishLib.InitializeModule("regex",
        ("Regex", BishRegex.StaticType),
        ("Match", BishRegexMatch.StaticType),
        ("Matches", BishRegexMatches.StaticType),
        ("RegexError", Error)
    );

    public static readonly BishType Error = new("RegexError", [BishError.StaticType]);
}

public class BishRegex(string pattern, string? flags, Regex regex) : BishObject
{
    public readonly string Pattern = pattern;
    public readonly string? Flags = TrimFlags(flags);
    public readonly Regex Regex = regex;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Regex");

    private static RegexOptions ParseFlags(string? flags) =>
        flags?.Aggregate(RegexOptions.None, (current, flag) => current | flag switch
        {
            'i' => RegexOptions.IgnoreCase,
            'm' => RegexOptions.Multiline,
            's' => RegexOptions.Singleline,
            'c' => RegexOptions.Compiled,
            _ => 0
        }) ?? RegexOptions.None;

    private static string? TrimFlags(string? flags) =>
        flags is null ? null : new string(flags.Where(c => !char.IsWhiteSpace(c)).ToArray());

    [Builtin("hook")]
    public static BishRegex New(BishString pattern, [DefaultNull] BishString? flags) => new(pattern.Value, flags?.Value,
        BishException.Wrapped(BishRegexModule.Error, () => new Regex(pattern.Value, ParseFlags(flags?.Value))));

    [Builtin]
    public static BishRegexMatch? Match(BishRegex self, BishString str) =>
        BishRegexMatch.Of(self.Regex.Match(str.Value));

    [Builtin]
    public static BishRegexMatches MatchAll(BishRegex self, BishString str) => new(self.Regex.Matches(str.Value));

    [Builtin]
    public static BishString Replace(BishRegex self, BishString str, BishObject replacer) => new(
        replacer is BishString value
            ? self.Regex.Replace(str.Value, value.Value)
            : self.Regex.Replace(str.Value,
                match => replacer.Call(new BishArgs([BishRegexMatch.Of(match) ?? BishNull.Instance as BishObject]))
                    .As<BishString>("replaced").Value));

    [Builtin]
    public static BishList Split(BishRegex self, BishString str) =>
        new(self.Regex.Split(str.Value).Select(part => new BishString(part)).ToList<BishObject>());

    [Builtin("hook")]
    public static BishString Get_pattern(BishRegex self) => new(self.Pattern);

    [Builtin("hook")]
    public static BishString? Get_flags(BishRegex self) => self.Flags is { } flags ? new BishString(flags) : null;

    [Builtin]
    public static BishString Repr(BishRegex self, BishReprContext _) => new('/' + self.Pattern + '/' + self.Flags);
}

public class BishRegexMatch : BishObject
{
    public readonly Capture Capture;

    private BishRegexMatch(Capture capture) => Capture = capture;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Match");

    public static BishRegexMatch? Of(Capture capture) =>
        (capture as Group)?.Success != false ? new BishRegexMatch(capture) : null;

    [Builtin("hook")]
    public static BishString Get_value(BishRegexMatch self) => new(self.Capture.Value);

    [Builtin("hook")]
    public static BishInt Get_start(BishRegexMatch self) => BishInt.Of(self.Capture.Index);

    [Builtin("hook")]
    public static BishInt Get_end(BishRegexMatch self) => BishInt.Of(self.Capture.Index + self.Capture.Length);

    [Builtin("hook")]
    public static BishInt Get_length(BishRegexMatch self) => BishInt.Of(self.Capture.Length);

    [Builtin("hook")]
    public static BishString? Get_name(BishRegexMatch self) =>
        self.Capture is Group group ? new BishString(group.Name) : null;

    [Builtin("hook")]
    public static BishRegexMatches? Get_groups(BishRegexMatch self) =>
        self.Capture is Match match ? new BishRegexMatches(match.Groups) : null;

    [Builtin("hook")]
    public static BishRegexMatches? Get_captures(BishRegexMatch self) =>
        self.Capture is Group group ? new BishRegexMatches(group.Captures) : null;

    [Builtin]
    public static BishString Repr(BishRegexMatch self, BishReprContext ctx) =>
        new($"Match({BishString.CallRepr(self.Capture.Value, ctx)})");
}

public class BishRegexMatches(IReadOnlyList<Capture> collection) : BishObject
{
    // This will always be (Capture|Group|Match)Collection
    public readonly IReadOnlyList<Capture> Collection = collection;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Matches");

    [Builtin("op")]
    public static BishRegexMatch? GetIndex(BishRegexMatches self, BishObject x) => BishRegexMatch.Of(x switch
    {
        BishInt index => self.Collection[index.Value],
        BishString name => self.Collection is GroupCollection groups
            ? groups[name.Value]
            : throw BishException.OfType_Expect("index", name, "int"),
        _ => throw BishException.OfType_Expect("index", x, "int or string")
    });

    [Builtin]
    public static BishRegexGroupsIterator Iter(BishRegexMatches self) => new(self);
}

public class BishRegexGroupsIterator(BishRegexMatches matches) : BishObject
{
    public readonly BishRegexMatches Matches = matches;
    public int Index;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Groups.iter");

    [Iter]
    public BishRegexMatch? Next() =>
        Index >= Matches.Collection.Count ? null : BishRegexMatch.Of(Matches.Collection[Index++]);
}