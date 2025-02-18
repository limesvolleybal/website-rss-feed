using System.Globalization;
using System.Text;

// variables
const string url     = "https://www.limesvolleybal.nl/";
const string output  = "rss.xml";

// attempting to create xml from existing website. 
try
{
    // Step 1: Fetch html content from the website. 
    using HttpClient client = new();
    string content = await client.GetStringAsync(url);

    // Step 2: Extract articles from HTML
    string articles = ExtractArticles(content);

    // Step 3: Generate RSS XML
    string rssContent = GenerateRss(articles, url);

    // Step 4: Save to file
    string path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.FullName, output);
    File.WriteAllText(path, rssContent, Encoding.UTF8);
    Console.WriteLine("✅ RSS feed generated successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

static string ExtractArticles(string content)
{
    // concatenate
    StringBuilder result = new();
    CultureInfo culture = new("nl-NL");

    // find start of articles
    const string startArticle = "<article class=";
    const string endArticle = "</article>";
    const string startHeader = "-heading";
    const string endHeader = "</h2>";

    // initial
    int articleIndex = 0;

    // traverses over all occurrences of 'start'.
    while ((articleIndex = content.IndexOf(startArticle, articleIndex)) != -1)
    {
        // extract single article from content.
        var article = Extract(content, startArticle, endArticle, articleIndex);
        if (article.Start == -1 || article.End == -1) break;

        // hard dependencies (skip article if failed)
        var header = Extract(article.String, startHeader, endHeader);
        if (header.Start == -1 || header.End == -1) break;
        var link = Extract(header.String, "href=\"", "\"");
        if (link.Start == -1 || link.End == -1) break;
        var title = Extract(header.String, ">", "</a>", link.End);
        if (title.Start == -1 || title.End == -1) break;

        // optional (leave empty if failed)
        var description = Extract(article.String, "<p>", "</p>", header.Start);
        var image = Extract(article.String, "background-image: url(", ");");
        var date = Extract(article.String, "jw-news-date\">", "<");

        // append article to result.
        result.AppendLine($@"
            <item>
                <title>{title.String}</title>
                <link>{url + link.String}</link>
                <description>{description.String}</description>
                <pubDate>{ParseDate(date.String, culture)}</pubDate>
            </item>");

        // move to next
        articleIndex = article.End;
    }

    return result.ToString();
}

static string GenerateRss(string items, string url)
{
    return $@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
    <rss version=""2.0"">
        <channel>
            <title>Limes Volleybal RSS Feed</title>
            <link>{url}</link>
            <description></description>
            {items}
        </channel>
    </rss>";
}

static string ParseDate(string input, CultureInfo culture)
{
    return DateTime.Parse(input, culture).ToString("ddd, dd MMM yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture);
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



