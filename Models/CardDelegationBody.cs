using Newtonsoft.Json;

namespace Splintermate.Delegation.Models;

public class CardDelegationBody
{
    [JsonProperty("players")]
    public string[] Players { get; set; }
    
    [JsonProperty("cards")]
    public DelegationCard[] Cards { get; set; }
}