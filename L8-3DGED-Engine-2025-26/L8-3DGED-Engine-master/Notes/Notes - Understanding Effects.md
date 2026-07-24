# Effects in 3D Graphics in MonoGame 

## Learning Objectives
By the end of this lesson, you should be able to:

1. **Identify** the main built-in effect classes provided by MonoGame and explain their purpose.  
2. **Configure** standard shader parameters such as `World`, `View`, and `Projection` matrices.  
3. **Apply** lighting, texturing, fog, and transparency using `BasicEffect` and other effect classes.  
4. **Demonstrate** use of `AlphaTestEffect`, `DualTextureEffect`, `EnvironmentMapEffect`, and `SkinnedEffect` in simple rendering examples.  
5. **Reflect** on when to use built-in effects versus custom HLSL shaders.

---

## Overview
MonoGame provides several **ready-to-use Effect classes** that wrap common rendering techniques.  
These shaders are implemented in HLSL but exposed through simple C# APIs, allowing you to control lighting, fog, textures, and skinning without manually writing GPU code.

All of these effects inherit from the base class `Effect` in the `Microsoft.Xna.Framework.Graphics` namespace.  
You can attach them to geometry, configure their parameters (matrices, textures, lighting), and render via `DrawPrimitives` or `DrawIndexedPrimitives`.

```csharp
// Typical use of a BasicEffect for a 3D object
_basicEffect = new BasicEffect(GraphicsDevice)
{
    TextureEnabled = true,
    LightingEnabled = true,
    PreferPerPixelLighting = true
};

// Set transforms (updated per frame)
_basicEffect.World = worldMatrix;
_basicEffect.View = camera.View;
_basicEffect.Projection = camera.Projection;

// Set lighting and color
_basicEffect.DiffuseColor = new Vector3(1f, 0.8f, 0.7f);
_basicEffect.SpecularColor = new Vector3(1f, 1f, 1f);
_basicEffect.SpecularPower = 16f;
```

---

## 1) BasicEffect
**Purpose:**  
A versatile shader for general 3D rendering that supports vertex colors, texturing, fog, and up to 3 directional lights.  
Ideal for rendering most primitive geometry.

```csharp
// Configure a simple lit textured model
var effect = new BasicEffect(GraphicsDevice)
{
    TextureEnabled = true,
    LightingEnabled = true,
    PreferPerPixelLighting = true,
    FogEnabled = true
};

effect.Texture = content.Load<Texture2D>("BrickWall");
effect.DiffuseColor = new Vector3(0.9f, 0.9f, 0.9f);
effect.FogStart = 10f;
effect.FogEnd = 100f;
effect.FogColor = new Vector3(0.5f, 0.5f, 0.5f);

effect.DirectionalLight0.Enabled = true;
effect.DirectionalLight0.Direction = new Vector3(-1f, -1f, -1f);
effect.DirectionalLight0.DiffuseColor = Vector3.One;
```

| Parameter | Description | Data Type |
|------------|-------------|-----------|
| `World` | Transforms object vertices from model to world space. | `Matrix` |
| `View` | Camera view matrix, world → camera space. | `Matrix` |
| `Projection` | Projection matrix (perspective or orthographic). | `Matrix` |
| `Texture` | Texture applied to geometry. | `Texture2D` |
| `TextureEnabled` | Enables or disables texture sampling. | `bool` |
| `VertexColorEnabled` | Uses vertex colors if available. | `bool` |
| `DiffuseColor` | RGB tint of surface color. | `Vector3` |
| `SpecularColor` | Specular highlight color. | `Vector3` |
| `SpecularPower` | Shininess of specular reflection. | `float` |
| `Alpha` | Global transparency multiplier (0–1). | `float` |
| `LightingEnabled` | Enables vertex lighting. | `bool` |
| `AmbientLightColor` | Global ambient color. | `Vector3` |
| `FogEnabled` | Enables depth-based fog. | `bool` |
| `FogColor` | Fog color (RGB). | `Vector3` |
| `FogStart`, `FogEnd` | Near and far fog distances. | `float` |

---

## 2) AlphaTestEffect
**Purpose:**  
Implements **alpha testing**—pixels are either drawn or discarded based on their alpha value.  
Used for crisp transparency like leaves, chain-link fences, or glass panes.

```csharp
var effect = new AlphaTestEffect(GraphicsDevice)
{
    Texture = content.Load<Texture2D>("LeafMask"),
    DiffuseColor = Vector3.One,
    AlphaFunction = CompareFunction.Greater,
    ReferenceAlpha = 128
};
```

| Parameter | Description | Data Type |
|------------|-------------|-----------|
| `World`, `View`, `Projection` | Standard transformation matrices. | `Matrix` |
| `Texture` | Diffuse texture with alpha channel. | `Texture2D` |
| `DiffuseColor` | Color tint. | `Vector3` |
| `Alpha` | Global alpha multiplier. | `float` |
| `AlphaFunction` | Test comparison (`Greater`, `LessEqual`, etc.). | `CompareFunction` |
| `ReferenceAlpha` | Alpha threshold for the test (0–255). | `int` |

---

## 3) DualTextureEffect
**Purpose:**  
Blends two textures based on vertex color or blend factor. Common in lightmaps, detail maps, or decals.

