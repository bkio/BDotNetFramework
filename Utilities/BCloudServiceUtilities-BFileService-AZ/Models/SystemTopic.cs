using Microsoft.Azure.Management.EventGrid.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BCloudServiceUtilities_BFileService_AZ.Models
{
    public class SystemTopicProperties
    {
        [JsonProperty(PropertyName = "metricResourceId")]
        public string MetricResourceId { get; set; }

        [JsonProperty(PropertyName = "provisioningState")]
        public string ProvisioningState { get; set; }

        [JsonProperty(PropertyName = "source")]
        public string Source { get; set; }

        [JsonProperty(PropertyName = "topicType")]
        public string TopicType { get; set; }
    }
    public partial class SystemTopic
    {
        public SystemTopic()
        {
            CustomInit();
        }
        public SystemTopic(
            string location, 
            string id = default(string), 
            string name = default(string), 
            string type = default(string), 
            IDictionary<string, string> tags = default(IDictionary<string, string>), 
            string provisioningState = default(string), 
            string source = default(string), 
            string metricResourceId = default(string), 
            string topicType = default(string))
        {
            Location = location;
            Id = id;
            Name = name;
            Type = type;
            Properties.MetricResourceId = metricResourceId;
            Properties.ProvisioningState = provisioningState;
            Properties.Source = source;
            Properties.TopicType = topicType;
        }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "tags")]
        public IDictionary<string,string> Tags { get; set; }

        [JsonProperty(PropertyName = "properties")]
        SystemTopicProperties Properties { get; set; } = new SystemTopicProperties();

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(Location))
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Location");
            }
            if (string.IsNullOrWhiteSpace(Id))
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Id");
            }
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Name");
            }
            if (string.IsNullOrWhiteSpace(Type))
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Type");
            }
            if(Properties == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Properties");
            }
            if(string.IsNullOrWhiteSpace(Properties.Source))
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Properties.Source");
            }
            if (string.IsNullOrWhiteSpace(Properties.TopicType))
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Properties.TopicType");
            }
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();
    }
}
