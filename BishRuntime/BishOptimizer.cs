using System.Runtime.CompilerServices;
using BishUtils;
using Codes = System.Collections.Generic.IList<BishRuntime.BishBytecode>;

namespace BishRuntime;

public static class BishOptimizer
{
    public static readonly IList<Optimizer> Optimizers = new ConcurrentList<Optimizer>();

    public static Codes Optimize(Codes codes) =>
        Optimizers.Aggregate(codes, (current, optimizer) => optimizer.Optimize(current));

    extension(Codes codes)
    {
        public void RenameTag(Tag from, Tag to)
        {
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.Tag == from) code.Tag = to;
                if (code is Jumper jumper && jumper.GoalTag == from)
                    codes[i] = jumper with { GoalTag = to };
            }
        }

        public void Replace(int i, BishBytecode code) => codes[i] = code.Tagged(codes[i].Tag);

        public Codes RemoveUnusedTag()
        {
            foreach (var code in codes)
                if (code.Tag is { } tag && codes.All(other => other is not Jumper jumper || jumper.GoalTag != tag))
                    code.Tag = null;
            return codes;
        }

        public Codes RemoveInnerOuter()
        {
            for (var i = 0; i < codes.Count - 1; i++)
                if (codes[i] is Inner && codes[i + 1] is Outer)
                {
                    codes.Replace(i, new Nop());
                    codes.Replace(i + 1, new Nop());
                }

            return codes;
        }

        public Codes RemoveMoveDel()
        {
            for (var i = 0; i < codes.Count - 1; i++)
                if (codes[i] is Move move && codes[i + 1] is Del del && move.Name == del.Name)
                {
                    codes.Replace(i, new Nop());
                    codes.Replace(i + 1, new Nop());
                }

            return codes;
        }

        public Codes RemoveDiscarded()
        {
            bool Discarded(string name) => name.All(c => c == '_');
            for (var i = 0; i < codes.Count; i++)
            {
                codes[i] = codes[i] switch
                {
                    Set code when Discarded(code.Name) => new Nop(),
                    Def code when Discarded(code.Name) => new Nop(),
                    Move code when Discarded(code.Name) => new Pop(),
                    { } code => code
                };
            }

            return codes;
        }

        public Codes RemoveValuePop()
        {
            for (var i = 0; i < codes.Count - 1; i++)
                if (codes[i] is Value && codes[i + 1] is Pop)
                {
                    codes.Replace(i, new Nop());
                    codes.Replace(i + 1, new Nop());
                }

            return codes;
        }

        public Codes CombineDefPop()
        {
            for (var i = 0; i < codes.Count - 1; i++)
                if (codes[i] is Def def && codes[i + 1] is Pop { Tag: null })
                {
                    codes.Replace(i, new Nop());
                    codes.Replace(i + 1, new Move(def.Name));
                }

            return codes;
        }

        public Codes CombineDefMemberPop()
        {
            for (var i = 0; i < codes.Count - 1; i++)
                if (codes[i] is DefMember def && codes[i + 1] is Pop { Tag: null })
                {
                    codes.Replace(i, new Nop());
                    codes.Replace(i + 1, new MoveMember(def.Name));
                }

            return codes;
        }

        public Codes MoveNopTag()
        {
            for (var i = 0; i < codes.Count - 1; i++)
                if (codes[i] is Nop { Tag: not null })
                {
                    switch (codes[i].Tag, codes[i + 1].Tag)
                    {
                        case ({ } tag, { } next):
                            codes.RenameTag(next, tag);
                            break;
                        case ({ } tag, null):
                            codes[i + 1].Tag = tag;
                            break;
                        case (_, null): break;
                    }

                    codes[i].Tag = null;
                }

            return codes;
        }

        public Codes RemoveUntaggedNop() =>
            codes.Where(code => !(code is Nop && code.Tag is null)).ToConcurrentList();

        public Codes CompressTags()
        {
            var tags = codes.Select(code => code.Tag)
                .Concat(codes.Select(code => (code as Jumper)?.GoalTag)).OfType<Tag>().ToArray();
            for (byte i = 0; i < tags.Length && i < byte.MaxValue; i++)
                codes.RenameTag(tags[i], i);
            return codes;
        }
    }

    public static void Add(Func<Codes, Codes> func, [CallerArgumentExpression(nameof(func))] string name = "?") =>
        Optimizers.Add(new Optimizer(name, func));

    public static string Info()
    {
        var infos = Optimizers.Select(optimizer => optimizer.Info).ToList();
        return string.Join("\n", infos) + "\n" + OptimizeInfo.Total(infos);
    }

    static BishOptimizer()
    {
        Add(RemoveUntaggedNop);
        Add(RemoveUnusedTag);
        Add(RemoveInnerOuter);
        Add(RemoveMoveDel);
        Add(RemoveDiscarded);
        Add(RemoveValuePop);
        Add(CombineDefPop);
        Add(CombineDefMemberPop);
        Add(MoveNopTag);
        Add(CompressTags);
    }
}

public class Optimizer(string name, Func<Codes, Codes> func)
{
    public readonly OptimizeInfo Info = new(name);
    public Func<Codes, Codes> Func => func;

    public Codes Optimize(Codes codes)
    {
        Interlocked.Add(ref Info.Before, codes.Count);
        var result = Func(codes).RemoveUntaggedNop();
        Interlocked.Add(ref Info.After, result.Count);
        return result;
    }
}

public class OptimizeInfo(string name, int before = 0, int after = 0)
{
    public string Name => name;
    public int Before = before;
    public int After = after;

    public static OptimizeInfo Total(IList<OptimizeInfo> infos) => new("total", infos[0].Before, infos.Last().After);

    public override string ToString() => $"{Name}: {Before} -> {After} ({1 - (double)After / Before:P})";
}