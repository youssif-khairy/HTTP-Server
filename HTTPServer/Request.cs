using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTTPServer
{
    public enum RequestMethod
    {
        GET,
        POST,
        HEAD
    }

    public enum HTTPVersion
    {
        HTTP10,
        HTTP11,
        HTTP09
    }

    class Request
    {
        string[] requestLines;
        public RequestMethod method;
        public string relativeURI;
        Dictionary<string, string> headerLines;

        public Dictionary<string, string> HeaderLines
        {
            get { return headerLines; }
        }

        HTTPVersion httpVersion;
        string requestString;
        string[] contentLines;//we are working get 

        public Request(string requestString)
        {
            this.requestString = requestString;
        }
        /// <summary>
        /// Parses the request string and loads the request line, header lines and content, returns false if there is a parsing error
        /// </summary>
        /// <returns>True if parsing succeeds, false otherwise.</returns>
        public bool ParseRequest()
        {

            //TODO: parse the receivedRequest using the \r\n delimeter  
             string[] stringSeparators = new string[] { "\r\n" };
             requestLines = requestString.Split(stringSeparators, StringSplitOptions.None);

            // check that there is atleast 3 lines: Request line, Host Header, Blank line (usually 4 lines with the last empty line for empty content)
             if (requestLines.Length < 3) return false;
            // Parse Request line
            
            if (!ParseRequestLine()) return false;

            // Validate blank line exists
             if (!ValidateBlankLine()) return false;
            // Load header lines into HeaderLines dictionary
             if (!LoadHeaderLines()) return false;

                 return true;
        }

        private bool ParseRequestLine()
        {
            string[] req_line = requestLines[0].Split(' ');
            if (req_line[0] == "GET") method = RequestMethod.GET;
            else if (req_line[0] == "POST") method = RequestMethod.POST;
            else if (req_line[0] == "HEAD") method = RequestMethod.HEAD;
            else return false;

            if (!ValidateIsURI(req_line[1])) 
                return false;
            relativeURI = req_line[1];
            string[] http_version = req_line[2].Split('/');
                
            if (http_version[1] == "1.0")
                httpVersion = HTTPVersion.HTTP10;
            else if (http_version[1] == "1.1")
                httpVersion = HTTPVersion.HTTP11;
            else
                httpVersion = HTTPVersion.HTTP09;

            return true;
        }

        private bool ValidateIsURI(string uri)
        {
            return Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute);
        }

        private bool LoadHeaderLines()
        {
            if (requestLines[2] == "") 
                return false; // if there is no headerlines return false (given that server is working with 1.1 from configuration class that needs at least host header)
            string[] stringSeparators = new string[] { ": " };
            headerLines = new Dictionary<string, string>();
            for (int i = 1; i < requestLines.Length - 2; i++)//descard request line and blank line (assuming GET  *NO CONTENT*) 
            {
                string[] kv = requestLines[i].Split(stringSeparators, StringSplitOptions.None);
                headerLines.Add(kv[0], kv[1]);
            }
            return true;
        }

        private bool ValidateBlankLine()
        {
            if (requestLines[requestLines.Length - 2] != "") 
                return false;
            return true;
        }

    }
}
