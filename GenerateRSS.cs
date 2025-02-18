using System.Globalization;
using System.Text;

// variables
const string url = "https://www.limesvolleybal.nl/";
const string output = "rss.xml";

// attempt to create RSS feed from pre-existing website. 
try
{
    // fetch source from website. 
    using HttpClient client = new();
    string content = await client.GetStringAsync(url);

    // extract articles from source.
    string articles = ExtractArticles(content);

    // generate RSS feed from articles.
    string rss = GenerateRSS(articles, url);

    // save RSS feed to local directory.
    string path = Path.Combine(Directory.GetCurrentDirectory(), output);
    File.WriteAllText(path, rss, Encoding.UTF8);

    // confirm feed has been generated successfully.
    Console.WriteLine($"RSS feed has been generated successfully at {path}.");
}
catch (Exception ex)
{
    // failure to either fetch the source, or generate valid RSS from it.
    Console.WriteLine($"Could not generate RSS feed: {ex.Message}");
}

// Generate RSS format.
static string GenerateRSS(string content, string url) => 
$@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
    <rss version=""2.0"">
        <channel>
            <title>Limes Volleybal RSS Feed</title>
            <link>{url}</link>
            <description></description>
            {content}
        </channel>
    </rss>";

// Parse dutch date format to RSS date format.
static string ParseDate(string input, CultureInfo culture)
{
    return DateTime.Parse(input, culture).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture);
}

// Extract all individual articles from website.
static string ExtractArticles(string content)
{
    // variables
    StringBuilder result = new();
    CultureInfo culture = new("nl-NL");

    // strings that denote assumed start and ending of seareched for information.
    const string articleStart       = "<article class=";
    const string articleEnd         = "</article>";
    const string headerStart        = "-heading";
    const string headerEnd          = "</h2>";
    const string linkStart          = "href=\"";
    const string linkEnd            = "\"";
    const string titleStart         = ">";
    const string titleEnd           = "</a>";
    const string descriptionStart   = "<p>";
    const string descriptionEnd     = "</p>";
    const string imageStart         = "background-image: url(";
    const string imageEnd           = ");";
    const string dateStart          = "jw-news-date\">";
    const string dateEnd            = "<";

    // initial
    int articleIndex = 0;

    // traverses over all occurrences of 'start'.
    while ((articleIndex = content.IndexOf(articleStart, articleIndex)) != -1)
    {
        // extract single article from content.
        var article = Extract(content, articleStart, articleEnd, articleIndex);
        if (article.Start == -1 || article.End == -1) break;

        // hard dependencies (skip article if failed)
        var header = Extract(article.String, headerStart, headerEnd);
        if (header.Start == -1 || header.End == -1) break;
        var link = Extract(header.String, linkStart, linkEnd);
        if (link.Start == -1 || link.End == -1) break;
        var title = Extract(header.String, titleStart, titleEnd, link.End);
        if (title.Start == -1 || title.End == -1) break;

        // optional (leave empty if failed)
        var description = Extract(article.String, descriptionStart, descriptionEnd, header.Start);
        var image = Extract(article.String, imageStart, imageEnd);
        var date = Extract(article.String, dateStart, dateEnd);

        // append article to result.
        result.AppendLine($@"
            <item>
                <title>{title.String}</title>
                <link>{url + link.String}</link>
                <description>{description.String} <![CDATA[<img src=""https://www.example.com/images/sample-image.jpg"" alt=""Sample Image"">]]></description>
                <pubDate>{ParseDate(date.String, culture)}</pubDate>
            </item>");

        // move to next
        articleIndex = article.End;
    }

    return result.ToString();
}

// function to extract between two unique
static (string String, int Start, int End) Extract(string source, string start, string end, int index = 0)
{
    // find end index of the start string.
    int startIndex = source.IndexOf(start, index);
    if (startIndex == -1) return (string.Empty, startIndex + start.Length, -1);
    startIndex += start.Length;

    // find start index of the end string.
    int endIndex = source.IndexOf(end, startIndex);
    if (endIndex == -1) return (string.Empty, startIndex, endIndex);

    // trim white space.
    return (source[startIndex..endIndex].Trim(), startIndex, endIndex);
}



