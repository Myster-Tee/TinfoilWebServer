using System.Linq;
using System.Text.Json.Nodes;

namespace TinfoilWebServer.Services.JSON;

public class JsonMerger : IJsonMerger
{

    public JsonObject Merge(JsonObject baseObj, params JsonObject?[] others)
    {
        var cloneObj = (JsonObject)baseObj.DeepClone();

        foreach (var jsonObj in others)
        {
            if (jsonObj != null)
                MergeInternal(cloneObj, jsonObj);
        }

        return cloneObj;
    }


    private static void MergeInternal(JsonObject baseObj, JsonObject newObj)
    {
        foreach (var (newPropName, newPropValue) in newObj)
        {
            if (!baseObj.TryGetPropertyValue(newPropName, out var basePropValue))
            {
                // New property not found in baseObj, we add it
                baseObj.Add(newPropName, newPropValue?.DeepClone());
                continue;
            }

            // New property also in baseObj
            if (basePropValue is JsonArray basePropArray && newPropValue is JsonArray newPropArray)
            {
                foreach (var newItem in newPropArray)
                {
                    if (basePropArray.Any(baseItem => JsonNode.DeepEquals(baseItem, newItem)))
                        continue;

                    basePropArray.Add(newItem?.DeepClone());
                }
            }
            else if (basePropValue is JsonObject basePropObj && newPropValue is JsonObject newPropObj)
            {
                MergeInternal(basePropObj, newPropObj);
            }
            else
            {
                // Replace base property value with new property value
                baseObj[newPropName] = newPropValue?.DeepClone();
            }


        }
    }
}