```csharp
var effect = new DualTextureEffect(GraphicsDevice)
{
    Texture = content.Load<Texture2D>("GroundBase"),
    Texture2 = content.Load<Texture2D>("GroundDetail"),
    DiffuseColor = new Vector3(1f, 1f, 1f)
};
```

| Parameter | Description | Data Type |
|------------|-------------|-----------|
| `World`, `View`, `Projection` | Transform matrices. | `Matrix` |
| `Texture` | Base texture. | `Texture2D` |
| `Texture2` | Secondary blend texture. | `Texture2D` |
| `DiffuseColor` | Tint applied to blend result. | `Vector3` |
| `Alpha` | Global opacity. | `float` |
| `VertexColorEnabled` | Use vertex color as blending control. | `bool` |

---

## 4) EnvironmentMapEffect
**Purpose:**  
Simulates **reflections and refractions** using a cubemap.  
Useful for metallic, glass, or water-like surfaces.

```csharp
var effect = new EnvironmentMapEffect(GraphicsDevice)
{
    Texture = content.Load<Texture2D>("SteelAlbedo"),
    EnvironmentMap = content.Load<TextureCube>("SkyboxCube"),
    EnvironmentMapAmount = 0.7f,
    FresnelFactor = 0.2f,
    DiffuseColor = Vector3.One
};
```

| Parameter | Description | Data Type |
|------------|-------------|-----------|
| `World`, `View`, `Projection` | Transform matrices. | `Matrix` |
| `Texture` | Diffuse base texture. | `Texture2D` |
| `EnvironmentMap` | Cubemap for reflection. | `TextureCube` |
| `EnvironmentMapAmount` | Blend strength (0–1). | `float` |
| `FresnelFactor` | Edge reflectivity scaling. | `float` |
| `DiffuseColor` | Base color tint. | `Vector3` |
| `Alpha` | Transparency. | `float` |

---

## 5) SkinnedEffect
**Purpose:**  
Supports hardware **skinning** for animated models with bones.  
Used in character animation and other deformable meshes.

```csharp
var effect = new SkinnedEffect(GraphicsDevice)
{
    Texture = content.Load<Texture2D>("CharacterTexture"),
    WeightsPerVertex = 4,
    PreferPerPixelLighting = true
};

// Each frame:
effect.SetBoneTransforms(skeletonMatrices);
```

| Parameter | Description | Data Type |
|------------|-------------|-----------|
| `World`, `View`, `Projection` | Transformation matrices. | `Matrix` |
| `Bones` | Array of bone matrices (max 72). | `Matrix[]` |
| `Texture` | Skin texture. | `Texture2D` |
| `WeightsPerVertex` | Number of bone influences per vertex. | `int` |
| `DiffuseColor` | Surface color. | `Vector3` |
| `SpecularColor` | Specular highlight color. | `Vector3` |
| `EmissiveColor` | Glow or self-lit color. | `Vector3` |
| `SpecularPower` | Shininess. | `float` |
| `Alpha` | Transparency. | `float` |
| `PreferPerPixelLighting` | Enables per-pixel lighting. | `bool` |

---

## 6) SpriteEffect (used by SpriteBatch)
**Purpose:**  
Simple 2D shader used internally by `SpriteBatch`.  
Handles texture sampling and color modulation.

```csharp
spriteBatch.Begin(transformMatrix: Matrix.CreateScale(2f));
spriteBatch.Draw(texture, Vector2.Zero, Color.White);
spriteBatch.End();
```

| Parameter | Description | Data Type |
|------------|-------------|-----------|
| `MatrixTransform` | Combined 2D transform. | `Matrix` |
| `Texture` | Source texture. | `Texture2D` |
| `Color` | Modulation color per sprite. | `Vector4` |

---

## Summary

MonoGame’s built-in effects simplify common rendering workflows:

| Effect | Main Use | Key Features |
|--------|-----------|---------------|
| **BasicEffect** | General 3D rendering | Lighting, fog, textures |
| **AlphaTestEffect** | Alpha cutouts | Hard-edge transparency |
| **DualTextureEffect** | Layer blending | Multi-texturing |
| **EnvironmentMapEffect** | Reflections | Cubemap-based reflection |
| **SkinnedEffect** | Animated meshes | Hardware skinning |
| **SpriteEffect** | 2D rendering | Used by SpriteBatch |

They are **excellent for prototyping and teaching**—letting you focus on scene setup, transformations, and gameplay code before diving into custom HLSL materials.

---

## Reflective Questions

1. When would you choose `BasicEffect` over writing a custom `Effect` in HLSL?  
2. How does `LightingEnabled` affect vertex processing and performance?  
3. What is the visual difference between alpha blending and alpha testing?  
4. Why does `SkinnedEffect` need an array of matrices each frame?  
5. How could `EnvironmentMapEffect` be extended to support normal maps?

---

## Further Exploration

- [MonoGame Custom Effects](https://docs.monogame.net/articles/getting_started/content_pipeline/custom_effects.html)  
- [Microsoft XNA 4.0 Effect Reference](https://learn.microsoft.com/en-us/previous-versions/windows/silverlight/dotnet-windows-silverlight/bb196527(v=xnagamestudio.35))  

