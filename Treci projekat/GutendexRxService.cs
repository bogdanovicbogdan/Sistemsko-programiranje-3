using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Newtonsoft.Json;

namespace Treci_projekat
{
    public class GutendexRxService
    {
        private static readonly HttpClient _client = new HttpClient();

        static GutendexRxService()
        {
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public IObservable<BookBatch> PollAuthorBooksPeriodically(string author, TimeSpan interval)
        {
            return Observable.Defer(() => BuildPollStream(author, interval))
                .Retry();
        }

        private IObservable<BookBatch> BuildPollStream(string author, TimeSpan interval)
        {
            return Observable
                .Timer(TimeSpan.Zero, interval, TaskPoolScheduler.Default)
                .SelectMany(tick => 
                    FetchAllPages($"https://gutendex.com/books/?search={Uri.EscapeDataString(author)}", 5)
                    .SelectMany(list => list)
                    .ToList()
                    .Select(list => list.ToList())
                    .SubscribeOn(TaskPoolScheduler.Default)
                    .Timeout(TimeSpan.FromSeconds(35))
                    .Retry(2)
                    .Catch<List<Book>, Exception>(ex =>
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] API ERROR | Author: {author} | {ex.Message}");
                        return Observable.Return(new List<Book>());
                    })
                )
                .Select(list =>
                {
                    var filtered = list
                        .Where(b => b.Authors.Any(a => a.Name.Contains(author, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    return new BookBatch(author, filtered);
                })
                .ObserveOn(TaskPoolScheduler.Default);
        }

        private IObservable<List<Book>> FetchAllPages(string url, int pageLimit)
        {
            if (pageLimit <= 0 || string.IsNullOrEmpty(url))
                return Observable.Return(new List<Book>());

            return Observable.FromAsync(async () =>
            {
                using (var response = await _client.GetAsync(url))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new HttpRequestException("Gutendex API returned 404 Not Found.");
                    }
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new HttpRequestException("Gutendex API returned 403 Forbidden. Cloudflare blocking may be active.");
                    }
                    if (response.StatusCode == (System.Net.HttpStatusCode)429)
                    {
                        throw new HttpRequestException("Gutendex API returned 429 Too Many Requests. Rate limiting active.");
                    }
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new HttpRequestException("Gutendex API returned 401 Unauthorized.");
                    }
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        throw new HttpRequestException("Gutendex API returned 400 Bad Request.");
                    }

                    response.EnsureSuccessStatusCode();
                    var responseString = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<GutendexResponse>(responseString);
                    return data ?? new GutendexResponse();
                }
            })
            .SelectMany(response =>
            {
                if (string.IsNullOrEmpty(response.Next) || response.Results.Count == 0)
                {
                    return Observable.Return(response.Results);
                }
                else
                {
                    return Observable.Return(response.Results)
                        .Concat(FetchAllPages(response.Next, pageLimit - 1));
                }
            });
        }
    }
}
