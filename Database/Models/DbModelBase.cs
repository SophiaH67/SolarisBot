using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolarisBot.Database
{
    public abstract class DbModelBase
    {
        public DbModelBase()
        {
            CreatedAt = Utils.GetCurrentUnix();
            UpdatedAt = CreatedAt;
        }

        public ulong CreatedAt { get; set; }
        public ulong UpdatedAt { get; set; }
    }
}
