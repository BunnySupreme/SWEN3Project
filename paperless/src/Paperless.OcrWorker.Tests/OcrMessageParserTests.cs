using log4net;
using log4net.Core;
using Paperless.OcrWorker;

public class OcrMessageParserTests
{
    [Fact]
    public void Parse_ValidMessage_Returns_Id_And_Title()
    {
        var logger = new FakeLogger();
        var json = "{\"DocumentId\":\"00000000-0000-0000-0000-000000000001\",\"DocumentTitle\":\"Rechnung\"}";

        var (id, title) = OcrMessageParser.Parse(json, logger);

        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), id);
        Assert.Equal("Rechnung", title);
    }

    [Fact]
    public void Parse_InvalidJson_LogsWarning_And_Returns_Defaults()
    {
        var logger = new FakeLogger();
        var json = "{not valid json";

        var (id, title) = OcrMessageParser.Parse(json, logger);

        Assert.Equal(Guid.Empty, id);
        Assert.Null(title);
    }
}
