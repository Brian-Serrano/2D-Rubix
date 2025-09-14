using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardResponse
{
    [JsonProperty("rank")]
    public int rank;

    [JsonProperty("leaderboard")]
    public List<Leaderboard> leaderboard;
}
