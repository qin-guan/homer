using System.Text.Json.Serialization;
using Refit;

namespace Homer.NetDaemon.Apps.Kdk;

public record KdkAuthPostTokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("refresh_token")]
    string? RefreshToken
);