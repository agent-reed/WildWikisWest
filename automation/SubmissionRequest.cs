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
        private GithubClient _githubClient;

        [FunctionName("SubmissionRequest")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            _wikipediaClient = new WikipediaClient(log);
            _githubClient = new GithubClient(log);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            SubmissionRequestDto submissionRequest = JsonConvert.DeserializeObject<SubmissionRequestDto>(requestBody);

            WikipediaArticle wikipediaArticle = await _wikipediaClient.FetchArticle(submissionRequest);
            string pullRequest = await _githubClient.OpenNewPullRequest();

            return new JsonResult(pullRequest);
        }
    }
}
