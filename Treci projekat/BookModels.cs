using System.Collections.Generic;
using Newtonsoft.Json;

namespace Treci_projekat
{
    public class Person
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("birth_year")]
        public int? BirthYear { get; set; }

        [JsonProperty("death_year")]
        public int? DeathYear { get; set; }
    }

    public class Book
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("authors")]
        public List<Person> Authors { get; set; } = new();

        [JsonProperty("summaries")]
        public List<string> Summaries { get; set; } = new();
    }

    public class GutendexResponse
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("next")]
        public string? Next { get; set; }

        [JsonProperty("previous")]
        public string? Previous { get; set; }

        [JsonProperty("results")]
        public List<Book> Results { get; set; } = new();
    }

    public class SummarySentiment
    {
        public string Text { get; set; } = string.Empty;
        public string Sentiment { get; set; } = string.Empty;
        public float Score { get; set; }
        public float Probability { get; set; }
    }

    public class BookSentimentDetail
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<SummarySentiment> Summaries { get; set; } = new();
    }
}
