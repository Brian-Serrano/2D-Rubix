using Newtonsoft.Json;
using UnityEngine;

public class Leaderboard
{
    [JsonProperty("rank")]
    public int rank;

    [JsonProperty("player_id")]
    public int playerId;

    [JsonProperty("player_name")]
    public string playerName;

    [JsonProperty("level")]
    public int level;
}
