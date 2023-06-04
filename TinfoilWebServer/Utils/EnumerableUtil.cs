using System.Collections.Generic;
using System.Linq;

namespace TinfoilWebServer.Utils;

public static class EnumerableUtil
{
    public static bool SequenceEqual<T>(IEnumerable<T>? e1, IEnumerable<T>? e2)
    {
        if(e1 == null && e2 == null || ReferenceEquals(e1, e2))
            return true;

        if (e1 == null || e2 == null)
            return false;

        return e1.SequenceEqual(e2);
    }

}