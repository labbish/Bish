namespace BishTest.Shell;

public class ShellTest : Test, IDisposable, IAsyncDisposable
{
    protected readonly TextWriter OrigWriter;
    protected readonly StringWriter Writer = new();
    protected readonly SemaphoreSlim Semaphore = new(1, 1);

    protected static void CreateDirectory(string path) => Directory.CreateDirectory(path);
    protected static void CreateFile(string path, string content = "") => File.WriteAllText(path, content);

    protected async Task<string> GetOutputAsync(params string[] args)
    {
        await Semaphore.WaitAsync();
        try
        {
            await Bish.Program.Main(args);
            var output = Writer.ToString();
            Writer.GetStringBuilder().Clear();
            return output;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    protected async Task ExpectOutputAsync(params string[] argsOutput)
    {
        var args = argsOutput[..^1];
        var expect = argsOutput[^1];
        var output = (await GetOutputAsync(args)).Trim();
        if (output != expect) Fail($"Expect {expect}, got {output}");
    }

    public ShellTest(TestInfoFixture fixture) : base(fixture)
    {
        CreateDirectory("./a/b/c");
        CreateFile("./a/rubbish.json");
        CreateFile("./a/b/c/rubbish.json");

        OrigWriter = Console.Out;
        Console.SetOut(Writer);
    }

    [Fact]
    public async Task TestCommand()
    {
        await ExpectOutputAsync("-c", "print('Hello!');", "Hello!");
    }

    [Fact]
    public async Task TestFile()
    {
        CreateFile("./a/a0.bish", "print('Hello!');");
        await ExpectOutputAsync("-f", "./a/a0.bish", "Hello!");
        await ExpectOutputAsync("-f", "./a/a0.bish", "-o", "./a/a0.bishc", "Hello!");
        await ExpectOutputAsync("-f", "./a/a0.bishc", "Hello!");
        await ExpectOutputAsync("-f", "./a/a0.bish", "-o", "./a/a0.bishc", "-s", "");
        await ExpectOutputAsync("-f", "./a/a0.bishc", "Hello!");
    }

    [Fact]
    public async Task TestImport()
    {
        CreateFile("./a/a1.bish", "a:='a';");
        CreateFile("./a/b/b1.bish", "b:='b';");
        CreateFile("./a/b/c/c1.bish", "c:='c';");

        const string s0 = "print(import('a/a1.bish').a+import('a/b/b1.bish').b+import('a/b/c/c1.bish').c);";
        const string s1 = "print(import('a1.bish').a+import('b/b1.bish').b+import('b/c/c1.bish').c);";
        const string s2 = "print(import('../../a1.bish').a+import('../b1.bish').b+import('c1.bish').c);";
        
        CreateFile("./a/a2.bish", s1);
        CreateFile("./a/b/b2.bish", s1);
        CreateFile("./a/b/c/c2.bish", s2);

        await ExpectOutputAsync("-c", s0, "abc");
        await ExpectOutputAsync("-f", "./a/a2.bish", "abc");
        await ExpectOutputAsync("-f", "./a/b/b2.bish", "abc");
        await ExpectOutputAsync("-f", "./a/b/c/c2.bish", "abc");
    }

    [Fact]
    public async Task TestCompiled()
    {
        CreateFile("./a/a3.bish", "a:='a';");
        CreateFile("./a/b/b3.bish", "b:='b';");
        CreateFile("./a/b/c/c3.bish", "c:='c';");

        const string s0 = "print(import('a/a3.bishc').a+import('a/b/b3.bishc').b+import('a/b/c/c3.bishc').c);";
        const string s1 = "print(import('a3.bishc').a+import('b/b3.bishc').b+import('b/c/c3.bishc').c);";
        const string s2 = "print(import('../../a3.bishc').a+import('../b3.bishc').b+import('c3.bishc').c);";
        
        CreateFile("./a/a4.bish", s1);
        CreateFile("./a/b/b4.bish", s1);
        CreateFile("./a/b/c/c4.bish", s2);

        foreach (var file in new[] { "./a/a", "./a/b/b", "./a/b/c/c" })
        {
            await GetOutputAsync("-f", $"{file}3.bish", "-o", $"{file}3.bishc", "-s");
            await GetOutputAsync("-f", $"{file}4.bish", "-o", $"{file}4.bishc", "-s");
        }

        await ExpectOutputAsync("-c", s0, "abc");
        await ExpectOutputAsync("-f", "./a/a4.bishc", "abc");
        await ExpectOutputAsync("-f", "./a/b/b4.bishc", "abc");
        await ExpectOutputAsync("-f", "./a/b/c/c4.bishc", "abc");
    }

    [Fact]
    public async Task TestMeta()
    {
        const string s = "print(meta.root);";
        
        CreateFile("./a/a5.bish", s);
        CreateFile("./a/b/b5.bish", s);
        CreateFile("./a/b/c/c5.bish", s);

        var p0 = Path.GetFullPath(".");
        var p1 = Path.GetFullPath("a");
        var p2 = Path.GetFullPath("a/b/c");

        await ExpectOutputAsync("-c", s, p0);
        await ExpectOutputAsync("-f", "./a/a5.bish", p1);
        await ExpectOutputAsync("-f", "./a/b/b5.bish", p1);
        await ExpectOutputAsync("-f", "./a/b/c/c5.bish", p2);
        
        CreateFile("./a/ax.bish", "return 0;");
        await GetOutputAsync("-f", "./a/ax.bish", "-o", "./a/ax.bishc", "-s");
        await ExpectOutputAsync("-c", "print(meta.compileFile('./a/ax.bish').execute());", "0");
        await ExpectOutputAsync("-c", "print(meta.compileFile('./a/ax.bishc').execute());", "0");
    }

    public void Dispose()
    {
        Console.SetOut(OrigWriter);
        Writer.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        Console.SetOut(OrigWriter);
        await Writer.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}