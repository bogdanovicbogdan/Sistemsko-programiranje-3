using System;
using Akka.Actor;
using Akka.Event;

namespace Treci_projekat
{
    public class StateActor : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly TimeSpan _pollInterval;
        private readonly IActorRef _sentimentActor;

        public StateActor(TimeSpan pollInterval, SentimentService sentimentService)
        {
            _pollInterval = pollInterval;
            _sentimentActor = Context.ActorOf(SentimentActor.Props(sentimentService), "sentiment");

            Receive<GetAuthorData>(req => GetOrCreateChild(req.AuthorName).Forward(req));
        }

        private IActorRef GetOrCreateChild(string author)
        {
            var childName = SanitizeName(author);
            var child = Context.Child(childName);

            if (child.IsNobody())
            {
                _log.Info($"[STATE ACTOR] Kreiranje AuthorActor za: {author}");
                child = Context.ActorOf(
                    Props.Create(() => new AuthorActor(author, _sentimentActor))
                         .WithDispatcher("gutenberg-dispatcher"),
                    childName);

                child.Tell(new StartPeriodicFetch(author, _pollInterval));
            }

            return child;
        }

        private static string SanitizeName(string author) =>
            Uri.EscapeDataString(author).Replace("%", "-").ToLowerInvariant();

        public static Props CreateProps(TimeSpan pollInterval, SentimentService sentimentService) =>
            Props.Create(() => new StateActor(pollInterval, sentimentService))
                .WithDispatcher("gutenberg-dispatcher");
    }
}
