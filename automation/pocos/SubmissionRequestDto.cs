using System;
namespace wildwikis.automation 
{
    public class SubmissionRequestDto {
        public string Link { get; set; }

        public string Comments { get; set; }

        private string _username;
        public string Username { 
            get { return string.IsNullOrEmpty(_username) ? "an anonymous reader" : _username; }
            set { _username = value; }
        }
    }
}