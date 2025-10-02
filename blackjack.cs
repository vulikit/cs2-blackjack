using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using StoreApi;

namespace cs2_blackjack
{
    public partial class Blackjack : BasePlugin, IPluginConfig<BlackJackConfig>
    {
        public override string ModuleName => "cs2-blackjack";
        public override string ModuleVersion => "0.0.1";
        public override string ModuleAuthor => "varkit (dc: vulikit)";
        public IStoreApi? StoreApi { get; set; }
        public string prefix { get; set; }
        public BlackJackConfig Config { get; set; }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            Store.LoadStoreApi(this);

            RegisterListener<Listeners.OnTick>(OnTick);
        }

        public void OnConfigParsed(BlackJackConfig config)
        {
            Config = config;
            prefix = config.Prefix.ReplaceColorTags();
            minBet = config.MinimumBet;
            maxBet = config.MaximumBet;
        }

        public int minBet { get; set; }
        public int maxBet { get; set; }

        public Dictionary<ulong, GameState> activeGames = new();
        public Dictionary<ulong, string> endGameStates = new();
        public Random rng = new Random();

        public void OnTick()
        {
            foreach (var (steamId, game) in activeGames)
            {
                var player = Utilities.GetPlayerFromSteamId(steamId);
                if (player == null || !player.IsValid) continue;

                ShowGameState(player, game);
            }

            foreach (var (steamId, html) in endGameStates)
            {
                var player = Utilities.GetPlayerFromSteamId(steamId);
                if (player == null || !player.IsValid) continue;

                player.PrintToCenterHtml(html);
            }
        }

        [ConsoleCommand("css_bj")]
        [ConsoleCommand("css_blackjack")]
        public void Command_Blackjack(CCSPlayerController? caller, CommandInfo command)
        {
            int playerCredits = Store.Credit_GetCredit(caller);
            var beta = command.GetArg(1);
            if (command.ArgCount != 2)
            {
                reply(caller, Localizer["Not Enough Args"]);
                return;
            }

            if (!int.TryParse(beta, out int betAmount))
            {
                reply(caller, Localizer["Incorrect Integer"]);
                return;
            }

            if (betAmount > playerCredits)
            {
                reply(caller, Localizer["Not Enough Credits", playerCredits]);
                return;
            }

            if (betAmount > maxBet)
            {
                reply(caller, Localizer["Maximum Bet", maxBet]);
                return;
            }

            if (betAmount < minBet)
            {
                reply(caller, Localizer["Minimum Bet", minBet]);
                return;
            }

            StartGame(caller, betAmount);
        }

        [ConsoleCommand("css_hit")]
        public void Command_Hit(CCSPlayerController? player, CommandInfo commandInfo)
        {
            HandleHit(player, player.AuthorizedSteamID.SteamId64);
        }

