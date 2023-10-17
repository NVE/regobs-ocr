namespace SnowProfileScanner.Models
{
    public class TemperatureProfile
    {
        public IEnumerable<SnowProfile> Layers { get; set; }
        public double? AirTemp { get; set; }
        public IEnumerable<SnowTemperature> SnowTemp { get; set; }

        public TemperatureProfile()
        {
            SnowTemp = new List<SnowTemperature>();
            Layers = new List<SnowProfile>();
        }

        public class SnowTemperature
        {
            public double? Depth { get; set; }
            public double? Temp { get; set; }
        }

        public class SnowProfile
        {
            public double? Thickness { get; set; }
            public string? Hardness { get; set; }
            public string? Grain { get; set; }
            public double? Size { get; set; }
            public string? LWC { get; set; }
        }
    }
}
