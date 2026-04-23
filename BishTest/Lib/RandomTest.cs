namespace BishTest.Lib;

public class RandomTest(TestInfoFixture fixture) : LibTest(fixture, "random", ["Random", "random"])
{
    [Theory]
    [Repeat(5)]
    public void TestRandom(int _)
    {
        ExpectTrue("random.rand() is of num and >=0 and <1");
        ExpectResult("random.randInt(0,0)", "0");
        ExpectTrue("random.randInt(1,5) is of int and >=1 and <5");
        ExpectTrue("random.choice(range(1,5)) is of int and >=1 and <5");
        ExpectTrue("l:=random.choices(range(1,5),3);l.length==3" +
                   "&&l.iter().all((item)item is of int and >=1 and <5)");
        ExpectTrue("l:=random.sample(range(1,5),3);l.unique().length==3" +
                   "&&l.iter().all((item)item is of int and >=1 and <5)");
        ExpectTrue("l:=random.shuffle(range(1,5));l.unique().length==4" +
                   "&&l.iter().all((item)item is of int and >=1 and <5)");
    }

    [Theory]
    [Repeat(5)]
    public void TestRandomSeed(int _)
    {
        ExpectResult("Random(114514).rand()", "Random(114514).rand()");
    }
}