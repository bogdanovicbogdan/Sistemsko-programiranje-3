using System;
using System.Collections.Generic;

namespace Treci_projekat
{
    public record GetAuthorData(string AuthorName);

    public record AuthorDataResponse(string AuthorName, List<BookSentimentDetail> Books, bool IsReady, bool IsNotFound);

    public record StartPeriodicFetch(string AuthorName, TimeSpan Interval);

    public record BookBatch(string AuthorName, List<Book> Books);

    public record AnalyzeSentimentRequest(long BookId, string Title, string Text);

    public record AnalyzeSentimentResponse(long BookId, string Title, string Text, bool Sentiment, float Score, float Probability);
}
