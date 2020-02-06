﻿using Newtonsoft.Json;

namespace DynamoDbServerless
{
    public class ActorsSearchRequest
    {
        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }
    }
}