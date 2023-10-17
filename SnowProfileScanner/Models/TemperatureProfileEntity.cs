using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SnowProfileScanner.Models;
namespace SnowProfileScanner.Models
{
    public class TemperatureProfileEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string ImageUrl { get; set; }
        public string Name { get; set; }
        public TemperatureProfile TemperatureProfile { get; set; }
        DateTimeOffset ITableEntity.Timestamp { get; set; }
        public string ETag { get; set; }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            this.ImageUrl = properties["ImageUrl"].StringValue;
            this.Name = properties["Name"].StringValue;
            if (properties.ContainsKey("TemperatureProfile"))
            {
                var temperatureProfileJson = properties["TemperatureProfile"].StringValue;
                this.TemperatureProfile = JsonConvert.DeserializeObject<TemperatureProfile>(temperatureProfileJson);
            }
        }
        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var properties = new Dictionary<string, EntityProperty>
        {
            { "PartitionKey", new EntityProperty(this.PartitionKey) },
            { "RowKey", new EntityProperty(this.RowKey) },
            { "ImageUrl", new EntityProperty(this.ImageUrl) },
            { "Name", new EntityProperty(this.Name) }
        };

            if (!string.IsNullOrEmpty(this.ETag))
            {
                properties.Add("ETag", new EntityProperty(this.ETag));
            }

            if (this.TemperatureProfile != null)
            {
                var temperatureProfileJson = JsonConvert.SerializeObject(this.TemperatureProfile);
                properties.Add("TemperatureProfile", new EntityProperty(temperatureProfileJson));
            }

            return properties;
        }
    }
}