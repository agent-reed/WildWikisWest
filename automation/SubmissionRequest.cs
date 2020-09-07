using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace wildwikis.automation
{
    public class SubmissionRequest
    {
        private WikipediaClient _wikipediaClient;

        [FunctionName("SubmissionRequest")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a Submission Request.");
            _wikipediaClient = new WikipediaClient(log);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            var link = data?.link;
            var comments = data?.comments;
            log.LogInformation($"The link: {link}");

            // Extract the title to use from the link
            var uri = new Uri(link.ToString());
            var segments = uri.Segments;
            log.LogInformation($"The segments: {segments}");

            var title = segments[2];
            var wikipediaDescription = await _wikipediaClient.FetchArticleText(title);

            // Hit - https://en.wikipedia.org/w/api.php?action=query&titles={title}&format=json&prop=images and get a list back of all the images on the page
            // Take the first image that isn't an svg. otherwise take svgs.  If no images set the image to a placeholder

            // Make another request to the info API and suck off all the Title, description and paragraph text

            // build up the page using the extracted info

            // Use Oktokit to open a PR with the newly created file added to the _posts folder

            return new JsonResult(data);
        }
    }
}
