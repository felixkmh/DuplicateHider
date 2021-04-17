using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateHider.Models
{
    public struct DuplicateEntry
    {
        public Guid gameId;
        public bool isEdition;
        public string editionTag;

        public override int GetHashCode()
        {
            return gameId.GetHashCode();
        }
    }
}
