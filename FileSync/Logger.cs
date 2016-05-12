using System;
using System.IO;

namespace FileSync
{
    class Logger
    {
        public Logger(string path)
        {
            _stream = new StreamWriter(path);  
        }

        public Logger WriteLine(string line = "")
        {
            string time = DateTime.Now.ToString();

            _stream.WriteLine(time + "\t" + line);

            return this;
        }

        public Logger Warning(string line = "")
        {
            WriteLine("Warning" + "\t" + line);

            return this;
        }

        public Logger Error(string line = "")
        {
            WriteLine("Error" + "\t" + line);

            return this;
        }

        public Logger Info(string line = "")
        {
            WriteLine("Info" + "\t" + line);

            return this;
        }

        public void Close()
        {
            _stream.Flush();
            _stream.Close();
        }


        private StreamWriter _stream;
    }
}
