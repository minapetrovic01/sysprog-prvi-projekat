using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekat1
{
    class Cache
    {
        private readonly Dictionary<string, LinkedListNode<string>> _cache;
        private readonly LinkedList<string> _currentList;
        private readonly ReaderWriterLockSlim _lock;
        private readonly int _maxSize;

        public Cache(int maxSize = 32)
        {
            _cache = new Dictionary<string, LinkedListNode<string>>();
            _currentList = new LinkedList<string>();
            _lock = new ReaderWriterLockSlim();
            _maxSize = maxSize;
        }

        public void Add(string key)
        {
            _lock.EnterWriteLock();
            try
            {
                if(_cache.ContainsKey(key))
                {
                    _currentList.Remove(_cache[key]);
                }
                else
                {
                    if(_cache.Count>=_maxSize)
                    {
                        string toRemove = _currentList.Last.Value;
                        _currentList.RemoveLast();
                        _cache.Remove(toRemove);
                    }
                }
                LinkedListNode<string> newNode = _currentList.AddFirst(key);
                _cache[key] = newNode;
            }
            catch (Exception e)
            {
                //ne radimo nista kada ne uspe da ga doda u cache, najbrze je da se ide dalje
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Contains(string key)
        {
            bool toReturn = false;
            _lock.EnterUpgradeableReadLock();
            try
            {
                if(_cache.TryGetValue(key,out LinkedListNode<string> node))
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _currentList.Remove(node);
                        _currentList.AddFirst(node);
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                    toReturn= true;
                }
               
            }
            catch 
            {
                toReturn= false; 
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
            return toReturn;
        }
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _cache.Clear();
                _currentList.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
