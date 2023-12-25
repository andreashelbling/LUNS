using LUNS.Library;
using System;

namespace LUNS.Demo
{
    class Logger : ILogger
    {
        public void ErrorFormat(string format, params object[] args)
        {
            Console.WriteLine("ERROR: " + format, args);
        }

        public void InfoFormat(string format, params object[] args)
        {
            Console.WriteLine("INFO : " + format, args);
        }

        public void WarnFormat(string format, params object[] args)
        {
            Console.WriteLine("WARN : " + format, args);
        }
    }
}
