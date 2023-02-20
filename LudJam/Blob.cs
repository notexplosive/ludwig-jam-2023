using System;
using System.Collections.Generic;
using ExplogineCore;
using Newtonsoft.Json;

namespace LudJam;

public class Blob
{
    // Must be public to be serialized
    public readonly List<ISerializedContent> Content = new();

    public void Add(ISerializedContent data)
    {
        Content.Add(data);
    }

    public string[] AsJson()
    {
        var blob = JsonConvert.SerializeObject(this, Formatting.Indented);
        return blob.SplitLines();
    }
}