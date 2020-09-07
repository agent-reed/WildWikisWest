using System.Text.RegularExpressions;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace wildwikis.automation
{
    public class WikipediaClient
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public WikipediaClient(ILogger logger)
        {
            _logger = logger;
            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://en.wikipedia.org");
        }

        public async Task<String> FetchArticleText(String title)
        {
            var response = await _client.GetStringAsync($"/w/api.php?action=query&format=json&prop=extracts&titles={title}&exlimit=1&exintro=1");
            JObject data = (JObject) JsonConvert.DeserializeObject(response);
            JToken extract = data["query"]["pages"].First?.First?["extract"];
            String stripped = Regex.Replace(extract.ToString(), "<.*?>", String.Empty);
            char[] charsToTrim = { ' ', '\t', '\n' };
            String trimmed = stripped.Trim(charsToTrim);
            String shortened = trimmed.Substring(0, trimmed.Length > 400 ? 400 : trimmed.Length);
            _logger.LogInformation($"\nTitle: {title} \nResponse: {shortened}\n");
            return response;
        }

        public Task<String> FetchArticleImageUrl(String title)
        {
            return _client.GetStringAsync("/");
        }
    }
}