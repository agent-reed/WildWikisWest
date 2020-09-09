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

        public async Task<WikipediaArticle> FetchArticle(SubmissionRequestDto submissionRequest)
        {
            WikipediaArticle wiki = new WikipediaArticle();

            // Extract the title to use from the link
            var uri = new Uri(submissionRequest.Link);
            var segments = uri.Segments;
            var title = segments[2];

            var detailsResponse = await _client.GetStringAsync($"/w/api.php?action=query&format=json&formatversion=2&prop=pageimages|extracts|pageterms&piprop=original&titles={title}&pilicense=any&exlimit=1&exintro=1");
            JObject data = (JObject) JsonConvert.DeserializeObject(detailsResponse);

            // Fetch the contents from wikimedia API
            Task<string> wikiDescription = ParseArticleDescription(data);
            Task<string> wikiImageUrl = ParseArticleImage(data);
            Task<string> wikiLinkText = ParseArticleLinkText(data);

            // Build the article object
            wiki.Title = title.Replace('_', ' ');
            wiki.Description = await wikiDescription;
            wiki.ImageUrl = await wikiImageUrl;
            var linkText = await wikiLinkText;
            wiki.LinkText = string.IsNullOrEmpty(linkText) ? $"A wild wiki submitted by {submissionRequest.Username}" : linkText;
            wiki.Author = submissionRequest.Username;
            wiki.Link = submissionRequest.Link;
            wiki.Comments = submissionRequest.Comments;
            return wiki;
        }

        private async Task<string> ParseArticleDescription(JObject data) 
        {
            var parseContentsTask = await Task.Run(() => {
                JToken page = data["query"]["pages"].First;
                JToken extract = page["extract"];
                string stripped = Regex.Replace(extract.ToString(), "<.*?>", String.Empty);
                char[] charsToTrim = { ' ', '\t', '\n' };
                string trimmed = stripped.Trim(charsToTrim);
                string shortened = trimmed.Substring(0, trimmed.Length > 400 ? 400 : trimmed.Length);

                return shortened;
            });
            return parseContentsTask;
        }

        private async Task<string> ParseArticleImage(JObject data) {
            var parseImageTask = await Task.Run(() => {
                JToken page = data["query"]["pages"].First;
                JToken imageSource = page["original"]["source"];
                return imageSource.ToString();
            });
            return parseImageTask;
        }

        private async Task<string> ParseArticleLinkText(JObject data) {
            var parseDescriptionTask = await Task.Run(() => {
                JToken page = data["query"]["pages"].First;
                JToken description = page["terms"]["description"].First;
                return description.ToString();
            });
            return parseDescriptionTask;
        }
    }
}