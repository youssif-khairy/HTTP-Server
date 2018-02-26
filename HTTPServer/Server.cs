using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace HTTPServer
{
    class Server
    {
        Socket serverSocket;

        public Server(int portNumber, string redirectionMatrixPath)
        {
            //TODO: call this.LoadRedirectionRules passing redirectionMatrixPath to it
            this.LoadRedirectionRules(redirectionMatrixPath);
            //TODO: initialize this.serverSocket
            this.serverSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            IPEndPoint hostEndPoint = new IPEndPoint(IPAddress.Any , portNumber);
            this.serverSocket.Bind(hostEndPoint);
        }

        public void StartServer()
        {
            // TODO: Listen to connections, with large backlog.
            this.serverSocket.Listen(100);
            // TODO: Accept connections in while loop and start a thread for each connection on function "Handle Connection"
            while (true)
            {
                //TODO: accept connections and start thread for each accepted connection.
                Socket clientSocket = this.serverSocket.Accept();
                Console.Write("New client accepted: "); Console.Write(clientSocket.RemoteEndPoint); Console.WriteLine();

                Thread newthread = new Thread(new ParameterizedThreadStart(HandleConnection));
                //Start the thread
                newthread.Start(clientSocket);

            }
        }

        public void HandleConnection(object obj)
        {
            // TODO: Create client socket 
            Socket clientSock = (Socket)obj;
            // set client socket ReceiveTimeout = 0 to indicate an infinite time-out period
            clientSock.ReceiveTimeout = 0;
            // TODO: receive requests in while true until remote client closes the socket.
            int receivedLength;
            byte[] data;
            while (true)
            {
                try
                {
                    // TODO: Receive request
                    data = new byte[1024];
                    receivedLength = clientSock.Receive(data);
                                                                                        //Console.WriteLine(Encoding.Default.GetString(data));
                    // TODO: break the while loop if receivedLen==0
                    if (receivedLength == 0)
                    {
                        Console.Write("Client: "); Console.Write(clientSock.RemoteEndPoint); Console.Write(" ended the connection"); Console.WriteLine();
                        break;
                    }
                    // TODO: Create a Request object using received request string
                    Request request = new Request(System.Text.Encoding.Default.GetString(data));
                    // TODO: Call HandleRequest Method that returns the response
                    Response response = HandleRequest(request);
                    // TODO: Send Response back to client
                    data = Encoding.ASCII.GetBytes(response.ResponseString);
                    //Console.WriteLine(response.ResponseString);
                    clientSock.Send(data);
                }
                catch (Exception ex)
                {
                    // TODO: log exception using Logger class
                    Logger.LogException(ex);
                }
            }

            // TODO: close client socket
            clientSock.Close();
        }

        Response HandleRequest(Request request)
        {
            string content;
            Response resp;
            try
            {
                //TODO: check for bad request 
                if (!request.ParseRequest())
                {
                    content = File.ReadAllText(Path.Combine(Configuration.RootPath,Configuration.BadRequestDefaultPageName));
                    if (request.method == RequestMethod.GET)
                    resp = new Response(StatusCode.BadRequest, "text/html", "",content, "");
                    else // for head
                        resp = new Response(StatusCode.BadRequest, "text/html", "", "", "");
                    return resp;
                }
                //TODO: map the relativeURI in request to get the physical path of the resource.
                String physicalpath = Configuration.RootPath + request.relativeURI;
                //Console.WriteLine("physical path "+physicalpath);
                //TODO: check for redirect
                string redirect1 = GetRedirectionPagePathIFExist((request.relativeURI).Substring(1, (request.relativeURI).Length-1));
                if (redirect1 != "")
                {
                    string x = Configuration.RootPath + request.relativeURI[0]+redirect1;//get firrst \ from aboutus 
                    if (!File.Exists(x)) //for any entered value that is not uri
                    {
                        content = File.ReadAllText(Path.Combine(Configuration.RootPath, Configuration.NotFoundDefaultPageName));
                        if (request.method == RequestMethod.GET)
                            resp = new Response(StatusCode.NotFound, "text/html", "",content, "");
                        else //for head
                            resp = new Response(StatusCode.NotFound, "text/html","","", "");
                        return resp;
                    }
                    content = File.ReadAllText(x);
                    string redirectpath = Path.Combine(Configuration.RootPath, redirect1);
                    if (request.method == RequestMethod.GET)
                        resp = new Response(StatusCode.Redirect, "text/html","", content, redirectpath);
                    else // for head
                        resp = new Response(StatusCode.Redirect, "text/html", Convert.ToString(File.GetLastWriteTime(Configuration.RootPath + redirect1)), "", redirectpath);
                    return resp;
                }
                
                //TODO: check file exists
                if (Configuration.RedirectionRules.ContainsKey((request.relativeURI).Substring(1, (request.relativeURI).Length - 1)))//if redirect1 == ""
                {
                    content = File.ReadAllText(Path.Combine(Configuration.RootPath, Configuration.NotFoundDefaultPageName));
                    if (request.method == RequestMethod.GET)
                        resp = new Response(StatusCode.NotFound, "text/html", "",content, "");
                    else // for head
                        resp = new Response(StatusCode.NotFound, "text/html", "","", "");
                    return resp;
                }
                //TODO: read the physical file
                string temp =  LoadDefaultPage(request.relativeURI);
                if (temp == "") {
                    content = File.ReadAllText(Path.Combine(Configuration.RootPath, Configuration.NotFoundDefaultPageName));
                    if (request.method == RequestMethod.GET)
                        resp = new Response(StatusCode.NotFound, "text/html", "",content, "");
                    else // for head
                        resp = new Response(StatusCode.NotFound, "text/html", "","", "");
                    return resp;
                }
                content = temp;
                // Create OK response
                if (request.method == RequestMethod.GET)
                    resp = new Response(StatusCode.OK, "text/html", "",content, "");
                else // for head
                    resp = new Response(StatusCode.OK, "text/html", Convert.ToString(File.GetLastWriteTime(Configuration.RootPath + request.relativeURI)), "", "");
                return resp;
            }
            catch (Exception ex)
            {
                // TODO: log exception using Logger class
                Logger.LogException(ex);
                // TODO: in case of exception, return Internal Server Error. 
                content = File.ReadAllText(Path.Combine(Configuration.RootPath,Configuration.InternalErrorDefaultPageName));
                if (request.method == RequestMethod.GET)
                    resp = new Response(StatusCode.InternalServerError, "text/html", "",content, "");
                else // for head
                    resp = new Response(StatusCode.InternalServerError, "text/html", "","", "");
                return resp;
            }
        }

        private string GetRedirectionPagePathIFExist(string relativePath)
        {
            // using Configuration.RedirectionRules return the redirected page path if exists else returns empty
            if (Configuration.RedirectionRules.ContainsKey(relativePath))
            {
                return Configuration.RedirectionRules[relativePath];
            }
            
            return string.Empty;
        }

        private string LoadDefaultPage(string defaultPageName)
        {
            //string filePath = Path.Combine(Configuration.RootPath, defaultPageName);
            String filePath = Configuration.RootPath + defaultPageName;
            // TODO: check if filepath not exist log exception using Logger class and return empty string
            if (!File.Exists(filePath))
            {
                Logger.LogException(new FileNotFoundException());
                return string.Empty;
            }
            // else read file and return its content
            return File.ReadAllText(filePath);
        }

        private void LoadRedirectionRules(string filePath)
        {
            try
            {
                // TODO: using the filepath paramter read the redirection rules from file 
                string[] filelines = File.ReadAllLines(filePath);
                // then fill Configuration.RedirectionRules dictionary 
                Configuration.RedirectionRules = new Dictionary<string, string>();
                foreach (string uri in filelines)
                {
                    if (uri != "" )
                    {
                        string[] kv = uri.Split(','); // split according to comma
                        if (!Configuration.RedirectionRules.ContainsKey(kv[0]))
                        Configuration.RedirectionRules.Add(kv[0], kv[1]);
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: log exception using Logger class
                Logger.LogException(ex);
                Environment.Exit(1);
            }
        }
    }
}
