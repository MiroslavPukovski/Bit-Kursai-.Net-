﻿using ManoPirmasDotNetProjektas.Paskaitos.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack.CssSelectors.NetCore;
using HtmlAgilityPack;
using PuppeteerSharp;

namespace ManoPirmasDotNetProjektas.Paskaitos.Networking
{
    public class NetworkExecutor : ITema
    {
        private readonly ILoggerServise _logger;

        private string _mindaugasBlogUrl = "https://blog.mindaugas.cf";
        private string _dadJokeUrl = "https://icanhazdadjoke.com/";

        private string _topoCentrasProcesorsBaseUrl = @"https://www.topocentras.lt/kompiuteriai-ir-plansetes/kompiuteriu-komponentai/procesoriai.html";
        private string _topoCentrasLimit = @"?limit=80";
        private string _topoCentrasPage = @"&p=";

        private string _LrytasBaseUrl = @"https://www.lrytas.lt/naujausi";
        private string _LrytasPaginationParameter = @"?page=";

        public NetworkExecutor(ILoggerServise logger)
        {
            _logger = logger;
        }

        public async Task Run()
        {
            //await GetWebHeader();
            //await GetDadJoke();
            //await Test10Jokes();
            //await ScrapeSomething();
            //await ScarpeLRytasHeadlines();
            await ScrapeTopoProcesors();
        }

        private async Task GetWebHeader()
        {
            using var client = new HttpClient();

            try
            {
                var result = await client.GetAsync(_mindaugasBlogUrl);

                result.EnsureSuccessStatusCode();
                Console.WriteLine(result.Headers);
                Console.WriteLine(await result.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                await _logger.LogWarning($"Cannot reach {_mindaugasBlogUrl}");
                await _logger.LogWarning($"error: {ex.Message} \nTrace: {ex.StackTrace}");

            }
        }

        private async Task<DadJoke> GetDadJoke(string id = "")
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            try
            {
                var result = await client.GetAsync(string.IsNullOrEmpty(id)
                    ? _dadJokeUrl
                    : $"{_dadJokeUrl}j/{id}");

                result.EnsureSuccessStatusCode();

                return JsonConvert.DeserializeObject<DadJoke>(await result.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                await _logger.LogWarning($"Cannot reach {_dadJokeUrl}");
                await _logger.LogWarning($"error: {ex.Message} \nTrace: {ex.StackTrace}");

                return null;

            }
        }

        private async Task Test10Jokes()
        {
            var likedJokesIds = new List<string>();

            for (int i = 0; i < 10; i++)
            {
                var joke = await GetDadJoke();

                var userAwnser = string.Empty;
                do
                {
                    Console.WriteLine($"Joke: {joke?.Joke ?? "Server error ;("}");
                    Console.WriteLine("\nDid you like this joke ? (y-yes/n-no)");
                    userAwnser = Console.ReadLine();
                }
                while (userAwnser != "n" && userAwnser != "y");

                if (userAwnser == "y")
                {
                    Console.WriteLine("\nGreat!");
                    likedJokesIds.Add(joke.Id);
                }
                else
                {
                    Console.WriteLine("\nSorry");
                }
            }

            foreach (var jokeId in likedJokesIds)
            {
                File.AppendAllText(@"./Juokingi.txt", $"{(await GetDadJoke(jokeId)).Joke}\n");
            }
        }

        private async Task ScrapeSomething()
        {
            var selector = "div > header > h2 > a:nth-child(1)";
            var res = await Scrape("https://www.lrytas.lt/", selector);

            Console.WriteLine(res);
        }

        private async Task<string> Scrape(string url, string cssSelector)
        {
            using var client = new HttpClient();

            var result = await client.GetAsync(url);
            var body = await result.Content.ReadAsStringAsync();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(body);

            var elements = htmlDocument.QuerySelectorAll(cssSelector);

            StringBuilder sb = new();

            foreach (var element in elements)
            {
                sb.Append(element.InnerText.Trim() + "\n");
            }

            return sb.ToString();
        }

       

        private async Task<string> ScrapePage(string url)
        {
            using var client = new HttpClient();

            var result = await client.GetAsync(url);

            return await result.Content.ReadAsStringAsync();
        }

        private async Task ScarpeLRytasHeadlines()
        {
            var page = 1;
            var headlines = new List<string>();

            while(headlines.Count() < 200)
            {
                var pageContent = await ScrapePage($"{_LrytasBaseUrl}{_LrytasPaginationParameter}{page}");
                page++;

                headlines.AddRange(getHeadlines(pageContent));
            }

            var counter = 1;
            foreach(var headline in headlines)
            {
                if (counter == 1)
                {
                    await File.WriteAllTextAsync("Lrytas_Straipsniu_antrastes.txt", $"{counter}: {headline}");
                }
                else
                {
                    await File.AppendAllTextAsync("Lrytas_Straipsniu_antrastes.txt", $"{counter}: {headline}");
                }

                counter++;
            }
            
        }

        private IEnumerable<string> getHeadlines(string htmlContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(htmlContent);

            var elements = htmlDocument.QuerySelectorAll("h2 .LPostContent__anchor");

            var headlines = new List<string>();

            foreach (var element in elements)
            {
                headlines.Add(element.InnerText);
            }

            return headlines;
        }

        private async Task ScrapeTopoProcesors()
        {
            var processors = new List<ProcessorListing>();
            var pageCounter = 1;

            var basePage = await ScrapeJavaScriptPage($"{_topoCentrasProcesorsBaseUrl}{_topoCentrasLimit}{_topoCentrasPage}{pageCounter}");

            var pageLimit = GetPageLimit(basePage);

            processors.AddRange(GetProcessorsFromPage(basePage));

            for (pageCounter = 2; pageCounter <= pageLimit; pageCounter++)
            {
                var nextPage = await ScrapeJavaScriptPage($"{_topoCentrasProcesorsBaseUrl}{_topoCentrasLimit}{_topoCentrasPage}{pageCounter}");

                processors.AddRange(GetProcessorsFromPage(nextPage));
            }

            File.WriteAllText("TopoCentroProcesoriai.json", JsonConvert.SerializeObject(processors));
        }

        private int GetPageLimit(string page)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(page);

            var elements = htmlDocument.QuerySelectorAll(".Count-pageCount-LOv > span");

            if (int.TryParse(elements[2].InnerHtml, out var pageLimit))
            {
                var pageCounter = 1;
                pageLimit -= 80;

                while(pageLimit > 0)
                {
                    pageLimit -= 80;
                    pageCounter++;
                }

                return pageCounter;
            }

            return 1;           
        }

       

        private IEnumerable<ProcessorListing> GetProcessorsFromPage(string page)
        {
            var processors = new List<ProcessorListing>();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(page);

            var elements = htmlDocument.QuerySelectorAll("article .ProductGridItem-productWrapper-2ip");

            foreach (var element in elements)
            {
                var price = element.QuerySelector(".Price-price-27p").InnerText;
                var name = element.QuerySelector(".ProductGridItem-productName-3ZD").InnerText;
                var pictureUrl = element.QuerySelector(".ProductGridItem-imageContainer-pMi").FirstChild.GetAttributeValue("src", string.Empty);

                var processorListing = new ProcessorListing(name, pictureUrl, price);

                processors.Add(processorListing);
            }

            return processors;
        }

        private async Task<string> ScrapeJavaScriptPage(string url)
        {
            await new BrowserFetcher(Product.Chrome).DownloadAsync();
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Product = Product.Chrome
            });

            using var page = await browser.NewPageAsync();
            await page.GoToAsync(url);

            return await page.GetContentAsync();
        }
    }
}