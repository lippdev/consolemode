using ConsoleMode.Services.Tv;

namespace ConsoleMode.Tests;

public sealed class HomeAssistantApiTests
{
    [Theory]
    [InlineData("script.tv_to_pc", "script", "turn_on")]
    [InlineData("scene.game_night", "scene", "turn_on")]
    [InlineData("automation.tv_pc", "automation", "trigger")]
    [InlineData("button.tv_power", "button", "press")]
    [InlineData("media_player.living_room", "media_player", "turn_on")]
    [InlineData(" switch.tv ", "switch", "turn_on")]
    public void Start_runs_the_entity_with_its_domain_service(string entity, string domain, string service)
    {
        var call = HomeAssistantApi.TurnOn(entity);
        Assert.NotNull(call);
        Assert.Equal((domain, service, entity.Trim()), (call.Value.Domain, call.Value.Service, call.Value.EntityId));
    }

    [Fact]
    public void Restore_prefers_the_off_entity()
    {
        var call = HomeAssistantApi.TurnOff("media_player.tv", "script.tv_off");
        Assert.Equal(("script", "turn_on", "script.tv_off"), (call!.Value.Domain, call.Value.Service, call.Value.EntityId));
    }

    [Fact]
    public void Restore_turns_a_stateful_start_entity_off()
    {
        var call = HomeAssistantApi.TurnOff("media_player.tv", "");
        Assert.Equal(("media_player", "turn_off"), (call!.Value.Domain, call.Value.Service));
    }

    [Theory]
    [InlineData("script.tv_to_pc")]
    [InlineData("scene.game_night")]
    [InlineData("")]
    public void Restore_does_nothing_when_the_start_entity_is_an_action(string onEntity)
    {
        Assert.Null(HomeAssistantApi.TurnOff(onEntity, ""));
    }

    [Theory]
    [InlineData("")]
    [InlineData("tv")]
    [InlineData("script.")]
    [InlineData(".tv")]
    [InlineData("Script.TV")]
    [InlineData("script.tv.pc")]
    [InlineData("script.tv pc")]
    public void Invalid_entities_are_rejected(string entity)
    {
        Assert.False(HomeAssistantApi.IsValidEntity(entity));
        Assert.Null(HomeAssistantApi.TurnOn(entity));
    }

    [Theory]
    [InlineData("http://homeassistant.local:8123", "http://homeassistant.local:8123/api/services/script/turn_on")]
    [InlineData("http://192.168.0.2:8123/", "http://192.168.0.2:8123/api/services/script/turn_on")]
    [InlineData("homeassistant.local:8123", "http://homeassistant.local:8123/api/services/script/turn_on")]
    [InlineData("https://ha.example.com", "https://ha.example.com/api/services/script/turn_on")]
    public void Service_uri_is_built_from_the_base_url(string baseUrl, string expected)
    {
        var call = HomeAssistantApi.TurnOn("script.tv_to_pc")!.Value;
        Assert.Equal(expected, HomeAssistantApi.ServiceUri(baseUrl, call).ToString());
    }

    [Fact]
    public void Body_carries_the_entity_id()
    {
        var call = HomeAssistantApi.TurnOn("media_player.tv")!.Value;
        Assert.Equal("""{"entity_id":"media_player.tv"}""", HomeAssistantApi.Body(call));
    }
}
