using System.Text.Json;
using System.Text.Json.Nodes;
using TinfoilWebServer.Services.JSON;

namespace TinfoilWebServer.Test.Services.JSON;

public class JsonMergerTest
{
    private readonly JsonMerger _jsonMerger = new();
    private readonly JsonSerializerOptions _opt = new() { WriteIndented = true };

    [Theory]
    [InlineData(
        """
        {
          "p1": "v1"
        }
        """,
        """
        {
          "p2": "v1"
        }
        """,
        """
        {
          "p1": "v1",
          "p2": "v1"
        }
        """
        )]
    [InlineData( // Existing array items are not added twice
        """
        {
          "p1": ["v1", false, true, 5]
        }
        """,
        """
        {
          "p1": [5, "v1", true, false]
        }
        """,
        """
        {
          "p1": ["v1", false, true, 5]
        }
        """
    )]
    [InlineData( // Base json properties are overridden
        """
        {
          "p0": {"sp0": null, "sp1": "Cool!"},
          "p1": "Hello",
          "P2": {"sp1":"A", "sp2": "B"},
          "p3": [1,2,3],
          "p4": null,
          "p5": true
        }
        """,
        """
        {
          "p0": false,
          "p1": "Bye",
          "P2": {"sp1":"B", "sp2": null},
          "p3": -1,
          "p4": [4,3,2]
        }
        """,
        """
        {
          "p0": false,
          "p1": "Bye",
          "P2": {"sp1":"B", "sp2": null},
          "p3": -1,
          "p4": [4,3,2],
          "p5": true
        }
        """
    )]
    [InlineData( // Maximum cases
        """
        {
          "p0": {"sp0": null, "sp1": "Cool!"},
          "p1": true
        }
        """,
        """
        {
          "p0": {"sp0": null, "sp2": -5 },
          "p2": false,
          "p3": "Hello",
          "p4": null
        }
        """,
        """
        {
          "p0": {"sp0": null, "sp1": "Cool!", "sp2": -5 },
          "p1": true,
          "p2": false,
          "p3": "Hello",
          "p4": null
        }
        """
    )]
    public void Merge(string json1, string json2, string expectedJson)
    {
        var obj1 = (JsonObject)JsonNode.Parse(json1)!;
        var obj2 = (JsonObject)JsonNode.Parse(json2)!;
        var expectedObj = (JsonObject)JsonNode.Parse(expectedJson)!;


        var mergedObj = _jsonMerger.Merge(obj1, obj2);

        Assert.Equal(expectedObj.ToJsonString(_opt), mergedObj.ToJsonString(_opt));

        Assert.Equal(((JsonObject)JsonNode.Parse(json1)!).ToJsonString(_opt), obj1.ToJsonString(_opt)); // Ensure input json is not modified
        Assert.Equal(((JsonObject)JsonNode.Parse(json2)!).ToJsonString(_opt), obj2.ToJsonString(_opt)); // Ensure input json is not modified
    }

    [Fact]
    public void MergeNullsAreIgnored()
    {
        var baseObj = new JsonObject();
        var mergedObj = _jsonMerger.Merge(baseObj, null, null, null);
        Assert.NotSame(baseObj, mergedObj);
        Assert.True(JsonNode.DeepEquals(baseObj, mergedObj));
    }
}