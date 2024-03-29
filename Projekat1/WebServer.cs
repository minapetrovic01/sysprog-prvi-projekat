﻿using System;
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
        private readonly Dictionary<string, object> _fileLocks = new Dictionary<string, object>();
        private object _lockLock = new object();
        public WebServer(string url, string rootPath, string cachedPath, int port = 80, int cacheSize = 32)
        {
            _url = url;
            _port = port;
            _cache = new Cache(cacheSize);
            _rootDirPath = rootPath;
            _cachedFilesPath = cachedPath;
        }

        public void Run()
        {
            using (HttpListener listener = new HttpListener())
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
                        string toLog = "";
                        try
                        {

                            HttpListenerContext httpListenerContext = (HttpListenerContext)listenerContext;
                            if (httpListenerContext == null)
                                throw new Exception("Can't parse given object to HttpListenerContext object!");
                            string fileName = Path.GetFileName(httpListenerContext.Request.Url.LocalPath);
                            string fileExtension = Path.GetExtension(fileName).TrimStart('.');
                            // Console.WriteLine(fileName+" "+fileExtension);
                            toLog += ($"\nNEW REQUEST\nRequested filename is:{fileName}\n");

                            string validation = this.ValidateRequest(httpListenerContext, fileName, fileExtension);

                            toLog += "VALID=" + validation + "\n";

                            if (validation != "OK")
                            {
                                this.SendResponse(httpListenerContext, validation, false);
                                return;
                            }

                            if (_cache.Contains(fileName))
                            {
                                toLog += "FILE WAS ALREADY TRANSLATED AND IN CACHE\n";
                                this.SendResponse(httpListenerContext, $"FILE{fileName} is translated", true);
                                return;
                            }

                            string[] files = Directory.GetFiles(_rootDirPath, fileName);
                            if (files.Length == 0)
                            {
                                toLog += "FILE NOT FOUND IN DIRECTORY";
                                this.SendResponse(httpListenerContext, "NO SUCH FILE IN ROOT DIR", false);
                                return;
                            }

                            string toTranslate = files[0];
                            string newFileName = Path.GetFileNameWithoutExtension(fileName);

                            

                            byte[] fileBytes;
                            using (FileStream fs = new FileStream(toTranslate, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                fileBytes = new byte[fs.Length];
                                int bytesRead = fs.Read(fileBytes, 0, fileBytes.Length);
                                if (bytesRead != fileBytes.Length)
                                {
                                    throw new IOException("Could not read the whole file");
                                }
                            }
                            if (fileBytes.Length <= 0)
                            {
                                toLog += "FILE IS EMPTY";
                                this.SendResponse(httpListenerContext, "FILE IS EMPTY", false);
                                return;
                            }


                            if (fileExtension == "txt")
                            {
                                newFileName += ".bin";
                                string filePath = _cachedFilesPath + "/" + newFileName;
                                object fileLock;

                                lock (_lockLock)
                                {
                                    if (!_fileLocks.TryGetValue(filePath, out fileLock))
                                    {
                                        fileLock = new object();
                                        _fileLocks.Add(filePath, fileLock);
                                    }
                                }

                                lock (fileLock)
                                {
                                    File.WriteAllBytes(filePath, fileBytes);
                                }
                                _cache.Add(fileName);
                                toLog += "TRANSLATED TO .bin";
                                this.SendResponse(httpListenerContext, "TRANSLATED TO .bin", true);
                                return;
                            }
                            else
                            {
                                newFileName += ".txt";
                                string filePath = _cachedFilesPath + "/" + newFileName;
                                object fileLock;
                                string text = Encoding.UTF8.GetString(fileBytes);

                                lock (_lockLock)
                                {
                                    if (!_fileLocks.TryGetValue(filePath, out fileLock))
                                    {
                                        fileLock = new object();
                                        _fileLocks.Add(filePath, fileLock);
                                    }
                                }

                                lock (fileLock)
                                {
                                    File.WriteAllText(filePath, text);
                                }
                                _cache.Add(fileName);
                                toLog += "TRANSLATED TO .txt";
                                this.SendResponse(httpListenerContext, "TRANSLATED TO .txt", true);
                                return;

                            }
                        }
                        catch (Exception e)
                        {
                            lock (_lock)
                            {
                                Console.WriteLine(e.ToString());
                                using (StreamWriter sw = File.AppendText("../../../logFile.txt"))
                                {
                                    sw.WriteLine(toLog+'\n');
                                    sw.WriteLine("-------------------------");
                                }
                                this.WriteBreakLine();
                            }
                        }
                        finally
                        {
                            lock (_lock)
                            {
                                if(toLog!="")
                                {
                                    Console.WriteLine($"\n{toLog}\n");
                                    using (StreamWriter sw = File.AppendText("../../../logFile.txt"))
                                    {
                                        sw.WriteLine(toLog + '\n');
                                        sw.WriteLine("-------------------------");
                                    }
                                }
                                
                                this.WriteBreakLine();
                            }
                        }
                    }, context);
                }
            }
        }

        private void SendResponse(HttpListenerContext context, string content, bool isOK)
        {
            HttpListenerResponse response = context.Response;
            string responseString = $"<html><body><h1>{content}</h1></body></html>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            try
            {
                response.StatusCode = isOK ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
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
        private string ValidateRequest(HttpListenerContext context, string fileName, string fileExtension)
        {
            if (fileName == string.Empty || fileExtension == string.Empty)
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
