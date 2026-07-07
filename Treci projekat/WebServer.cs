using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Newtonsoft.Json;

namespace Treci_projekat
{
    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly IActorRef _stateActor;
        private readonly string _prefix;

        public WebServer(IActorRef stateActor, string prefix = "http://localhost:8080/books/")
        {
            _stateActor = stateActor;
            _prefix = prefix;
            _listener.Prefixes.Add(prefix);
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Server je pokrenut");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Listening on: {_prefix}");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Primer: {_prefix}?author=shakespeare");
        }

        public void Stop() => _listener.Stop();
        public void Close() => _listener.Close();

        public async Task RunAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var context = await _listener.GetContextAsync();
                    _ = HandleRequest(context).ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] REQUEST ERROR: {t.Exception?.GetBaseException().Message}");
                    }, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            catch (HttpListenerException) when (token.IsCancellationRequested) { }
        }

        private async Task HandleRequest(HttpListenerContext ctx)
        {
            var author = ctx.Request.QueryString["author"];
            var requestId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                if (string.IsNullOrWhiteSpace(author))
                {
                    await WriteJson(ctx, HttpStatusCode.BadRequest, new { error = "Query parametar 'author' je obavezan." });
                    return;
                }

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Zahtev [{requestId}] | Author: {author}");

                var result = await _stateActor.Ask<AuthorDataResponse>(
                    new GetAuthorData(author),
                    TimeSpan.FromSeconds(15)
                );

                if (result.IsNotFound)
                {
                    await WriteJson(ctx, HttpStatusCode.NotFound, new
                    {
                        author = result.AuthorName,
                        status = "not_found",
                        message = $"Nisu pronadjene knjige za autora {result.AuthorName}."
                    });
                }
                else if (!result.IsReady)
                {
                    await WriteJson(ctx, HttpStatusCode.ServiceUnavailable, new
                    {
                        author = result.AuthorName,
                        status = "loading",
                        message = "Podaci se preuzimaju i sentiment se analizira. Molimo vas da osvežite stranicu za par sekundi."
                    });
                }
                else
                {
                    await WriteJson(ctx, HttpStatusCode.OK, new
                    {
                        author = result.AuthorName,
                        count = result.Books.Count,
                        books = result.Books
                    }, indented: true);
                }
            }
            catch (TaskCanceledException)
            {
                await WriteJson(ctx, HttpStatusCode.ServiceUnavailable, new
                {
                    error = "Timeout kod obrade zahteva, pokušajte ponovo."
                });
            }
            catch (Exception ex)
            {
                await WriteJson(ctx, HttpStatusCode.InternalServerError, new
                {
                    error = ex.Message
                });
            }
            finally
            {
                ctx.Response.Close();
            }
        }

        private static async Task WriteJson(HttpListenerContext ctx, HttpStatusCode status, object payload, bool indented = false)
        {
            var json = JsonConvert.SerializeObject(payload, indented ? Formatting.Indented : Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(json);
            ctx.Response.StatusCode = (int)status;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
