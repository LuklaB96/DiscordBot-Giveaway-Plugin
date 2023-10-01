using Giveaway.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Giveaway.Entities
{
    public class User : IUser
    {
        public ulong Id { get; private set; }
        public User(ulong id)
        {
            Id = id;
        }
    }
}
