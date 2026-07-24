# Lab: JSON Serialization

## Overview
In this lab, you’ll author a small **JSON file** that describes a single model spawn (position, rotation in degrees, scale, texture key, model key, and object name), then write a few **C# helpers** to read the JSON and **call `InitializeModel(...)`** with those values.

We’ll load JSON during your **`Main.Initialize()`** setup phase—**after** assets are loaded (so model/texture keys exist) and **before** the game starts updating. This keeps data-driven spawns **declarative** (editable without code) and repeatable across runs.

## Rationale
- Your `Main` already has a helper **`InitializeModel(...)`** that takes `Vector3 position`, `Vector3 eulerRotationDegrees`, `Vector3 scale`, `string textureName`, `string modelName`, and `string objectName`. We’ll reuse it instead of re-implementing model setup.
- Students can tweak **spawn data** in JSON without recompiling.
- We’ll use `System.Text.Json` with a tiny **`Vector3JsonConverter`** so authors can type vectors as `[x, y, z]`.

---

## Prerequisites
- The content dictionaries already map at least one model (e.g., `"crate"`) and texture (e.g., `"crate1"`). These keys must match your JSON.
- Ensure your JSON file is set to **Copy to Output Directory** (`Copy if newer`), so the loader can find it at runtime.

> Reference: `Main.InitializeModel(...)` exists and creates a GameObject with transform + components using your dictionaries.

---

## End-State (what your project should have by the end)

**Folders & files**

```
GDEngine
└─ Core
   └─ Serialization
      ├─ ModelSpawnData.cs
      ├─ Vector3JsonConverter.cs
      └─ JSONSerializationUtility.cs
Main.cs
Content/
  ├─ single_model_spawn.json
  └─ multi_model_spawn.json
```

**Runtime behavior**

- At start, you call a method that reads **one** spawn file (single object) or **many** (list of objects), and for each entry the engine calls `InitializeModel(...)` to create a `GameObject` with the desired transform, texture, model, and name.

---

# Part A — Author the JSON (data-first)

Create these two files in your **Content** folder. It is **critical** that you set each file to **Copy to Output Directory → Copy if newer**.

### `single_model_spawn.json`
```json
{
  "Position": [0, 5, 0],
  "RotationDegrees": [0, 0, 0],
  "Scale": [1, 1, 1],
  "TextureName": "crate1",
  "ModelName": "crate",
  "ObjectName": "Crate_A"
}
```

### `multi_model_spawn.json`
```json
[
  {
    "Position": [2, 5, -3],
    "RotationDegrees": [0, 30, 0],
    "Scale": [1.2, 1.2, 1.2],
    "TextureName": "crate1",
    "ModelName": "crate",
    "ObjectName": "Crate_01"
  },
  {
    "Position": [-6, 5, 4],
    "RotationDegrees": [0, -20, 0],
    "Scale": [0.8, 0.8, 0.8],
    "TextureName": "crate2",
    "ModelName": "crate",
    "ObjectName": "Crate_02"
  }
]
```

> The converter in Part C will also accept object-form vectors like `{ "x": 0, "y": 5, "z": 0 }` if you prefer that style.

---

# Part B — Create the data-only class

Create `GDEngine/Core/Serialization/ModelSpawnData.cs`:

```csharp
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Serialization
{
    /// <summary>
    /// Plain data container for a single model spawn.
    /// </summary>
    public sealed class ModelSpawnData
    {
        public Vector3 Position { get; set; }
        public Vector3 RotationDegrees { get; set; }
        public Vector3 Scale { get; set; }

        public string TextureName { get; set; }
        public string ModelName { get; set; }
        public string ObjectName { get; set; }
    }
}
```

- This mirrors the JSON keys.
- No behavior here—**just data** the engine will consume.

---

# Part C — Implement a `Vector3` JSON converter

Create `GDEngine/Core/Serialization/Vector3JsonConverter.cs`:

