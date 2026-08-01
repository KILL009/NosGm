using NosGm.DAL.EF.Interceptors;
using System.Data.Entity;

namespace NosGm.DAL.EF
{
    public class NosGmDbConfiguration : DbConfiguration
    {
        public NosGmDbConfiguration()
        {
            AddInterceptor(new SlowQueryInterceptor());
        }
    }
}
