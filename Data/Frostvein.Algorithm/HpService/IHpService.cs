using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.Algorithm
{
    public interface IHpService
    {
        long GetHp(ClassType entityClass, byte level);
    }
}
