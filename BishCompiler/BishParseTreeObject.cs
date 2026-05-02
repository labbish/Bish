namespace BishCompiler;

public class BishParseTreeObject : BishObject
{
    public readonly IParseTree Tree;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("ParseTree");

    public override string ToString() => ToString(Tree);

    private static string ToString(IParseTree tree)
    {
        if (tree is TerminalNodeImpl terminal) return terminal.ToString();
        return $"([{TypeName(tree)}] {string.Join(" ", Children(tree).Select(ToString))})";
    }

    private BishParseTreeObject(IParseTree tree)
    {
        Tree = tree;
        DefMember("type", new BishString(TypeName(tree)));
        DefMember("text", tree is TerminalNodeImpl node ? new BishString(node.Symbol.Text) : BishNull.Instance);
    }

    public static string TypeName(IParseTree tree) => tree.GetType().Name.TrimEnd("Context").ToString();

    public static BishParseTreeObject From(IParseTree root)
    {
        var chained = ChainedTrees(root);
        var objects = chained.Select(tree => new BishParseTreeObject(tree)).ToList();
        foreach (var (tree, obj) in chained.Zip(objects))
        {
            obj.DefMember("parent", tree.Parent is { } parent ? ObjectFrom(parent) : BishNull.Instance);
            obj.DefMember("children", new BishList(Children(tree).Select(ObjectFrom).ToList<BishObject>()));
        }

        return objects.First();
        BishParseTreeObject ObjectFrom(IParseTree tree) => objects[chained.IndexOf(tree)];
    }

    public static List<IParseTree> Children(IParseTree tree) =>
        Enumerable.Range(0, tree.ChildCount).Select(tree.GetChild).ToList();

    public static List<IParseTree> ChainedTrees(IParseTree root)
    {
        List<IParseTree> excludes = [];
        return Inner(root);

        List<IParseTree> Inner(IParseTree? tree)
        {
            if (tree is null || excludes.Contains(tree)) return [];
            excludes.Add(tree);
            return [tree, ..Inner(tree.Parent), ..Children(tree).SelectMany(Inner)];
        }
    }
}