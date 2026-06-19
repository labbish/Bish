namespace BishTest.Lib;

public class HttpTest : LibTest
{
    public static readonly string Url = Environment.GetEnvironmentVariable("CI") == "true"
        ? "http://localhost:8080"
        : "https://httpbin.org";
    
    public HttpTest(TestInfoFixture fixture) : base(fixture, "http", ["Client"])
    {
        Execute("{.fetch}:=Client();");
        Execute("{.parse,.stringify}:=import('json').JSON;");
        Execute("data:={'name':'zz_404','age':'18'};");
    }

    [Fact]
    public void TestFetch()
    {
        Execute($"r:=await fetch('{Url}/status/200');");
        ExpectResult("r.status", "200");
        ExpectTrue("r.success");

        Execute($"r:=await fetch('{Url}/status/404');");
        ExpectResult("r.status", "404");
        ExpectFalse("r.success");

        Execute($"r:=await fetch('{Url}/get?name=zz_404&age=18');");
        ExpectTrue("r.success");
        ExpectResult("parse(await r.content)['args']", "data");
        
        Execute("o:={.headers:{'X-Custom':'ZZ_404'}};");
        Execute($"r:=await fetch('{Url}/headers',o);");
        ExpectTrue("r.success");
        ExpectResult("parse(await r.content)['headers']['X-Custom']", "'ZZ_404'");
    }

    [Fact]
    public void TestPost()
    {
        Execute("o:={.method:'POST',.body:stringify(data),.mediaType:'application/json'};");
        Execute($"r:=await fetch('{Url}/post',o);");
        ExpectTrue("r.success");
        ExpectResult("parse(await r.content)['json']", "data");
    }
}