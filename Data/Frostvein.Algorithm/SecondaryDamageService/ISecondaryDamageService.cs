using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.Algorithm
{
    public interface ISecondaryDamageService
    {
        long GetSecondaryMinDamage(ClassType entityClass, byte level);
        long GetSecondaryMaxDamage(ClassType entityClass, byte level);
    }
}
