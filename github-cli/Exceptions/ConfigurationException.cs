using System;
using System.Collections.Generic;
using System.Text;

namespace github_cli.Exceptions
{
    internal class ConfigurationException : Exception
    {
        public ConfigurationException(string item): base($"Missing configuration item: {item}")
        {
            
        }
    }
}
