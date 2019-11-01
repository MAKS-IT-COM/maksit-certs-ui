namespace LetsEncrypt.Helpers
{
    public class AppSettings {
        public string active { get; set; }
        public Environment [] environments { get; set; }
        public Customer [] customers { get; set;}
    }

    public class Environment {
        public string name { get; set; }
        public string url { get; set; }
        public string www { get; set; }
        public string acme { get; set; }
        public string ssl { get; set; }
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
