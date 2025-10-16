using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace StreamingVideo.Server {
    class DynuDns {
        private const string BaseUrl = "https://api.dynu.com/nic/update?";
        public static string Update(String host, string username, string password) {
            string resp = null;
            try {
                string url = BuildUrl(host, username, password);
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = client.Send(request);
                resp = response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex) {
                resp = ex.Message;
            }
            return resp;
        }

        private static string BuildUrl(string hostname, string username, string password) {
            return $"{BaseUrl}hostname={hostname}&username={username}&password={password}";
        }
    }
}
