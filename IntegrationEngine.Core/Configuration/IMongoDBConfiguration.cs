using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Configuration
{
    public interface IMongoDBConfiguration : IIntegrationPointConfiguration
    {
        string HostName { get; set; }
    }
}
