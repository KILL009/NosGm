using System;

namespace Frostvein.SCS.Communication.ScsServices.Service
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class ScsServiceAttribute : Attribute
    {
        public string Version { get; set; }

        public ScsServiceAttribute() => this.Version = "NO_VERSION";
    }
}
