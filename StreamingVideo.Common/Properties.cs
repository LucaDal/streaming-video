using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media.Media3D;

namespace StreamingVideo.Common {
    public class Properties {
        [JsonIgnore]
        public static Properties Instance { get; } = Create(Path.Combine(Directory.GetCurrentDirectory(), "properties.json"));
        public string DynuHostname { get; set; } = string.Empty;
        public string DynuUsername { get; set; } = string.Empty;
        public string DynuPassword { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        private string FileName = Path.Combine(Directory.GetCurrentDirectory(), "properties.json");

        public static Properties Create(string path) {

            Properties toRet = new Properties();
            if (!File.Exists(path))
                return toRet;
            try {
                string readText = File.ReadAllText(path);

                var prop = JsonSerializer.Deserialize<Properties>(readText);

                if (prop == null)
                    return toRet;

                foreach (var pi in prop.GetType().GetProperties( BindingFlags.Public | BindingFlags.Instance)) {
                    var value = pi.GetValue(prop); 
                    pi.SetValue(toRet, value);
                }
            }catch(Exception ex) {
                Debug.WriteLine(ex.Message);
            }
            return toRet;
        }
        public void Save() {
            string stringProp = JsonSerializer.Serialize(this);
            File.WriteAllText(FileName, stringProp);
        }
    }
}
