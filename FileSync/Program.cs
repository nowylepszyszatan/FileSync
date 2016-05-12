using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace FileSync
{
    class Program : IDisposable
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 1)
                {
                    using (Program p = new Program())
                    {
                        p.RunThruFile(args[0]);
                    }
                } 
            }
            catch(Exception exc)
            {
                Console.Error.WriteLine("Something went terribly wrong: " + exc.ToString());
            }
        }

        public Program()
        {
            string logFile = @"C:\Users\aniol\logs\" + "fileSync" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".log";
            _log = new Logger(logFile);
        }

        public void RunThruFile(string path)
        {
            _log.Info("Opening config file: " + path);

            using (var reader = new StreamReader(path))
            {
                var doc = new XmlDocument();

                doc.LoadXml(reader.ReadToEnd());

                var directoryList = doc.SelectNodes("/root/directory");

                foreach(XmlNode dirNode in directoryList)
                {
                    string sourceDir = dirNode.SelectSingleNode("source").FirstChild.InnerText;
                    string destDir = dirNode.SelectSingleNode("destination").FirstChild.InnerText;
                    string type = dirNode.SelectSingleNode("type").FirstChild.InnerText;
                    string fileMatch = dirNode.SelectSingleNode("fileMatch").FirstChild.InnerText;

                    _log.Info("Found node:");
                    _log.Info("\t" + "source: " + sourceDir);
                    _log.Info("\t" + "destination: " + destDir);
                    _log.Info("\t" + "type: " + type);
                    _log.Info("\t" + "fileMatch: " + fileMatch);

                    if (sourceDir == destDir)
                    {
                        _log.Warning("Source and destination are the same: " + sourceDir);
                        _log.Warning("Ommiting directory");

                        continue;
                    }

                    switch (type)
                    {
                        case "mirror": MirrorSync(sourceDir, destDir, fileMatch); break;
                        default: _log.Warning("Unkown synchronisation type: " + type); break;
                    }
                }
            }
        }

        public void MirrorSync(string source, string destination, string fileMatch)
        {
            _log.Info("Starting mirror sync...");

            var sourceFiles = GetFileList(source, fileMatch);
            var destinationFiles = GetFileList(destination);
            var sourceDirs = GetDirectoryList(source);
            var destinationDirs = GetDirectoryList(destination);
            var filesToRemove = destinationFiles.Except(sourceFiles).ToList();
            var dirsToRemove = destinationDirs.Except(sourceDirs).ToList();

            if (sourceFiles.Count == 0)
            {
                _log.Info("Nothing to sync.");
                return;
            }

            // delete from server all files that are not present on client
            foreach (string path in filesToRemove)
            {
                DeleteFile(destination + path);
            }

            // delete from server all dirs that are not present on client
            foreach (string path in dirsToRemove)
            {
                DeleteDirectory(destination + path);
            }

            // Create destination directories if not exists
            CheckDirectory(destination);
            foreach (var path in sourceDirs)
            {
                CheckDirectory(destination + path);
            }

            // now sync all files from source to dest
            foreach (string path in sourceFiles)
            {
                try
                {
                    FileInfo sourceFile = new FileInfo(source + path);
                    FileInfo destinationFile = new FileInfo(destination + path);

                    if (!destinationFile.Exists)
                    {
                        _log.Info("Creating new file: " + "\t" + destination + path);

                        sourceFile.CopyTo(destination + path);
                    }
                    else if (sourceFile.LastWriteTime > destinationFile.LastWriteTime)
                    {
                        _log.Info("Overriding file: " + "\t" + destination + path);

                        sourceFile.CopyTo(destination + path, true);
                    }
                }
                catch (Exception exc)
                {
                    _log.Error("Exception while syncing file: " + source + path);
                    _log.Error(exc.ToString());
                }
            }

            _log.Info("Finished mirror sync");
        }

        public List<string> GetFileList(string path, string fileMatch = "*")
        {
            var dirInfo = new DirectoryInfo(path);
            var paths = new List<string>();
            var pathLenght = path.Length;

            if (dirInfo.Exists)
            {
                var files = dirInfo.GetFiles(fileMatch, SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    paths.Add(file.FullName.Substring(pathLenght));
                }
            }

            return paths;
        }

        public List<string> GetDirectoryList(string path)
        {
            var dirInfo = new DirectoryInfo(path);
            var dirPaths = new List<string>();
            var pathLenght = path.Length;

            if (dirInfo.Exists)
            {
                var dirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories);

                var paths = from dir in dirs select dir.FullName.Substring(pathLenght);

                dirPaths = paths.ToList();
            }

            return dirPaths;
        }

        public void DeleteFile(string path)
        {
            _log.Info("Removing file: " + "\t" + path);

            try
            {
                File.Delete(path);
            }
            catch (Exception exc)
            {
                _log.Error("    Cannot remove file: " + path + " due to exception:");
                _log.Error("    " + exc.ToString());
            }
        }

        public void DeleteDirectory(string path)
        {
            _log.Info("Removing directory: " + "\t" + path);

            try
            {
                Directory.Delete(path);
            }
            catch (Exception exc)
            {
                _log.Error("    Cannot remove directory: " + path + " due to exception:");
                _log.Error("    " + exc.ToString());
            }
        }

        public void CheckDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                _log.Info("Creating directory: " + "\t" + path);
                Directory.CreateDirectory(path);
            }
        }


        public void Dispose()
        {
            _log.Info("Closing log file");
            _log.Close();
        }

        private Logger _log;
    }
}
