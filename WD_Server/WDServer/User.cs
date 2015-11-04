using System;
using System.Net;
using WDServer;

namespace WindowsDefender_WebApp
{
    public class User
    {
        private object  _lock = new object();
        public EndPoint  EndPoint { get; set; }
        public string    Username { get; set; }
        private int     _timeout = 0;

        public User(EndPoint endpoint)
        {
            EndPoint = endpoint;
        }

        public void IncrementTimeout()
        {
            lock (_lock)
                _timeout++;
        }

        public bool IsConnectionExpired()
        {
            lock (_lock)
            {
                if (_timeout > Server.CLIENT_TIMEOUT)
                {
                    return true;
                }
            }
            return false;
        }

        public void ResetTimeout()
        {
            lock (_lock)
                _timeout = 0;
        }
    }
}