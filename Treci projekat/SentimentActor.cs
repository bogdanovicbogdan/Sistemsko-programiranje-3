using Akka.Actor;

namespace Treci_projekat
{
    public class SentimentActor : ReceiveActor
    {
        private readonly SentimentService _sentimentService;

        public SentimentActor(SentimentService sentimentService)
        {
            _sentimentService = sentimentService;

            Receive<AnalyzeSentimentRequest>(req =>
            {
                var prediction = _sentimentService.Predict(req.Text);
                Sender.Tell(new AnalyzeSentimentResponse(
                    req.BookId,
                    req.Title,
                    req.Text,
                    prediction.Prediction,
                    prediction.Score,
                    prediction.Probability
                ));
            });
        }

        public static Props Props(SentimentService sentimentService) =>
            Akka.Actor.Props.Create(() => new SentimentActor(sentimentService))
                .WithDispatcher("gutenberg-dispatcher");
    }
}
