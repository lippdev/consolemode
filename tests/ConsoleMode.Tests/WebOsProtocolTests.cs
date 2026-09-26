using System.Text.Json.Nodes;
using ConsoleMode.Services.Tv;

namespace ConsoleMode.Tests;

public sealed class WebOsProtocolTests
{
    [Fact]
    public void Register_asks_for_a_prompt_with_the_minimal_permissions()
    {
        var message = JsonNode.Parse(WebOsProtocol.Register(null))!.AsObject();
        Assert.Equal("register", message["type"]!.GetValue<string>());
        var payload = message["payload"]!.AsObject();
        Assert.Equal("PROMPT", payload["pairingType"]!.GetValue<string>());
        Assert.False(payload.ContainsKey("client-key"));
        var permissions = payload["manifest"]!["permissions"]!.AsArray().Select(p => p!.GetValue<string>()).ToList();
        Assert.Contains("CONTROL_POWER", permissions);
        Assert.Contains("CONTROL_INPUT_TV", permissions);
    }

    [Fact]
    public void Register_sends_the_saved_client_key()
    {
        var message = JsonNode.Parse(WebOsProtocol.Register("abc123"))!;
        Assert.Equal("abc123", message["payload"]!["client-key"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(1, "HDMI_1")]
    [InlineData(3, "HDMI_3")]
    [InlineData(0, "HDMI_1")]
    [InlineData(7, "HDMI_4")]
    public void Switch_input_targets_the_hdmi_port(int hdmi, string inputId)
    {
        var message = JsonNode.Parse(WebOsProtocol.SwitchInput("input_0", hdmi))!;
        Assert.Equal("request", message["type"]!.GetValue<string>());
        Assert.Equal("input_0", message["id"]!.GetValue<string>());
        Assert.Equal(WebOsProtocol.SwitchInputUri, message["uri"]!.GetValue<string>());
        Assert.Equal(inputId, message["payload"]!["inputId"]!.GetValue<string>());
    }

    [Fact]
    public void Replies_are_classified()
    {
        Assert.Equal(WebOsProtocol.ReplyKind.Prompt,
            WebOsProtocol.Parse("""{"type":"response","id":"register_0","payload":{"pairingType":"PROMPT","returnValue":true}}""").Kind);

        var registered = WebOsProtocol.Parse("""{"type":"registered","id":"register_0","payload":{"client-key":"k1"}}""");
        Assert.Equal(WebOsProtocol.ReplyKind.Registered, registered.Kind);
        Assert.Equal("k1", registered.ClientKey);

        var ok = WebOsProtocol.Parse("""{"type":"response","id":"input_0","payload":{"returnValue":true}}""");
        Assert.Equal((WebOsProtocol.ReplyKind.Response, "input_0"), (ok.Kind, ok.Id));

        var refused = WebOsProtocol.Parse("""{"type":"response","id":"input_0","payload":{"returnValue":false,"errorText":"no input"}}""");
        Assert.Equal((WebOsProtocol.ReplyKind.Error, "no input"), (refused.Kind, refused.Error));

        var error = WebOsProtocol.Parse("""{"type":"error","id":"register_0","error":"403 cancelled"}""");
        Assert.Equal((WebOsProtocol.ReplyKind.Error, "403 cancelled"), (error.Kind, error.Error));

        Assert.Equal(WebOsProtocol.ReplyKind.Other, WebOsProtocol.Parse("not json").Kind);
    }
}
