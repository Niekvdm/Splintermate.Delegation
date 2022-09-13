using Newtonsoft.Json;

namespace Splintermate.Delegation.Models;

public class TokenTransferBody
{
    [JsonProperty("players")]
    public string[] Players { get; set; }
    
    [JsonProperty("mode")]
    public string Mode { get; set; }
    
    [JsonProperty("token")]
    public string Token { get; set; }
    
    [JsonProperty("threshold")]
    public int Threshold { get; set; }
    
    [JsonProperty("quantity")]
    public int Quantity { get; set; }
}