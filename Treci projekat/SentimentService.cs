using Microsoft.ML;
using Microsoft.ML.Data;

namespace Treci_projekat
{
    public class SentimentData
    {
        [ColumnName("Label")]
        public bool Sentiment { get; set; }

        [ColumnName("SentimentText")]
        public string SentimentText { get; set; } = string.Empty;
    }

    public class SentimentPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }

        public float Probability { get; set; }

        public float Score { get; set; }
    }

    public class SentimentService
    {
        private readonly MLContext _mlContext;
        private readonly ITransformer _model;
        private readonly PredictionEngine<SentimentData, SentimentPrediction> _predictionEngine;
        private readonly object _predictionLock = new object();

        public SentimentService()
        {
            _mlContext = new MLContext(seed: 1);

            var trainingData = new[]
            {
                new SentimentData { SentimentText = "A masterpiece of literature, absolutely brilliant and uplifting.", Sentiment = true },
                new SentimentData { SentimentText = "Wonderful writing style, highly recommended and engaging.", Sentiment = true },
                new SentimentData { SentimentText = "This is a great and wonderful book, full of beautiful moments.", Sentiment = true },
                new SentimentData { SentimentText = "An excellent read, very positive and pleasant story.", Sentiment = true },
                new SentimentData { SentimentText = "I loved reading this, it was fantastic and sweet.", Sentiment = true },
                new SentimentData { SentimentText = "Beautiful descriptions, very moving, emotional and inspiring.", Sentiment = true },
                new SentimentData { SentimentText = "Brilliant poetry, highly sophisticated and emotionally satisfying.", Sentiment = true },
                new SentimentData { SentimentText = "A delightful comedy, lighthearted, clever, and very funny.", Sentiment = true },
                new SentimentData { SentimentText = "Highly recommend this wonderful and positive tale of love.", Sentiment = true },
                new SentimentData { SentimentText = "Outstanding collection of classic works, highly satisfying.", Sentiment = true },
                new SentimentData { SentimentText = "A peaceful and pleasant story about redemption and hope.", Sentiment = true },
                new SentimentData { SentimentText = "A beautiful classic romance that is sweet and lovely.", Sentiment = true },
                new SentimentData { SentimentText = "Superb storytelling, great characters, very positive.", Sentiment = true },
                new SentimentData { SentimentText = "A bright, joyful, and wonderful adventure for everyone.", Sentiment = true },
                new SentimentData { SentimentText = "Captivating plot, delightful writing, truly excellent.", Sentiment = true },

                new SentimentData { SentimentText = "Very poor writing, boring and a complete waste of time.", Sentiment = false },
                new SentimentData { SentimentText = "Terrible novel, awful characters, extremely dull and dark.", Sentiment = false },
                new SentimentData { SentimentText = "I hated this book, it was slow, painful, and badly written.", Sentiment = false },
                new SentimentData { SentimentText = "Disappointing ending, frustrating plot, not recommended.", Sentiment = false },
                new SentimentData { SentimentText = "Horrible, worst book I have ever read, avoid it at all costs.", Sentiment = false },
                new SentimentData { SentimentText = "A tragic story, full of despair, pain, and death.", Sentiment = false },
                new SentimentData { SentimentText = "Depressing and dark, filled with sorrow and suffering.", Sentiment = false },
                new SentimentData { SentimentText = "Awful poetry, poorly structured, boring and tedious.", Sentiment = false },
                new SentimentData { SentimentText = "Dull dialogue, slow-paced, frustrating and waste of energy.", Sentiment = false },
                new SentimentData { SentimentText = "A dark and violent tragedy, painful to read, depressing.", Sentiment = false },
                new SentimentData { SentimentText = "Boring descriptions, lack of plot, disappointing book.", Sentiment = false },
                new SentimentData { SentimentText = "Stupid plot, terrible characters, completely unsatisfying.", Sentiment = false },
                new SentimentData { SentimentText = "Sad and dark tale, depressing and full of regret.", Sentiment = false },
                new SentimentData { SentimentText = "Frustrating experience, bad writing, horrible execution.", Sentiment = false },
                new SentimentData { SentimentText = "Boring, dry, uninteresting, and deeply disappointing.", Sentiment = false }
            };

            var trainDataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            var estimator = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(SentimentData.SentimentText))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));

            _model = estimator.Fit(trainDataView);
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(_model);
        }

        public SentimentPrediction Predict(string text)
        {
            lock (_predictionLock)
            {
                return _predictionEngine.Predict(new SentimentData { SentimentText = text });
            }
        }
    }
}