        [ConsoleCommand("css_stand")]
        public void Command_Stand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            HandleStand(player, player.AuthorizedSteamID.SteamId64);
        }

        public void StartGame(CCSPlayerController? player, int betAmount)
        {
            if (player == null || !player.IsValid)
                return;

            ulong steamId = player.SteamID;
            if (activeGames.ContainsKey(steamId))
            {
                reply(player, Localizer["Already Playing A Game"]);
                return;
            }

            Store.Credit_AddCredit(player, -betAmount);

            GameState game = new GameState
            {
                Deck = CreateDeck(),
                PlayerHand = new List<Card>(),
                DealerHand = new List<Card>(),
                BetAmount = betAmount
            };

            game.PlayerHand.Add(DrawCard(game));
            game.PlayerHand.Add(DrawCard(game));
            game.DealerHand.Add(DrawCard(game));
            activeGames[steamId] = game;

            reply(player, Localizer["Starting A Game", betAmount]);
        }

        public void HandleHit(CCSPlayerController player, ulong steamId)
        {
            if (!activeGames.TryGetValue(steamId, out GameState game))
            {
                reply(player, Localizer["No Active Game"]);
                return;
            }

            game.PlayerHand.Add(DrawCard(game));
            int playerTotal = CalculateHand(game.PlayerHand);
            reply(player, Localizer["Hit"]);

            if (playerTotal > 21)
            {
                ShowEndGame(player, steamId, Localizer["Bust"], "#FF0000", Localizer["More Than 21"]);
            }
        }

        public void HandleStand(CCSPlayerController player, ulong steamId)
        {
            if (!activeGames.TryGetValue(steamId, out GameState game))
            {
                reply(player, Localizer["No Active Game"]);
                return;
            }

            while (CalculateHand(game.DealerHand) < 17)
                game.DealerHand.Add(DrawCard(game));

            int playerTotal = CalculateHand(game.PlayerHand);
            int dealerTotal = CalculateHand(game.DealerHand);
            bool playerWins = dealerTotal > 21 || (playerTotal <= 21 && playerTotal > dealerTotal);
            reply(player, Localizer["Stand"]);
            string title, color, message;
            if (playerWins)
            {
                reply(player, Localizer["Win", game.BetAmount * 2]);
                title = Localizer["WinTitle"];
                color = "#00FF00";
                message = $"{Localizer["Prize"]}: {game.BetAmount * 2}";
                Store.Credit_AddCredit(player, game.BetAmount * 2);
            }
            else if (playerTotal == dealerTotal)
            {
                reply(player, Localizer["Draw", game.BetAmount]);
                title = Localizer["DrawTitle"];
                color = "#FFA500";
                message = Localizer["CashBack"];
                Store.Credit_AddCredit(player, game.BetAmount);
            }
            else
            {
                reply(player, Localizer["Lose"]);
                title = Localizer["LoseTitle"];
                color = "#FF0000";
                message = Localizer["DealerWon"];
            }

            ShowEndGame(player, steamId, title, color, message, dealerTotal);
        }

        public void ShowGameState(CCSPlayerController player, GameState game)
        {
            string playerHand = HandToString(game.PlayerHand, game.PlayerHand);
            int playerTotal = CalculateHand(game.PlayerHand);
            string dealerHand = $"{HandToString(new List<Card> { game.DealerHand[0] }, game.DealerHand)} <img src='https://raw.githubusercontent.com/vulikit/varkit-resources/refs/heads/main/bj/53.jpg' width='50'>";

            string html = $@"
                <center>
                    <font color='white'>Black</font><font color='red'>Jack</font> <font color='#d255dc'>({Localizer["Bet"]}: {game.BetAmount})</font><br>
                    <font color='#dc5555'><b>{Localizer["Dealer"]}:</b><br>{dealerHand}<br></font>
                    <font color='#d8dc55'><b>{Localizer["You"]}:</b><br>{playerHand}</font><br>
                    <font color='#61dc55' class='fontSize-l'>{Localizer["HitTitle"]}: !hit</font> <font color='#dc5555' class='fontSize-l'>{Localizer["StandTitle"]}: !stand</font>
                </center>";

            player.PrintToCenterHtml(html);
        }

        public void ShowEndGame(CCSPlayerController player, ulong steamId, string title, string color, string message, int dealerTotal = -1)
        {
            if (!activeGames.TryGetValue(steamId, out GameState game)) return;

            string dealerHand = HandToString(game.DealerHand, game.DealerHand);
            string playerHand = HandToString(game.PlayerHand, game.PlayerHand);

            string html = $@"
                <center>
                    <font color='white'>Black</font><font color='red'>Jack</font> <font color='#d255dc'>({Localizer["Bet"]}: {game.BetAmount})</font><br>
                    <font color='#dc5555'><b>{Localizer["Dealer"]}:</b><br>{dealerHand}<br></font>
                    <font color='#d8dc55'><b>{Localizer["You"]}:</b><br>{playerHand}</font><br>
                    <font color='{color}' class='fontSize-l'>{title} {message}</font>
                </center>";

            endGameStates[steamId] = html;
            AddTimer(2, () =>
            {
                endGameStates.Remove(steamId);
            });
            activeGames.Remove(steamId);
        }

        public List<Card> CreateDeck()
        {
            string[] suits = { "♣", "♦", "♥", "♠" };
            string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
            List<Card> deck = new();
            foreach (var suit in suits)
                foreach (var rank in ranks)
                    deck.Add(new Card(rank, suit));
            ShuffleDeck(deck);
            return deck;
        }

        public void ShuffleDeck(List<Card> deck)
        {
            int n = deck.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Card temp = deck[k];
                deck[k] = deck[n];
                deck[n] = temp;
            }
        }

        public Card DrawCard(GameState game)
        {
            if (game.Deck.Count == 0) game.Deck.AddRange(CreateDeck());
            Card card = game.Deck[0];
            game.Deck.RemoveAt(0);
            return card;
        }

        public int CalculateHand(List<Card> hand)
        {
            int total = 0, aces = 0;
            foreach (var card in hand)
            {
                if (card.Rank == "A") aces++;
                else if (card.Rank == "J" || card.Rank == "Q" || card.Rank == "K") total += 10;
                else total += int.Parse(card.Rank);
            }
            for (int i = 0; i < aces; i++)
                total += (total + 11 <= 21) ? 11 : 1;
            return total;
        }

        public int GetCardValue(Card card, List<Card> hand)
        {
            if (card.Rank == "A")
            {
                int totalWithoutAces = 0;
                int aces = 0;
                foreach (var c in hand)
                {
                    if (c.Rank == "A") aces++;
                    else if (c.Rank == "J" || c.Rank == "Q" || c.Rank == "K") totalWithoutAces += 10;
                    else totalWithoutAces += int.Parse(c.Rank);
                }
                for (int i = 0; i < aces; i++)
                    totalWithoutAces += (totalWithoutAces + 11 <= 21) ? 11 : 1;
                return totalWithoutAces - CalculateHand(hand.Where(c => c != card).ToList());
            }
            else if (card.Rank == "J" || card.Rank == "Q" || card.Rank == "K")
                return 10;
            else
                return int.Parse(card.Rank);
        }

        public string HandToString(List<Card> hand, List<Card> fullHand)
        {
            string cardsHtml = string.Join(" ", hand.Select(c => $"<img src='{GetCardImageUrl(c)}' width='50'>"));
            string valuesHtml = string.Join(" + ", hand.Select(c => $"{GetCardValue(c, fullHand)}"));
            int total = CalculateHand(fullHand);
            if (hand.Count == 1)
                valuesHtml = $"({GetCardValue(hand[0], fullHand)})";
            else
                valuesHtml = $"({valuesHtml} = {total})";

            return $"<div style='display:inline-block; vertical-align:middle; margin:0 5px'>{cardsHtml}</div><div style='display:inline-block; vertical-align:middle; color:#00FFFF; margin-left:10px'>{valuesHtml}</div>";
        }

        public string GetCardImageUrl(Card card)
        {
            string[] suits = { "♣", "♦", "♥", "♠" };
            string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
            int suitIndex = Array.IndexOf(suits, card.Suit);
            int rankIndex = Array.IndexOf(ranks, card.Rank);
            int cardNumber = suitIndex * 13 + rankIndex + 1;
            return $"https://raw.githubusercontent.com/vulikit/varkit-resources/refs/heads/main/bj/{cardNumber}.jpg";
        }
        public void reply(CCSPlayerController player, string ms)
        {
            player.PrintToChat(prefix + ms);
        }
    }


    public class Card
    {
        public string Rank { get; }
        public string Suit { get; }
        public Card(string rank, string suit) { Rank = rank; Suit = suit; }
        public override string ToString() => $"{Rank}{Suit}";
    }

    public class GameState
    {
        public List<Card> Deck { get; set; } = new();
        public List<Card> PlayerHand { get; set; } = new();
        public List<Card> DealerHand { get; set; } = new();
        public int BetAmount { get; set; }
    }
}