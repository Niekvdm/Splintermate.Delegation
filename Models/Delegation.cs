using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Splintermate.Delegation.Models
{
    public class Delegation
    {
        public string[] Players { get; set; }
        public DelegationCard[] Cards { get; set; }
        public DelegationTokens Tokens { get; set; }
    }

    public class DelegationCard
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("gold")]
        public bool Gold { get; set; }
    }

    public class DelegationTokens
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("threshold")]
        public int Threshold { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}