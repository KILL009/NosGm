using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.Algorithm
{
    public interface IDistanceDefenceService
    {
        long GetDistanceDefence(ClassType entityClass, byte level);
    }
}
