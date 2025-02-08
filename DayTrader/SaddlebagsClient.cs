using DayTrader.Models;
using DayTrader.Models.Saddlebags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DayTrader
{
    internal class SaddlebagsClient
    {
        public static async Task<ItemHistory> GetItemInfo(uint itemId, bool itemHq, string worldName, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient())
            {
                var uriBuilder = new UriBuilder($"http://api.saddlebagexchange.com/api/history");
                var jsonPayload = new {
                    item_id = itemId,
                    home_server = worldName,
                    initial_days = 7,
                    end_days = 0,
                    item_type = itemHq ? "hq_only" : "nq_only"
                };

                cancellationToken.ThrowIfCancellationRequested();

                var res = await client
                    .PostAsJsonAsync(uriBuilder.Uri, jsonPayload, cancellationToken)
                    .ConfigureAwait(false);

                //var res = await client
                //    .GetStreamAsync(uriBuilder.Uri, cancellationToken)
                //    .ConfigureAwait(false);

                try
                {
                    var parsedRes = await JsonSerializer
                        .DeserializeAsync<ItemHistory>(res.Content.ReadAsStream(cancellationToken), cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    return parsedRes!;
                } catch (JsonException ex)
                {
                    Service.PluginLog.Debug(ex.Message);
                    throw;
                }
            }
        }
    }
}
