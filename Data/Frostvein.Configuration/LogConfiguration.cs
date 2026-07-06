using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.Configuration
{
    public static class LogConfiguration
    {
        public static readonly string ConnectionString = "mongodb://localhost:27017";
        public static readonly string DatabaseName = "LogService";

        public static readonly string LoadServiceModel = "LoadServiceModel";
        public static readonly string ErrorServiceModel = "ErrorServiceModel";
        public static string LoadOutput { get; set; }

    }
}
