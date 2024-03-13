// Adapted from https://github.com/fmauNeko/MarketBoardPlugin/blob/develop/MarketBoardPlugin/Helpers/UniversalisClient.cs

using DayTrader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DayTrader
{
    internal class UniversalisClient
    {
        public static async Task<CurrentlyShownView> GetItemInfo(uint itemId, string worldName, int historyCount, CancellationToken cancellationToken)
        {
#if !DEBUG
            switch (worldName)
            {
                case "Aether":
                    return new CurrentlyShownView()
                    {
                        DcName = "Aether",
                        NqSaleVelocity = 16.142857f,
                        CurrentAveragePriceNq = 54.72152f,
                        UnitsForSale = 1292,
                        UnitsSold = 118
                    };
                case "Jenova":
                    return new CurrentlyShownView()
                    {
                        WorldName = "Jenova",
                        WorldID = 40,
                        NqSaleVelocity = 0.85714287f,
                        CurrentAveragePriceNq = 52.5f,
                        UnitsForSale = 162,
                        UnitsSold = 67
                    };
                case "Faerie":
                    return new CurrentlyShownView()
                    {
                        WorldName = "Faerie",
                        WorldID = 54,
                        NqSaleVelocity = 0f,
                        CurrentAveragePriceNq = 59.2f,
                        UnitsForSale = 118,
                        UnitsSold = 53
                    };
                case "Siren":
                    return new CurrentlyShownView()
                    {
                        WorldName = "Siren",
                        WorldID = 57,
                        NqSaleVelocity = 0.71428573f,
                        CurrentAveragePriceNq = 35.4f,
                        UnitsForSale = 334,
                        UnitsSold = 50
                    };
                case "Gilgamesh":
                    return new CurrentlyShownView()
                    {
                        WorldName = "Gilgamesh",
                        WorldID = 63,
                        NqSaleVelocity = 11.714286f,
                        CurrentAveragePriceNq = 221.2f,
                        UnitsForSale = 95,
                        UnitsSold = 118
                    };
                case "Midgardsormr":
                    return new CurrentlyShownView()
                    {
                        WorldName = "Midgardsormr",
                        WorldID = 65,
                        NqSaleVelocity = 0.71428573f,
                        CurrentAveragePriceNq = 20f,
                        UnitsForSale = 118,
                        UnitsSold = 53
                    };
                case "Adamantoise":
                    return new CurrentlyShownView()
                    {
                        WorldName = "Adamantoise",
                        WorldID = 73,
                        NqSaleVelocity = 1.4285715f,
                        CurrentAveragePriceNq = 64.9f,
                        UnitsForSale = 177,
                        UnitsSold = 89
                    };
                case "Cactuar":
                    return new CurrentlyShownView()
                    {
                        WorldName = "Cactuar",
                        WorldID = 79,
                        NqSaleVelocity = 0.71428573f,
                        CurrentAveragePriceNq = 38.3f,
                        UnitsForSale = 98,
                        UnitsSold = 90
                    };
                case "Sargatanas":
                    return new CurrentlyShownView()
                    {
                        WorldName = "Sargatanas",
                        WorldID = 99,
                        NqSaleVelocity = 0f,
                        CurrentAveragePriceNq = 35.8f,
                        UnitsForSale = 190,
                        UnitsSold = 60
                    };
                default:
                    throw new NotImplementedException();
            }

#else
            var uriBuilder = new UriBuilder($"https://universalis.app/api/v2/{worldName}/{itemId}?entries={historyCount}");

            cancellationToken.ThrowIfCancellationRequested();

            using var client = new HttpClient();

            cancellationToken.ThrowIfCancellationRequested();

            var res = await client
                .GetStreamAsync(uriBuilder.Uri, cancellationToken)
                .ConfigureAwait(false);

            var parsedRes = await JsonSerializer
                .DeserializeAsync<CurrentlyShownView>(res, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return parsedRes;
#endif
        }
    }
}
