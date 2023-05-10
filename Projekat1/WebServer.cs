using System;
using System.Collections.Generic;
using System.Linq;
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
        private object _lock = new object();//koristimo ga za zakljucavanje logovanja u fajl i konzolu


        public WebServer(string url,string rootPath, int port=80, int cacheSize=32)
        {
            _url = url;
            _port = port;
            _cache = new Cache(cacheSize);
            _rootDirPath = rootPath;
        }

        public void Run()
        {
            throw new NotImplementedException();
        }
    }
}
