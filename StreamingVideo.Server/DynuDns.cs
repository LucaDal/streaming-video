using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace StreamingVideo.Server {
    class DynuDns {
        private const string BaseUrl = "https://update.dyndns.it/nic/update?";
        /// <summary>
        /// Call this from another class to update a zone.
        /// </summary>
        /// <param name="host">The full name of the host</param>
        /// <returns></returns>
        public static string Update(String host, string username, string password) {
            string url = BuildUrl(host);
            return PerformUpdate(url, username, password);
        }

        private static string BuildUrl(String hostname) {
            return BaseUrl + "hostname=" + hostname;
        }
        /// <summary>
        /// Performs the actual request to the dyndns server to update the entity
        /// </summary>
        /// <param name="url">url to post</param>
        private static string PerformUpdate(String url, string username, string password) {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            NetworkCredential creds = new NetworkCredential(username, password);
            request.Credentials = creds;
            request.Method = "GET";
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream reply = response.GetResponseStream();
            StreamReader readReply = new StreamReader(reply);
            return readReply.ReadToEnd();
        }
    }
}
