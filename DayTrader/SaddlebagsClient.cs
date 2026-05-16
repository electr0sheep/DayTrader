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
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DayTrader");
                var uriBuilder = new UriBuilder($"https://docs.saddlebagexchange.com/api/ffxiv/v2/history");
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

                var body = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var parsedRes = JsonSerializer.Deserialize<ItemHistory>(body);
                    return parsedRes!;
                } catch (JsonException ex)
                {
                    Service.PluginLog.Error($"Failed to parse saddlebags response (status {(int)res.StatusCode}): {ex.Message}");
                    Service.PluginLog.Error($"Body: {body}");
                    throw;
                }
            }
        }
    }
}
