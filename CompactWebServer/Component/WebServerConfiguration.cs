//------------------------------------------------------------------------------
// WebServerConfiguration.cs
//
// http://bansky.net
//
// This code was written by Pavel Bansky. It is released under the terms of 
// the Creative Commons "Attribution NonCommercial ShareAlike 2.5" license.
// http://creativecommons.org/licenses/by-nc-sa/2.5/
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace CompactWebServer
{
    /// <summary>
    /// Class implements configuration settings for WebServer
    /// </summary>
    public class WebServerConfiguration
    {
        #region Fields
        private int _port = 8080;
        private string _serverRoot = string.Empty;
        private string _serverName = "CompactWeb";
        private IPAddress _IPAddress = IPAddress.Parse("127.0.0.1");

        private Dictionary<string, string> mimeTypes = new Dictionary<string, string>();
        private Dictionary<string, string> virtualDirectories = new Dictionary<string, string>();
        private List<string> defaultFiles = new List<string>();
        private List<string> specialFileTypes = new List<string>();

        private WebRouter router = new WebRouter();

       
        #endregion

        /// <summary>
        /// Commucation port for incoming connections
        /// </summary>
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        /// <summary>
        /// IPAddress assigned to web server
        /// </summary>
        public IPAddress IPAddress
        {
            get { return _IPAddress; }
            set { _IPAddress = value; }
        }

        /// <summary>
        /// Server root directory
        /// </summary>
        public string ServerRoot
        {
            get { return _serverRoot; }
            set { _serverRoot = value; }
        }

        /// <summary>
        /// Server name to be sent in headers
        /// </summary>
        public string ServerName
        {
            get { return _serverName; }
            set { _serverName = value; }
        }

        /// <summary>
        /// Adds default filename
        /// </summary>
        /// <param name="fileName"></param>
        public void AddDefaultFile(string fileName)
        {
            if (!defaultFiles.Contains(fileName)) defaultFiles.Add(fileName);
        }

        /// <summary>
        /// Tries to gest existing default filename in specified directory
        /// </summary>
        /// <param name="localDirectory">Directory path</param>
        /// <returns>Filename if exists</returns>
        public string GetDefaultFileName(string localDirectory)
        {
            string defaultFile = string.Empty;

            foreach (string file in defaultFiles)
            {
                if (File.Exists(Path.Combine(localDirectory, file)))
                {
                    defaultFile = file;
                    break;
                }
            }

            return defaultFile;
        }

        /// <summary>
        /// Adds mime type to file type
        /// </summary>
        /// <param name="fileExtension">File extensions</param>
        /// <param name="mimeType">Mime type</param>
        public void AddMimeType(string fileExtension, string mimeType)
        {
            fileExtension = fileExtension.ToLower();
            if (!mimeTypes.ContainsKey(fileExtension))
                mimeTypes.Add(fileExtension, mimeType);
        }

        /// <summary>
        /// Returs mime type for given file type
        /// </summary>
        /// <param name="fileExtension">file extension</param>
        /// <returns>Mime type</returns>
        public string GetMimeType(string fileExtension)
        {
            fileExtension = fileExtension.ToLower();
            return mimeTypes.ContainsKey(fileExtension) ? mimeTypes[fileExtension] : null;
        }

        /// <summary>
        /// Adds virtual directory
        /// </summary>
        /// <param name="name">Virtual directoty name</param>
        /// <param name="path">Physical path to directory</param>
        public void AddVirtualDirectory(string name, string path)
        {
            if (!virtualDirectories.ContainsKey(name))
                virtualDirectories.Add(name, path);
        }

        /// <summary>
        /// Gets physical path for given virtual directory
        /// </summary>
        /// <param name="directory">Virutal directory name</param>
        /// <returns>Physical path</returns>
        public string GetVirtualDirectory(string directory)
        {
            return virtualDirectories.ContainsKey(directory) ? virtualDirectories[directory] : string.Empty;
        }

        /// <summary>
        /// Adds file type that need special handling
        /// </summary>
        /// <param name="fileExtension">File extension</param>
        public void AddSpecialFileType(string fileExtension)
        {
            if (!specialFileTypes.Contains(fileExtension)) specialFileTypes.Add(fileExtension);
        }

        public WebRouter Router
        {
            get { return router; }
            set { router = value; }
        }

        /// <summary>
        /// Determines whether the given file need special handling
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>True if file should be special handled</returns>
        public bool IsSpecialFileType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return specialFileTypes.Contains(extension);
        }
    }
}
