using Newtonsoft.Json;
using UnityEngine;

public class LeaderboardRequest
{
    [JsonProperty("level")]
    public int level;

    public LeaderboardRequest(int level)
    {
        this.level = level;
    }
}
