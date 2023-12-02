using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TinfoilWebServer.Utils;

public static class ListExtension
{

    public static bool TryRemoveFirst<T>(this List<T> list, [NotNullWhen(true)] out T? item)
    {
        if (list.Count > 0)
        {
            item = list[0]!;
            list.RemoveAt(0);
            return true;
        }

        item = default;
        return false;
    }

}