namespace BishTest.Lib;

public class RandomTest(TestInfoFixture fixture) : LibTest(fixture, "random", ["Random", "random"])
{
    [Fact]
    public void TestRandom() => Parallel.For(0, 5, _ =>
    {
        Result("random.rand()").Should().BeOfType<BishNum>().Which.Value
            .Should().BeGreaterThanOrEqualTo(0).And.BeLessThan(1);
        ExpectResult("random.randInt(0,0)", I(0));
        Result("random.randInt(1,5)").Should().BeOfType<BishInt>().Which.Value
            .Should().BeGreaterThanOrEqualTo(1).And.BeLessThan(5);
        Result("random.choice(range(1,5))").Should().BeOfType<BishInt>().Which.Value
            .Should().BeGreaterThanOrEqualTo(1).And.BeLessThan(5);
        Result("random.choices(range(1,5),3)").Should().BeOfType<BishList>().Which.List.Should().HaveCount(3)
            .And.AllSatisfy(x => x.Should().BeOfType<BishInt>().Which.Value
                .Should().BeGreaterThanOrEqualTo(1).And.BeLessThan(5));
        Result("random.sample(range(1,5),3)").Should().BeOfType<BishList>().Which.List.Should().HaveCount(3)
            // Oh my, I really hate these limitations on Expressions
            .And.OnlyHaveUniqueItems(x => x is BishInt ? ((BishInt)x).Value : -1)
            .And.AllSatisfy(x => x.Should().BeOfType<BishInt>().Which.Value
                .Should().BeGreaterThanOrEqualTo(1).And.BeLessThan(5));
        Result("random.shuffle(range(1,5))").Should().BeOfType<BishList>().Which.List.Should().HaveCount(4)
            .And.OnlyHaveUniqueItems(x => x is BishInt ? ((BishInt)x).Value : -1)
            .And.AllSatisfy(x => x.Should().BeOfType<BishInt>().Which.Value
                .Should().BeGreaterThanOrEqualTo(1).And.BeLessThan(5));
    });

    [Fact]
    public void TestRandomSeed()
    {
        var a = Result("Random(114514).rand()");
        var b = Result("Random(114514).rand()");
        a.Should().BeEquivalentTo(b);
    }
}