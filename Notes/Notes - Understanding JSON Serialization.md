# JSON Serialization 

## Learning Objectives

By the end of this lesson, you will be able to:
1. **Understand** what JSON is and how it maps to C# types.
2. **Read** (deserialize) JSON into a simple C# class.
3. **Write** (serialize) a C# object back to JSON text.
4. **Understand** every non-trivial line used in the serializer/deserializer.

---

## The JSON we’ll read and write

Create `Content/simple.json`:

```json
{
  "name": "GreenCrate",
  "hitPoints": 25,
  "speed": 3.5,
  "position": [2, 0, -3]
}
```

- `name` → string  
- `hitPoints` → int  
- `speed` → float (JSON calls them “numbers”; we’ll parse to `float`)  
- `position` → a 3-element array parsed into `Vector3`

We’ll allow vectors as either `[x,y,z]` or `{ "x":.., "y":.., "z":.. }` using a small converter.

---

## The C# types (simple and explicit)

Create a new file (e.g., `SimpleData.cs`) or add these near the bottom of `Main.cs`:

```csharp
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework; // for Vector3

/// <summary>
/// A very small data class that matches simple.json
/// </summary>
public sealed class SimpleThing
{
    public string Name { get; set; }       // maps to "name"
    public int HitPoints { get; set; }     // maps to "hitPoints"
    public float Speed { get; set; }       // maps to "speed"
    public Vector3 Position { get; set; }  // maps to "position"
}

/// <summary>
/// JSON converter for Microsoft.Xna.Framework.Vector3.
/// Allows two input formats:
///   1) Array form: [x, y, z]
///   2) Object form: { "x": X, "y": Y, "z": Z }
///
/// Why we need this:
/// System.Text.Json doesn't know how to read/write XNA/MonoGame's Vector3 out of the box.
/// This converter teaches the serializer how to parse Vector3 from common authoring formats,
/// and how to write it back as a clean array [x,y,z].
/// </summary>
public sealed class Vector3JsonConverter : JsonConverter<Vector3>
{
    /// <summary>
    /// Reads JSON -> Vector3. We handle two token shapes:
    /// - StartArray: expect exactly three numbers [x,y,z]
    /// - StartObject: expect properties "x","y","z" in any order
    /// Throw JsonException for anything else (e.g., strings like "0,1,2").
    /// </summary>
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Case 1: [x, y, z]
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            // Advance to first number and read three doubles (cast to float).
            reader.Read(); float x = (float)reader.GetDouble();
            reader.Read(); float y = (float)reader.GetDouble();
            reader.Read(); float z = (float)reader.GetDouble();

            // Move past the closing ]
            reader.Read();
            return new Vector3(x, y, z);
        }

        // Case 2: { "x":..., "y":..., "z":... }
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            float x = 0, y = 0, z = 0;

            // Loop through object properties until we hit the closing }
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                // Current token is a property name ("x", "y", "z")
                string name = reader.GetString();

                // Move to the property's value
                reader.Read();

                // Read the numeric value as double (JSON number) then cast to float
                float val = (float)reader.GetDouble();

                // Assign based on property name (case-insensitive)
                if (string.Equals(name, "x", StringComparison.OrdinalIgnoreCase)) x = val;
                else if (string.Equals(name, "y", StringComparison.OrdinalIgnoreCase)) y = val;
                else if (string.Equals(name, "z", StringComparison.OrdinalIgnoreCase)) z = val;
            }

            return new Vector3(x, y, z);
        }

        // Any other token shape is not supported (e.g., a string or single number)
        throw new JsonException("Vector3 must be [x,y,z] or {x,y,z}.");
    }

    /// <summary>
    /// Writes Vector3 -> JSON in array form [x, y, z].
    /// We choose array form for compactness and readability.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X); // write X component
        writer.WriteNumberValue(value.Y); // write Y component
        writer.WriteNumberValue(value.Z); // write Z component
        writer.WriteEndArray();
    }
}
```

---

## Reading (deserializing) JSON 

Add to `Main.cs` (or any suitable class):

```csharp
private SimpleThing LoadSimpleThing(string path)
{
    // 1) Read all text from disk into a string.
    string text = File.ReadAllText(path);

    // 2) Configure how the serializer should behave.
    var opts = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,          // accept "Name" or "name"
        ReadCommentHandling = JsonCommentHandling.Skip, // allow // and /* */ comments
        AllowTrailingCommas = true                   // tolerate { "a": 1, }
    };

    // 3) Teach the serializer how to parse Vector3.
    opts.Converters.Add(new Vector3JsonConverter());

    // 4) Convert JSON text -> SimpleThing instance using our options.
    SimpleThing thing = JsonSerializer.Deserialize<SimpleThing>(text, opts);

    return thing;
}
```

**What the non-trivial code does:**
- `File.ReadAllText` — loads the JSON file into memory as plain text.
- `JsonSerializerOptions` — a bag of toggles that makes parsing friendlier for humans (comments, trailing commas, case-insensitive).
- `opts.Converters.Add(new Vector3JsonConverter())` — registers our custom Vector3 reader/writer.
- `JsonSerializer.Deserialize<SimpleThing>(text, opts)` — parses the string into your C# object.

---

## Writing (serializing) JSON — line-by-line

```csharp
private void SaveSimpleThing(string path, SimpleThing thing)
{
    // 1) Pretty-print so humans (and Git diffs) can read it easily.
    var opts = new JsonSerializerOptions { WriteIndented = true };

    // 2) Ensure Vector3 writes as [x,y,z].
    opts.Converters.Add(new Vector3JsonConverter());

    // 3) Convert C# object -> JSON string.
    string json = JsonSerializer.Serialize(thing, opts);

    // 4) Save the JSON text back to disk.
    File.WriteAllText(path, json);
}
```

**What the non-trivial lines do:**
- `WriteIndented = true` — adds whitespace/newlines for readability.
- `Serialize(thing, opts)` — turns your object into a JSON string using the converter.
- `File.WriteAllText` — writes the string to a file.

---

## Mini-lab (10–15 minutes)

1) Create `Content/simple.json` and load it with `LoadSimpleThing`.  
2) Add a trailing comma and a comment to the JSON — it should still parse.  
3) Change `position` to object form `{ "x": 5, "y": 1, "z": -2 }` — confirm it still loads.  
4) Modify values in code and save as `simple_out.json`.  
5) Add a new field (e.g., `"rarity": "uncommon"`) to the JSON and a matching property to `SimpleThing`, then confirm it round-trips.

---

## Summary
- JSON is just text: objects `{}`, arrays `[]`, and numbers/strings/bools/null.
- **Deserialize** (read) = text → object; **Serialize** (write) = object → text.
- `JsonSerializerOptions` can make your files forgiving for students (comments, trailing commas, case-insensitive keys).
- A tiny **Vector3 converter** makes authoring positions as `[x,y,z]` or `{x,y,z}` easy.
- You now have everything needed to tackle the model-spawn loader yourself next.
