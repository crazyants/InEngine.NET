﻿using Newtonsoft.Json;
using System;

namespace IntegrationEngine.Client
{
    public class JsonConvertAdapter : IJsonConvert
    {
        public virtual T DeserializeObject<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }

        public virtual string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value);
        }
    }
}

