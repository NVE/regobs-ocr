using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SnowProfileScanner.Models;
namespace SnowProfileScanner.Models
{
    public class SnowProfileEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string ImageUrl { get; set; }
        public string? PlotUrl { get; set; }
        public string Name { get; set; }
        public SnowProfile SnowProfile { get; set; }
        DateTimeOffset ITableEntity.Timestamp { get; set; }
        public string ETag { get; set; }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            this.ImageUrl = properties["ImageUrl"].StringValue;
            properties.TryGetValue("PlotUrl", out var plotUrl);
            this.PlotUrl = plotUrl?.StringValue;
            this.Name = properties["Name"].StringValue;
            if (properties.ContainsKey("SnowProfile"))
            {
                var snowProfileJson = properties["SnowProfile"].StringValue;
                this.SnowProfile = JsonConvert.DeserializeObject<SnowProfile>(snowProfileJson);
            }
        }
        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var properties = new Dictionary<string, EntityProperty>
        {
            { "PartitionKey", new EntityProperty(this.PartitionKey) },
            { "RowKey", new EntityProperty(this.RowKey) },
            { "ImageUrl", new EntityProperty(this.ImageUrl) },
            { "PlotUrl", new EntityProperty(this.PlotUrl) },
            { "Name", new EntityProperty(this.Name) }
        };

            if (!string.IsNullOrEmpty(this.ETag))
            {
                properties.Add("ETag", new EntityProperty(this.ETag));
            }

            if (this.SnowProfile != null)
            {
                var snowProfileJson = JsonConvert.SerializeObject(this.SnowProfile);
                properties.Add("SnowProfile", new EntityProperty(snowProfileJson));
            }

            return properties;
        }
    }
}