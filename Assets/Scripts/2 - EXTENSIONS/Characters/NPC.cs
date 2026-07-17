using System;

namespace Quark
{
    public class NPC : Entity
    {
        

        [Serializable]
        public class Behavior : Addon<NPC>
        {
            
        }

        [Serializable]
        public class Navigation : Addon<NPC>
        {
            
        }
    }
}
