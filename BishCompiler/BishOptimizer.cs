using BishBytecode.Bytecodes;

namespace BishCompiler;

public static class BishOptimizer
{
    public static readonly List<Func<Codes, Codes>> Optimizers = [];

    public static Codes Optimize(Codes codes) =>
        Optimizers.Aggregate(codes, (current, optimizer) => optimizer(current));

    extension(Codes codes)
    {
        public Codes RenameTag(string from, string to)
        {
            foreach (var code in codes)
            {
                if (code.Tag == from) code.Tag = to;
                if (code is Jumper jumper && jumper.GoalTag == from) jumper.GoalTag = to;
            }

            return codes;
        }

        public Codes CombineTag(int i)
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

            return codes;
        }

        public Codes Replace(int i, BishBytecode.BishBytecode code)
        {
            codes[i] = code.Tagged(codes[i].Tag);
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
                if (codes[i] is Def def && codes[i + 1] is Pop)
                {
                    codes.CombineTag(i);
                    codes.Replace(i, new Nop());
                    codes.Replace(i + 1, new Move(def.Name));
                }

            return codes;
        }

        public Codes MoveNopTag()
        {
            for (var i = 0; i < codes.Count - 1; i++)
                if (codes[i] is Nop { Tag: not null })
                    codes.CombineTag(i);
            return codes;
        }

        public Codes RemoveUntaggedNop() =>
            codes.Where(code => !(code is Nop && code.Tag is null)).ToList();
    }

    static BishOptimizer()
    {
        // Current optimization percentage: 15%
        Optimizers.Add(RemoveInnerOuter);
        Optimizers.Add(RemoveValuePop);
        Optimizers.Add(CombineDefPop);
        Optimizers.Add(MoveNopTag);
        Optimizers.Add(RemoveUntaggedNop);
    }
}