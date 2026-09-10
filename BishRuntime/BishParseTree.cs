using BishUtils;

namespace BishRuntime;

public record Node(string Type, string? Text);

public class BishParseTree(Node node, IList<BishParseTree> children, SourcePosition? source = null) : BishObject
{
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("ParseTree");

    public readonly Node Node = node;
    public readonly IList<BishParseTree> Children = children.ToConcurrentList();
    public readonly SourcePosition? Source = source;
    public string? File = null;

    public string? Text => Node.Text;

    public void Deconstruct(out string type, out List<BishParseTree> children)
    {
        type = Node.Type;
        children = Children.ToList();
    }

    public BishParseTree WithFile(string file)
    {
        File = file;
        return this;
    }

    [Builtin("hook")]
    public static BishParseTree New(BishString type, [DefaultNull] BishList? children,
        [DefaultNull] BishList? source) =>
        new(children is null ? new Node("Terminal", type.Value) : new Node(type.Value, null),
            children?.List.Select(item => item.As<BishParseTree>("child")).ToList() ?? [],
            source is null ? null : SourcePosition.FromObject(source));

    [Builtin("hook")]
    public static BishString Get_type(BishParseTree self) => new(self.Node.Type);

    [Builtin("hook")]
    public static BishString? Get_text(BishParseTree self) => self.Text is { } text ? new BishString(text) : null;

    [Builtin("hook")]
    public static BishList Get_children(BishParseTree self) => new(self.Children.ToList<BishObject>());

    [Builtin("hook")]
    public static BishList? Get_source(BishParseTree self) => self.Source?.ToObject();

    [Builtin]
    public static BishString Repr(BishParseTree self, BishReprContext _) => new(self.Repr());

    public string Repr() => Node.Text is { } text
        ? $"[{text}]"
        : $"({string.Join(' ', [Node.Type, ..Children.Select(tree => tree.Repr())])})";
}