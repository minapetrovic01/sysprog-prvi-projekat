using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Projekat1
{
    class WebServer
    {
        private Cache _cache;
        private readonly string _url;
        private readonly int _port;
        private readonly string _rootDirPath;
        private object _lock = new object();//koristim ga za zakljucavanje logovanja u fajl i konzolu
        private readonly string _cachedFilesPath;

        public WebServer(string url,string rootPath,string cachedPath, int port=80, int cacheSize=32)
        {
            _url = url;
            _port = port;
            _cache = new Cache(cacheSize);
            _rootDirPath = rootPath;
            _cachedFilesPath = cachedPath;
        }

        public void Run()
        {
           using(HttpListener listener=new HttpListener())
            {
                string urlListener = _url + $":{_port}/";
                listener.Prefixes.Add(urlListener);
                listener.Start();

                Console.WriteLine("Server is listening on:" + urlListener);

                while (listener.IsListening)
                {
                    HttpListenerContext context = listener.GetContext();

                    ThreadPool.QueueUserWorkItem((object listenerContext) =>
                    {
                        try
                        {
                            HttpListenerContext httpListenerContext = (HttpListenerContext)listenerContext;
                            if (httpListenerContext == null)
                                throw new Exception("Can't parse given object to HttpListenerContext object!");
                            string fileName=Path.GetFileName(httpListenerContext.Request.Url.LocalPath);
                            string fileExtension = Path.GetExtension(fileName).TrimStart('.');
                            // Console.WriteLine(fileName+" "+fileExtension);

                            lock (_lock)
                            {
                                Console.WriteLine("NEW REQUEST");
                                Console.WriteLine($"Requested filename is:{fileName}");
                            }

                            string validation = this.ValidateRequest(httpListenerContext, fileName, fileExtension);

                            if (validation!="OK")
                            {
                                this.SendResponse(httpListenerContext, validation, false);
                                return;
                            }

                            if (_cache.Contains(fileName))
                            {
                                this.SendResponse(httpListenerContext, _cachedFilesPath + "/" + fileName, true);
                            }


                            
                        }
                        catch (Exception e)
                        {
                            lock(_lock)
                            {
                                Console.WriteLine(e.ToString());
                                this.WriteBreakLine();
                            }
                        }
                    }, context);
                }
            }
        }
        
        private void SendResponse(HttpListenerContext context,string content, bool isOK)
        {
            HttpListenerResponse response = context.Response;
            string responseString = $"<html><body><h1>{content}</h1></body></html>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            try
            {
                response.StatusCode=isOK? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                lock (_lock)
                {
                    Console.WriteLine(e.ToString());
                    this.WriteBreakLine();
                }
            }
            finally 
            {
                response.OutputStream.Close();
            }
        }
        private string ValidateRequest(HttpListenerContext context,string fileName,string fileExtension)
        {
            if (fileName == string.Empty||fileExtension==string.Empty)
                return "NO FILE NAME OR EXTENSION";
            if (!fileExtension.Equals("bin") && !fileExtension.Equals("txt"))
                return "WRONG EXTENSION";
            if (!context.Request.HttpMethod.Equals("GET"))
                return "METHOD IS NOT GET";
            return "OK";
        }
        private void WriteBreakLine()
        {
            Console.WriteLine("----------------------------------------------------");
        }
    }
}
