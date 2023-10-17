namespace SnowProfileScanner.Models
{
    public class TemperatureProfile
    {
        public IEnumerable<SnowProfile> Layers { get; set; }
        public string? AirTemp { get; set; }
        public IEnumerable<SnowTemperature> SnowTemp { get; set; }

        public TemperatureProfile()
        {
            SnowTemp = new List<SnowTemperature>();
            Layers = new List<SnowProfile>();
        }

        public class SnowTemperature
        {
            public string Depth { get; set; }
            public string Temp { get; set; }
        }

        public class SnowProfile
        {
            public string Thickness { get; set; }
            public string Hardness { get; set; }
            public string Grain { get; set; }
            public string? Size { get; set; }
            public string LWC { get; set; }
        }
    }
    
}
