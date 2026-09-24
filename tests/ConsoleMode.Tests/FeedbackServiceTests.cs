using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class FeedbackServiceTests
{
    private const string Environment = "Console Mode 1.4.0 (installed)\nWindows 10.0.26200 (X64)\nLanguage: pt-BR";

    [Fact]
    public void BuildIssueUrl_OpensTheFeedbackFormOfTheRepository()
    {
        var uri = new Uri(FeedbackService.BuildIssueUrl("lippdev/consolemode", Environment));

        Assert.Equal("github.com", uri.Host);
        Assert.Equal("/lippdev/consolemode/issues/new", uri.AbsolutePath);
        Assert.Equal("feedback.yml", Query(uri)["template"]);
    }

    [Fact]
    public void BuildIssueUrl_PrefillsTheEnvironmentFieldVerbatim()
    {
        var uri = new Uri(FeedbackService.BuildIssueUrl("lippdev/consolemode", Environment));

        Assert.Equal(Environment, Query(uri)["environment"]);
    }

    [Fact]
    public void BuildIssueUrl_EscapesCharactersThatWouldBreakTheQuery()
    {
        var url = FeedbackService.BuildIssueUrl("lippdev/consolemode", "a&b=c #d\né");

        Assert.DoesNotContain(" ", url);
        Assert.DoesNotContain("#", url);
        Assert.Equal("a&b=c #d\né", Query(new Uri(url))["environment"]);
    }

    [Fact]
    public void BuildEnvironment_DescribesAppWindowsAndLanguage()
    {
        var text = FeedbackService.BuildEnvironment("1.4.0", false, "10.0.26200.0", "X64", "en-US");

        Assert.Equal("Console Mode 1.4.0 (portable)\nWindows 10.0.26200.0 (X64)\nLanguage: en-US", text);
    }

    [Fact]
    public void BuildIssueUrl_StaysWellBelowBrowserUrlLimits()
    {
        var environment = FeedbackService.BuildEnvironment("1.4.0", true, "10.0.26200.0", "X64", "pt-BR");

        Assert.True(FeedbackService.BuildIssueUrl("lippdev/consolemode", environment).Length < 8000);
    }

    private static Dictionary<string, string> Query(Uri uri) =>
        uri.Query.TrimStart('?').Split('&')
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(parts => parts[0], parts => Uri.UnescapeDataString(parts[1]));
}
