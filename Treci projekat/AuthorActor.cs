using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;

namespace Treci_projekat
{
    public class AuthorActor : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly string _author;
        private readonly IActorRef _sentimentActor;
        private readonly GutendexRxService _rxService = new GutendexRxService();

        private readonly List<BookSentimentDetail> _books = new List<BookSentimentDetail>();
        private readonly HashSet<long> _analyzedBookIds = new HashSet<long>();
        private IDisposable? _rxSubscription;
        private bool _isReady = false;
        private bool _isNotFound = false;

        public AuthorActor(string author, IActorRef sentimentActor)
        {
            _author = author;
            _sentimentActor = sentimentActor;

            Receive<StartPeriodicFetch>(start =>
            {
                if (_rxSubscription != null) return;

                var self = Self;
                _rxSubscription = _rxService
                    .PollAuthorBooksPeriodically(_author, start.Interval)
                    .Subscribe(
                        onNext: batch => self.Tell(batch),
                        onError: ex => _log.Error(ex, "Rx subscription error")
                    );
            });

            Receive<BookBatch>(batch =>
            {
                if (batch.Books.Count == 0 && _analyzedBookIds.Count == 0)
                {
                    _isReady = true;
                    _isNotFound = true;
                    return;
                }

                _isNotFound = false;

                var newBooks = batch.Books
                    .Where(b => !_analyzedBookIds.Contains(b.Id))
                    .ToList();

                if (newBooks.Count == 0)
                {
                    _isReady = true;
                    return;
                }

                Context.ActorOf(Props.Create(() => new AuthorQueryActor(newBooks, _sentimentActor, Self)));
            });

            Receive<NewBooksAnalyzed>(msg =>
            {
                foreach (var detail in msg.Books)
                {
                    if (_analyzedBookIds.Add(detail.Id))
                    {
                        _books.Add(detail);
                    }
                }
                _isReady = true;
            });

            Receive<GetAuthorData>(req =>
            {
                Sender.Tell(new AuthorDataResponse(_author, new List<BookSentimentDetail>(_books), _isReady, _isNotFound));
            });
        }

        protected override void PostStop()
        {
            _rxSubscription?.Dispose();
            base.PostStop();
        }
    }

    public class NewBooksAnalyzed
    {
        public List<BookSentimentDetail> Books { get; }
        public NewBooksAnalyzed(List<BookSentimentDetail> books)
        {
            Books = books;
        }
    }

    public class AuthorQueryActor : ReceiveActor
    {
        private readonly IActorRef _parent;
        private readonly Dictionary<long, BookSentimentDetail> _bookDetails = new Dictionary<long, BookSentimentDetail>();
        private int _totalExpectedReplies = 0;
        private int _receivedReplies = 0;

        public AuthorQueryActor(List<Book> newBooks, IActorRef sentimentActor, IActorRef parent)
        {
            _parent = parent;

            foreach (var book in newBooks)
            {
                var detail = new BookSentimentDetail
                {
                    Id = book.Id,
                    Title = book.Title
                };
                _bookDetails.Add(book.Id, detail);

                if (book.Summaries.Count == 0)
                {
                    _totalExpectedReplies++;
                    sentimentActor.Tell(new AnalyzeSentimentRequest(book.Id, book.Title, book.Title));
                }
                else
                {
                    foreach (var summary in book.Summaries)
                    {
                        _totalExpectedReplies++;
                        sentimentActor.Tell(new AnalyzeSentimentRequest(book.Id, book.Title, summary));
                    }
                }
            }

            Receive<AnalyzeSentimentResponse>(res =>
            {
                if (_bookDetails.TryGetValue(res.BookId, out var detail))
                {
                    detail.Summaries.Add(new SummarySentiment
                    {
                        Text = res.Text,
                        Sentiment = res.Sentiment ? "Positive" : "Negative",
                        Score = res.Score,
                        Probability = res.Probability
                    });
                }

                _receivedReplies++;

                if (_receivedReplies == _totalExpectedReplies)
                {
                    _parent.Tell(new NewBooksAnalyzed(_bookDetails.Values.ToList()));
                    Context.Stop(Self);
                }
            });
        }
    }
}