```csharp
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Serialization
{
    /// <summary>
    /// JSON converter for Vector3.
    /// Accepts array form [x,y,z] and object form {"x":X,"y":Y,"z":Z}.
    /// Writes [x,y,z].
    /// </summary>
    public sealed class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read(); float x = (float)reader.GetDouble();
                reader.Read(); float y = (float)reader.GetDouble();
                reader.Read(); float z = (float)reader.GetDouble();
                reader.Read(); // EndArray
                return new Vector3(x, y, z);
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                float x = 0, y = 0, z = 0;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    string name = reader.GetString();
                    reader.Read();
                    float val = (float)reader.GetDouble();
                    if (string.Equals(name, "x", StringComparison.OrdinalIgnoreCase)) x = val;
                    else if (string.Equals(name, "y", StringComparison.OrdinalIgnoreCase)) y = val;
                    else if (string.Equals(name, "z", StringComparison.OrdinalIgnoreCase)) z = val;
                }
                return new Vector3(x, y, z);
            }

            throw new JsonException("Vector3 must be [x,y,z] or {x,y,z}.");
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.Z);
            writer.WriteEndArray();
        }
    }
}
```

**Why a converter?** `System.Text.Json` doesn’t know how to construct `Vector3` out-of-the-box. The converter teaches it to parse and emit the form we want.

---

# Part D — Add loader with a single reusable utility

Create `GDEngine/Core/Serialization/JSONSerializationUtility.cs` and paste **your** implementation below.  
This single method loads one **or** many records depending on whether the root is `{...}` or `[...]`, and **always** returns a `List<T>`.

```csharp
﻿using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GDEngine.Core.Serialization
{
    public static class JSONSerializationUtility
    {
        public static List<T> LoadData<T>(ContentManager content, string relativePath)
        {
            string path = Path.Combine(content.RootDirectory, relativePath);
            string json = File.ReadAllText(path);

            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            opts.Converters.Add(new Vector3JsonConverter());

            int i = 0;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            bool isArray = i < json.Length && json[i] == '[';

            if (isArray)
            {
                var many = JsonSerializer.Deserialize<List<T>>(json, opts);
                if (many != null)
                    return many;

                return new List<T>();
            }
            else
            {
                var one = JsonSerializer.Deserialize<T>(json, opts);
                var list = new List<T>();
                if (one != null)
                    list.Add(one);

                return list;
            }
        }

    }
}

```

> Notes:
> - Registers `Vector3JsonConverter` so `Vector3` fields deserialize from either arrays or objects.
> - Detects a root `'['` to choose array vs object parsing.
> - Normalizes to `List<T>` so calling code is simple.

---

# Part E — Wire it in `Main` 

Add a **demo method** somewhere appropriate (e.g., after assets are loaded) and **call it from your initialization flow**. You do **not** need to change any other `Main` code.

**Snippet — demo usage**
```csharp
private void DemoLoadFromJSON()
{
    foreach (var d in JSONSerializationUtility.LoadData<ModelSpawnData>(Content, "single_model_spawn.json"))
        InitializeModel(d.Position, d.RotationDegrees, d.Scale, d.TextureName, d.ModelName, d.ObjectName);

    foreach (var d in JSONSerializationUtility.LoadData<ModelSpawnData>(Content, "multi_model_spawn.json"))
        InitializeModel(d.Position, d.RotationDegrees, d.Scale, d.TextureName, d.ModelName, d.ObjectName);
}

// somewhere in your startup (e.g., Initialize())
DemoLoadFromJSON();
```

That’s it—no other `Main` changes required for this lab.

---

# Checkpoints

- [ ] JSON files exist in `Content/` and are **copied to output**.
- [ ] `ModelSpawnData` compiles and matches the JSON keys.
- [ ] `Vector3JsonConverter` is implemented.
- [ ] `JSONSerializationUtility.LoadData<T>` works for `{}` and `[]` roots.
- [ ] Objects appear with expected position, rotation (degrees), scale, texture, and name.

---

# Troubleshooting

- **File not found** — Ensure *Copy to Output Directory: Copy if newer* for your JSON files and that the relative path matches the deployed filename.
- **Vectors wrong / exceptions** — Verify the converter registration in the utility and that your JSON vector shapes are valid.
- **Nothing draws** — Confirm model/texture keys exist in your content dictionaries and match the JSON strings.
- **Rotation feels off** — `RotationDegrees` expects **degrees**. If your internals use radians, convert once in `InitializeModel(...)`.

---

## Suggested Improvements 

- **Optional fields**: add `Layer` and `IsStatic` to the JSON and apply them to the `GameObject` during spawn.  
- **Materials**: add `"MaterialType": "Unlit|Lit|AlphaCutout"` and branch to the correct effect setup.  
- **Save back**: author a quick “snapshot” command that serializes a selected object’s current transform to JSON for roundtrips.
