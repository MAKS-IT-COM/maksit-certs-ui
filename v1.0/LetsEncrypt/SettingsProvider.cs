using System;
using System.IO;
using Newtonsoft.Json;

namespace LetsEncrypt
{
    

    public class SettingsProvider
    {
        private readonly string _path;
        public Settings settings;
        public SettingsProvider(string path) {
            _path = path ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

            if(!File.Exists(_path))
                throw new FileNotFoundException(string.Format("Settings file \"{0}\" not found."), _path);

            settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(_path));
        }
    }

    public class Settings {
        public string url { get; set; }
        public string www { get; set; }
        public string acme { get; set; }
        public string ssl { get; set; }

        public Customer [] customers { get; set;}
    }

    public class Customer {
        public string id { get; set; }
        public string [] contacts { get; set; }
        public string name { get; set; }
        public string lastname { get; set; }
        public Site [] sites { get; set; }
    }

    public class Site {
        public string root { get; set; }
        public string name { get; set; }
        public string [] hosts { get; set; }
        public string challenge { get; set; }
    }


    
}