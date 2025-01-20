using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PuppeteerSharp;
using HtmlAgilityPack;

namespace HolostreamApp.Services
{
    public class ScheduleScraper
    {
        public async Task<List<StreamItem>> ScrapeStreamsAsync(string url)
        {
            List<StreamItem> streamItems = new();
            try
            {
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();

                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
                using var page = await browser.NewPageAsync();

                Console.WriteLine($"Navigating to {url}...");
                await page.GoToAsync(url);

                await page.WaitForSelectorAsync("#today li");
                Console.WriteLine("Content loaded.");

                string htmlContent = await page.GetContentAsync();

                HtmlDocument doc = new();
                doc.LoadHtml(htmlContent);

                var todayNode = doc.DocumentNode.SelectSingleNode("//ul[@id='today']");
                if (todayNode == null) return streamItems;

                var liNodes = todayNode.SelectNodes("./li");
                if (liNodes == null) return streamItems;

                foreach (var liNode in liNodes)
                {
                    string link = liNode.SelectSingleNode(".//a")?.GetAttributeValue("href", "N/A") ?? "N/A";
                    string start = liNode.SelectSingleNode(".//p[contains(@class, 'start')]")?.InnerText.Trim() ?? "N/A";
                    string name = liNode.SelectSingleNode(".//p[contains(@class, 'name')]")?.InnerText.Trim() ?? "N/A";
                    string text = liNode.SelectSingleNode(".//p[contains(@class, 'txt')]")?.InnerText.Trim() ?? "N/A";

                    var liveNode = liNode.SelectSingleNode(".//p[contains(@class, 'cat') and contains(@class, 'now_on_air')]");
                    string liveStatus = liveNode != null ? "Live" : "Not Live";

                    streamItems.Add(new StreamItem
                    {
                        Link = link,
                        Start = start,
                        Name = name,
                        Text = text,
                        LiveStatus = liveStatus
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return streamItems;
        }
    }

    public class StreamItem
    {
        public string Link { get; set; }
        public string Start { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public string LiveStatus { get; set; }
    }
}
