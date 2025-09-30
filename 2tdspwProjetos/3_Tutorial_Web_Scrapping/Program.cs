using HtmlAgilityPack;

//foreach (var i in Enumerable.Range(1, 7067))
// paralel foreach range 1 to 7067
Parallel.ForEach(Enumerable.Range(1, 7067), i =>
{
    Console.WriteLine(i);
    var web = new HtmlWeb();
    var doc = web.Load(
        $"https://www.metacritic.com/browse/game/all/all/all-time/new/?releaseYearMin=1958&releaseYearMax=2025&page={i}");
    var gameLinks = doc.DocumentNode.SelectNodes("//a[@class='c-finderProductCard_container g-color-gray80 u-grid']")
        .ToList()
        .Where(link => link.Attributes["href"] != null && link.Attributes["href"]
            .Value.Contains("/game")).ToList();
    //foreach (var link in gameLinks)
        //Console.WriteLine(link.Attributes["href"].Value);
});

static MetacriticGame GetMetacriticData(string url)
{
    Console.WriteLine("Web scrapping inicializando...");
    var web = new HtmlWeb();
    var doc = web.Load(
        url);
//Console.WriteLine(doc.DocumentNode.InnerHtml);

    var name = doc.DocumentNode.SelectSingleNode("//h1").InnerText;
    var metascore = doc.DocumentNode
        .SelectSingleNode(
            "/html/body/div[1]/div/div/div[2]/div[1]/div[1]/div/div/div[2]/div[3]/div[4]/div[1]/div/div[1]/div[2]/div/div/span")
        .InnerText;
    var userscore = doc.DocumentNode
        .SelectSingleNode(
            "/html/body/div[1]/div/div/div[2]/div[1]/div[1]/div/div/div[2]/div[3]/div[4]/div[2]/div[1]/div[2]/div/div/span")
        .InnerText;

    var metacriticGame = new MetacriticGame(name, metascore, userscore);
    Console.WriteLine(metacriticGame);
    return metacriticGame;
}

public record MetacriticGame(string Name, string MetaScore, string UserScore);




