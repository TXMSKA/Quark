using System;
using System.Collections.Generic;

namespace Quark
{
    public static class Quantum
    {
        static readonly HashSet<string> uids = new();

        public static string NewUid()
        {
            string uid;
            do { uid = Guid.NewGuid().ToString(); }
            while (!uids.Add(uid));
            return uid;
        }
    }
}
