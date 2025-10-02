using CounterStrikeSharp.API.Core;
using StoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cs2_blackjack
{
    public static class Store
    {
        public static IStoreApi StoreApi { get; set; }
        public static void LoadStoreApi(Blackjack plugin)
        {
            plugin.StoreApi = IStoreApi.Capability.Get() ?? throw new Exception("Store Api Error!");
            StoreApi = plugin.StoreApi;
        }

        public static int Credit_GetCredit(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid || StoreApi == null)
                return 0;

            return StoreApi.GetPlayerCredits(player);
        }

        public static void Credit_AddCredit(CCSPlayerController? player, int amount)
        {
            if (player == null || !player.IsValid || StoreApi == null)
                return;

            StoreApi.GivePlayerCredits(player, amount);
        }
    }
}
