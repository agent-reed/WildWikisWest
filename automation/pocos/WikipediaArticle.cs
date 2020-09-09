using System;
namespace wildwikis.automation 
{
    public class WikipediaArticle {
        public string Title { get; set; }

        public string Description { get; set; }

        public string Link { get; set; }

        public string LinkText { get; set; }

        public string ImageUrl { get; set; }

        public string Author { get; set; }

        public string Comments { get; set; }

        public string ToMarkdownPost() {
            return String.Join(
                Environment.NewLine,
                "---",
                "layout: wiki",
                $"title: {this.Title}",
                $"subtitle: {this.LinkText}",
                $"date: {DateTime.Now.ToString("yyyy-MM-dd")}",
                $"link: {this.Link}",
                $"linkText: {this.Description}",
                $"author: {this.Author}",
                $"image: {this.ImageUrl}",
                "---",
                $"{this.Comments}",
                "---"
            );
        }
    }
}