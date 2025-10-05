using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace StreamingVideo.Server {
    internal class SimpleVideoServer {

        private HttpListener _listener = new HttpListener();
        public string? FilePath { get; set; }
        public void Start(int port) {
            string prefix = $"https://+:{port}/";
            _listener.Prefixes.Add(prefix);
            _listener.Start();
            Debug.WriteLine($"Server avviato su {prefix}");

            Task.Run(async () => {
                while (_listener.IsListening) {
                    var ctx = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(ctx));
                }
            });
        }

        public void SetPathVideo(string path) {

            if (!File.Exists(path)) {
                throw new Exception("File doesn't exists");
            }
            FilePath = path; 
        }

        private void HandleRequest(HttpListenerContext ctx) {
            var req = ctx.Request;
            var resp = ctx.Response;

            if (req.Url.AbsolutePath.StartsWith("/")) {
                //var fileName = req.Url.AbsolutePath.Replace("/videos/", "");
                //var filePath = Path.Combine(@"C:\Users\luca-\Videos\", fileName);

                //filePath = @"C:\Users\luca-\Videos\Desktop\Desktop 2023.12.13 - 16.12.28.02.mp4";
                //if (!File.Exists(filePath)) {
                //    resp.StatusCode = 404;
                //    resp.Close();
                //    return;
                //}

                if (string.IsNullOrEmpty(FilePath))
                    return;
                var fileInfo = new FileInfo(FilePath);
                long totalSize = fileInfo.Length;
                long start = 0, end = totalSize - 1;

                if (req.Headers["Range"] != null) {
                    var m = Regex.Match(req.Headers["Range"], @"bytes=(\d*)-(\d*)");
                    if (m.Success) {
                        if (!string.IsNullOrEmpty(m.Groups[1].Value))
                            start = long.Parse(m.Groups[1].Value);
                        if (!string.IsNullOrEmpty(m.Groups[2].Value))
                            end = long.Parse(m.Groups[2].Value);
                    }
                    resp.StatusCode = 206; // Partial Content
                    resp.AddHeader("Content-Range", $"bytes {start}-{end}/{totalSize}");
                }

                resp.ContentType = "video/mp4";
                resp.AddHeader("Accept-Ranges", "bytes");

                using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs.Seek(start, SeekOrigin.Begin);

                byte[] buffer = new byte[64 * 1024];
                long remaining = end - start + 1;
                while (remaining > 0) {
                    int read = fs.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    if (read <= 0) break;
                    try {
                        resp.OutputStream.Write(buffer, 0, read);
                    }
                    catch { }
                    remaining -= read;
                }
                resp.Close();
            }
            else {
                resp.StatusCode = 200;
                using var writer = new StreamWriter(resp.OutputStream);
                writer.Write("Simple Video Server running");
                resp.Close();
            }
        }
    }
}
