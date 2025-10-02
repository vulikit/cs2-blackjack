using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace cs2_blackjack
{
    public class BlackJackConfig : BasePluginConfig
    {
        [JsonPropertyName("Prefix")]
        public string Prefix { get; set; } = "{blue}⌈ Blackjack ⌋";

        [JsonPropertyName("MaximumBet")]
        public int MaximumBet { get; set; } = 999999;

        [JsonPropertyName("MinimumBet")]
        public int MinimumBet { get; set; } = 100;
    }
}
