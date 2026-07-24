using System.Runtime.CompilerServices;
using GDEngine.Core.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Factories
{
    /// <summary>
    /// Utility factory for building simple GPU meshes for quick demos and tests.
    /// Produces MeshFilter components ready to attach to a <see cref="GameObject"/>.
    /// </summary>
    /// <see cref="MeshFilter"/>
    /// <see cref="Entities.GameObject"/>
    public static class MeshFilterFactory
    {

        //stores a single instance of meshfilter if class on a method
        //with O(1) complexity
        private static Dictionary<string, MeshFilter> registry = new();

        #region Unlit

        #region Wireframe
        public static MeshFilter CreateMyFirstInitial(GraphicsDevice device)
        {
            //TODO - Homework
            throw new NotImplementedException("add initial (N)");
        }

        /// <summary>
        /// Creates XYZ axes as colored lines (X=Red, Y=Green, Z=Blue) starting at the origin.
        /// </summary>
        public static MeshFilter CreateAxesXYZ(GraphicsDevice device, float length = 1f)
        {
            var verts = new VertexPositionColor[6];
            // X axis (red)
            verts[0] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Red);
            verts[1] = new VertexPositionColor(new Vector3(length, 0, 0), Color.Red);
            // Y axis (green)
            verts[2] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Green);
            verts[3] = new VertexPositionColor(new Vector3(0, length, 0), Color.Green);
            // Z axis (blue)
            verts[4] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Blue);
            verts[5] = new VertexPositionColor(new Vector3(0, 0, length), Color.Blue);

            var indices = new short[] { 0, 1, 2, 3, 4, 5 };

            var mf = new MeshFilter();
            mf.SetGeometry(device, verts, indices, PrimitiveType.LineList);
            return mf;
        }

        /// <summary>
        /// Creates an XY plane grid (triangles), centered at origin.
        /// </summary>
        public static MeshFilter CreatePlaneGrid(GraphicsDevice device, int cols = 10, int rows = 10, float tileSize = 1f)
        {
            if (cols < 1) cols = 1;
            if (rows < 1) rows = 1;

            int vx = cols + 1;
            int vy = rows + 1;
            int vertexCount = vx * vy;
            int indexCount = cols * rows * 6;

            var verts = new VertexPositionColor[vertexCount];
            var indices = new short[indexCount];

            var hl = 0.5f;
            var w = cols * tileSize;
            var h = rows * tileSize;
            Vector2 origin = new Vector2(-w * hl, -h * hl);

            int k = 0;
            for (int y = 0; y < vy; y++)
                for (int x = 0; x < vx; x++)
                    verts[k++] = new VertexPositionColor(new Vector3(origin.X + x * tileSize, origin.Y + y * tileSize, 0f), Color.White);

            int t = 0;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    int i0 = y * vx + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + vx;
                    int i3 = i2 + 1;

                    indices[t++] = (short)i0; indices[t++] = (short)i1; indices[t++] = (short)i3;
                    indices[t++] = (short)i0; indices[t++] = (short)i3; indices[t++] = (short)i2;
                }
            }

            var mf = new MeshFilter();
            mf.SetGeometry(device, verts, indices, PrimitiveType.TriangleList);
            return mf;
        }

        /// <summary>
        /// Creates an XY wire grid (LineList), centered at origin.
        /// </summary>
        public static MeshFilter CreateWireGrid(GraphicsDevice device, int cols = 10, int rows = 10, float spacing = 1f)
        {
            if (cols < 1)
                cols = 1;
            if (rows < 1)
                rows = 1;

            int verticalLines = cols + 1;
            int horizontalLines = rows + 1;

            var verts = new VertexPositionColor[(verticalLines + horizontalLines) * 2];

            var hl = 0.5f;
            var w = cols * spacing;
            var h = rows * spacing;
            var x0 = -w * hl;
            var y0 = -h * hl;

            int k = 0;
            // verticals
            for (int c = 0; c <= cols; c++)
            {
                float x = x0 + c * spacing;
                var col = c % 5 == 0 ? Color.LightGray : Color.White; // inline, simple variation
                verts[k++] = new VertexPositionColor(new Vector3(x, y0, 0f), col);
                verts[k++] = new VertexPositionColor(new Vector3(x, y0 + h, 0f), col);
            }
            // horizontals
            for (int r = 0; r <= rows; r++)
            {
                float y = y0 + r * spacing;
                var col = r % 5 == 0 ? Color.LightGray : Color.White; // inline, simple variation
                verts[k++] = new VertexPositionColor(new Vector3(x0, y, 0f), col);
                verts[k++] = new VertexPositionColor(new Vector3(x0 + w, y, 0f), col);
            }

            var indices = new short[verts.Length];
            for (short i = 0; i < indices.Length; i++) indices[i] = i;

            var mf = new MeshFilter();
            mf.SetGeometry(device, verts, indices, PrimitiveType.LineList);
            return mf;
        }

        /// <summary>
        /// Creates a wireframe axis-aligned box (LineList), centered at origin.
        /// </summary>
        public static MeshFilter CreateWireBox(GraphicsDevice device, Vector3? size = null)
        {
            var hl = 0.5f;
            Vector3 s = size ?? Vector3.One;
            Vector3 h = s * hl;

            var p = new[]
            {
                new Vector3(-h.X, -h.Y, -h.Z), new Vector3( h.X, -h.Y, -h.Z),
                new Vector3( h.X,  h.Y, -h.Z), new Vector3(-h.X,  h.Y, -h.Z),
                new Vector3(-h.X, -h.Y,  h.Z), new Vector3( h.X, -h.Y,  h.Z),
                new Vector3( h.X,  h.Y,  h.Z), new Vector3(-h.X,  h.Y,  h.Z),
            };

            var v = new VertexPositionColor[24];
            int k = 0;

            // bottom
            v[k++] = new VertexPositionColor(p[0], Color.White); v[k++] = new VertexPositionColor(p[1], Color.White);
            v[k++] = new VertexPositionColor(p[1], Color.White); v[k++] = new VertexPositionColor(p[2], Color.White);
            v[k++] = new VertexPositionColor(p[2], Color.White); v[k++] = new VertexPositionColor(p[3], Color.White);
            v[k++] = new VertexPositionColor(p[3], Color.White); v[k++] = new VertexPositionColor(p[0], Color.White);

            // top
            v[k++] = new VertexPositionColor(p[4], Color.White); v[k++] = new VertexPositionColor(p[5], Color.White);
            v[k++] = new VertexPositionColor(p[5], Color.White); v[k++] = new VertexPositionColor(p[6], Color.White);
            v[k++] = new VertexPositionColor(p[6], Color.White); v[k++] = new VertexPositionColor(p[7], Color.White);
            v[k++] = new VertexPositionColor(p[7], Color.White); v[k++] = new VertexPositionColor(p[4], Color.White);

            // verticals
            v[k++] = new VertexPositionColor(p[0], Color.White); v[k++] = new VertexPositionColor(p[4], Color.White);
            v[k++] = new VertexPositionColor(p[1], Color.White); v[k++] = new VertexPositionColor(p[5], Color.White);
            v[k++] = new VertexPositionColor(p[2], Color.White); v[k++] = new VertexPositionColor(p[6], Color.White);
            v[k++] = new VertexPositionColor(p[3], Color.White); v[k++] = new VertexPositionColor(p[7], Color.White);

            var indices = new short[v.Length];
            for (short i = 0; i < indices.Length; i++) indices[i] = i;

            var mf = new MeshFilter();
            mf.SetGeometry(device, v, indices, PrimitiveType.LineList);
            return mf;
        }

        #endregion

        #region Solid Colored
        /// <summary>
        /// Creates a colored triangle in the XY plane centered near the origin.
        /// </summary>
        public static MeshFilter CreateTriangleColored(GraphicsDevice device)
        {
            var hl = 0.5f;

            var verts = new VertexPositionColor[3];
            verts[0] = new VertexPositionColor(new Vector3(-hl, -hl, 0f), Color.Red);
            verts[1] = new VertexPositionColor(new Vector3(hl, -hl, 0f), Color.Green);
            verts[2] = new VertexPositionColor(new Vector3(0.0f, hl, 0f), Color.Blue);

            var indices = new short[] { 0, 1, 2 };

            var mf = new MeshFilter();
            mf.SetGeometry(device, verts, indices, PrimitiveType.TriangleList);
            return mf;
        }

        /// <summary>
        /// Creates a unit quad (1x1) in the XY plane centered at the origin, with per-vertex color.
        /// </summary>
        public static MeshFilter CreateQuadColored(GraphicsDevice device)
        {
            var hl = 0.5f;

            //TODO - Wk 5
            var verts = new VertexPositionColor[4];
            verts[0] = new VertexPositionColor(
                new Vector3(-hl, -hl, 0), Color.Red); //0 - BL
            verts[1] = new VertexPositionColor(
               new Vector3(hl, -hl, 0), Color.Green);  //1- BR
            verts[2] = new VertexPositionColor(
               new Vector3(-hl, hl, 0), Color.Blue);  //2 - TL
            verts[3] = new VertexPositionColor(
               new Vector3(hl, hl, 0), Color.Yellow);  //3 - TR

            var indices = new short[] {
                2, 1, 0, //bottom triangle - winding order (LEFT HAND) clockwise
                3, 1, 2  //top triangle  - winding order (LEFT HAND) clockwise
            };

            var meshFilter = new MeshFilter();
            meshFilter.SetGeometry(device, verts, indices,
                PrimitiveType.TriangleList);
            return meshFilter;
        }

        /// <summary>
        /// Creates a solid (triangle) box centered at origin, aligned to axes.
        /// </summary>
        public static MeshFilter CreateBoxColored(GraphicsDevice device, Vector3? size = null)
        {
            Vector3 s = size ?? Vector3.One;
            Vector3 h = s * 0.5f;

            var v = new VertexPositionColor[24];

            // +Z (front)
            v[0] = new VertexPositionColor(new Vector3(-h.X, -h.Y, h.Z), Color.Red);
            v[1] = new VertexPositionColor(new Vector3(h.X, -h.Y, h.Z), Color.Red);
            v[2] = new VertexPositionColor(new Vector3(h.X, h.Y, h.Z), Color.Red);
            v[3] = new VertexPositionColor(new Vector3(-h.X, h.Y, h.Z), Color.Red);

            // -Z (back)
            v[4] = new VertexPositionColor(new Vector3(h.X, -h.Y, -h.Z), Color.Green);
            v[5] = new VertexPositionColor(new Vector3(-h.X, -h.Y, -h.Z), Color.Green);
            v[6] = new VertexPositionColor(new Vector3(-h.X, h.Y, -h.Z), Color.Green);
            v[7] = new VertexPositionColor(new Vector3(h.X, h.Y, -h.Z), Color.Green);

            // +X (right)
            v[8] = new VertexPositionColor(new Vector3(h.X, -h.Y, h.Z), Color.Blue);
            v[9] = new VertexPositionColor(new Vector3(h.X, -h.Y, -h.Z), Color.Blue);
            v[10] = new VertexPositionColor(new Vector3(h.X, h.Y, -h.Z), Color.Blue);
            v[11] = new VertexPositionColor(new Vector3(h.X, h.Y, h.Z), Color.Blue);

            // -X (left)
            v[12] = new VertexPositionColor(new Vector3(-h.X, -h.Y, -h.Z), Color.Yellow);
            v[13] = new VertexPositionColor(new Vector3(-h.X, -h.Y, h.Z), Color.Yellow);
            v[14] = new VertexPositionColor(new Vector3(-h.X, h.Y, h.Z), Color.Yellow);
            v[15] = new VertexPositionColor(new Vector3(-h.X, h.Y, -h.Z), Color.Yellow);

            // +Y (top)
            v[16] = new VertexPositionColor(new Vector3(-h.X, h.Y, h.Z), Color.Cyan);
            v[17] = new VertexPositionColor(new Vector3(h.X, h.Y, h.Z), Color.Cyan);
            v[18] = new VertexPositionColor(new Vector3(h.X, h.Y, -h.Z), Color.Cyan);
            v[19] = new VertexPositionColor(new Vector3(-h.X, h.Y, -h.Z), Color.Cyan);

            // -Y (bottom)
            v[20] = new VertexPositionColor(new Vector3(-h.X, -h.Y, -h.Z), Color.Magenta);
            v[21] = new VertexPositionColor(new Vector3(h.X, -h.Y, -h.Z), Color.Magenta);
            v[22] = new VertexPositionColor(new Vector3(h.X, -h.Y, h.Z), Color.Magenta);
            v[23] = new VertexPositionColor(new Vector3(-h.X, -h.Y, h.Z), Color.Magenta);

            short[] i =
            {
                0,1,2,  0,2,3,     // front
                4,5,6,  4,6,7,     // back
                8,9,10, 8,10,11,   // right
                12,13,14, 12,14,15,// left
                16,17,18, 16,18,19,// top
                20,21,22, 20,22,23 // bottom
            };

            var mf = new MeshFilter();
            mf.SetGeometry(device, v, i, PrimitiveType.TriangleList);
            return mf;
        }
        #endregion

        #region Solid Textured
        /// <summary>
        /// Creates a unit textured quad (1x1) on the XY plane centered at the origin.
        /// Unlit variant: POSITION + TEXCOORD only.
        /// </summary>
        public static MeshFilter CreateQuadTextured(GraphicsDevice device)
        {
            var halfLength = 0.5f;

            var verts = new VertexPositionTexture[4];
            // 0-BL, 1-BR, 2-TL, 3-TR  (matches CreateQuadColored)
            verts[0] = new VertexPositionTexture(new Vector3(-halfLength, -halfLength, 0f), new Vector2(0f, 1f)); // BL
            verts[1] = new VertexPositionTexture(new Vector3(halfLength, -halfLength, 0f), new Vector2(1f, 1f)); // BR
            verts[2] = new VertexPositionTexture(new Vector3(-halfLength, halfLength, 0f), new Vector2(0f, 0f)); // TL
            verts[3] = new VertexPositionTexture(new Vector3(halfLength, halfLength, 0f), new Vector2(1f, 0f)); // TR

            // Clockwise (left-hand) to match CreateQuadColored
            var indices = new short[] {
                            2, 1, 0,   // bottom tri
                            3, 1, 2    // top tri
                        };

            var mf = new MeshFilter();
            mf.SetGeometry(device, verts, indices, PrimitiveType.TriangleList);
            return mf;
        }

        /// <summary>
        /// Creates a textured 1x1x1 cube centered at the origin.
        /// (VertexPositionTexture). UVs are (0,1) BL, (1,1) BR, (1,0) TR, (0,0) TL per face.
        /// </summary>
        public static MeshFilter CreateCubeTextured(GraphicsDevice device)
        {
            float halfLength = 0.5f;

            // 24 unique vertices (4 per face) so each face can have its own UVs.
            var v = new VertexPositionTexture[24];

            // UVs (BL, BR, TR, TL)
            Vector2 uvBL = new Vector2(0f, 1f);
            Vector2 uvBR = new Vector2(1f, 1f);
            Vector2 uvTR = new Vector2(1f, 0f);
            Vector2 uvTL = new Vector2(0f, 0f);

            int k = 0;

            // +Z (front)
            v[k++] = new VertexPositionTexture(new Vector3(-halfLength, -halfLength, halfLength), uvBL); // 0
            v[k++] = new VertexPositionTexture(new Vector3(halfLength, -halfLength, halfLength), uvBR); // 1
            v[k++] = new VertexPositionTexture(new Vector3(halfLength, halfLength, halfLength), uvTR); // 2
            v[k++] = new VertexPositionTexture(new Vector3(-halfLength, halfLength, halfLength), uvTL); // 3

            // -Z (back)
            v[k++] = new VertexPositionTexture(new Vector3(halfLength, -halfLength, -halfLength), uvBR); // 4
            v[k++] = new VertexPositionTexture(new Vector3(-halfLength, -halfLength, -halfLength), uvBL); // 5
            v[k++] = new VertexPositionTexture(new Vector3(-halfLength, halfLength, -halfLength), uvTL); // 6
            v[k++] = new VertexPositionTexture(new Vector3(halfLength, halfLength, -halfLength), uvTR); // 7

            // +X (right)
            v[k++] = new VertexPositionTexture(new Vector3(halfLength, -halfLength, halfLength), uvBL); // 8
            v[k++] = new VertexPositionTexture(new Vector3(halfLength, -halfLength, -halfLength), uvBR); // 9
            v[k++] = new VertexPositionTexture(new Vector3(halfLength, halfLength, -halfLength), uvTR); //10
            v[k++] = new VertexPositionTexture(new Vector3(halfLength, halfLength, halfLength), uvTL); //11

            // -X (left)
            v[k++] = new VertexPositionTexture(new Vector3(-halfLength, -halfLength, -halfLength), uvBR); //12
            v[k++] = new VertexPositionTexture(new Vector3(-halfLength, -halfLength, halfLength), uvBL); //13
            v[k++] = new VertexPositionTexture(new Vector3(-halfLength, halfLength, halfLength), uvTL); //14
            v[k++] = new VertexPositionTexture(new Vector3(-halfLength, halfLength, -halfLength), uvTR); //15

            // +Y (top)
            v[k++] = new VertexPositionTexture(new Vector3(-halfLength, halfLength, halfLength), uvBL); //16
            v[k++] = new VertexPositionTexture(new Vector3(halfLength, halfLength, halfLength), uvBR); //17
            v[k++] = new VertexPositionTexture(new Vector3(halfLength, halfLength, -halfLength), uvTR); //18
            v[k++] = new VertexPositionTexture(new Vector3(-halfLength, halfLength, -halfLength), uvTL); //19

            // -Y (bottom)
            v[k++] = new VertexPositionTexture(new Vector3(-halfLength, -halfLength, -halfLength), uvTL); //20
            v[k++] = new VertexPositionTexture(new Vector3(halfLength, -halfLength, -halfLength), uvTR); //21
            v[k++] = new VertexPositionTexture(new Vector3(halfLength, -halfLength, halfLength), uvBR); //22
            v[k++] = new VertexPositionTexture(new Vector3(-halfLength, -halfLength, halfLength), uvBL); //23

            // Indices with CW winding (visible outside with CullCounterClockwiseFace)
            short[] iArr =
            {
                // front  (+Z)
                0,2,1,  0,3,2,
                // back   (-Z)
                4,6,5,  4,7,6,
                // right  (+X)
                8,10,9, 8,11,10,
                // left   (-X)
                12,14,13, 12,15,14,
                // top    (+Y)
                16,18,17, 16,19,18,
                // bottom (-Y)
                20,22,21, 20,23,22
            };

            var mf = new MeshFilter();
            mf.SetGeometry(device, v, iArr, PrimitiveType.TriangleList);
            return mf;
        }


        #endregion

        #endregion

        #region Lit

        #region Solid Textured
        /// <summary>
        /// Creates a unit textured quad (1x1) on the XY plane centered at the origin,
        /// with normals pointing +Z for basic lighting (e.g., BasicEffect LightingEnabled=true).
        /// </summary>
        public static MeshFilter CreateQuadTexturedLit(GraphicsDevice device)
        {
            //TODO - can we get the name of the current method?
            if (registry.ContainsKey("CreateQuadTexturedLit"))
                return registry["CreateQuadTexturedLit"];

            var halfLength = 0.5f;
            var normal = new Vector3(0f, 0f, 1f);

            var verts = new VertexPositionNormalTexture[4];
            // 0-BL, 1-BR, 2-TL, 3-TR (same order as colored/textured)
            verts[0] = new VertexPositionNormalTexture(new Vector3(-halfLength, -halfLength, 0f), normal, new Vector2(0f, 1f)); // BL
            verts[1] = new VertexPositionNormalTexture(new Vector3(halfLength, -halfLength, 0f), normal, new Vector2(1f, 1f)); // BR
            verts[2] = new VertexPositionNormalTexture(new Vector3(-halfLength, halfLength, 0f), normal, new Vector2(0f, 0f)); // TL
            verts[3] = new VertexPositionNormalTexture(new Vector3(halfLength, halfLength, 0f), normal, new Vector2(1f, 0f)); // TR

            var indices = new short[] {
                            2, 1, 0,
                            3, 1, 2
                        };

            var mf = new MeshFilter();
            mf.SetGeometry(device, verts, indices, PrimitiveType.TriangleList);

            registry.Add("CreateQuadTexturedLit", mf);

            return mf;
        }

        /// <summary>
        /// Creates a subdivided textured quad on the XY plane centered at the origin.
        /// Lets you:
        /// 1) Control the number of segments across width/height.
        /// 2) Control how many times the UVs tile across the whole quad.
        ///
        /// Useful for large ground planes (e.g. grass) where you want repeated detail.
        /// </summary>
        /// <param name="device">Graphics device for buffer creation.</param>
        /// <param name="widthSegments">Number of segments along the X axis (minimum 1).</param>
        /// <param name="heightSegments">Number of segments along the Y axis (minimum 1).</param>
        /// <param name="width">World-space width of the quad (in X).</param>
        /// <param name="height">World-space height of the quad (in Y).</param>
        /// <param name="uvTilesX">How many times the texture repeats across X.</param>
        /// <param name="uvTilesY">How many times the texture repeats across Y.</param>
        public static MeshFilter CreateQuadGridTexturedLit(
            GraphicsDevice device,
            int widthSegments,
            int heightSegments,
            float width,
            float height,
            float uvTilesX,
            float uvTilesY)
        {
            if (widthSegments < 1)
                widthSegments = 1;
            if (heightSegments < 1)
                heightSegments = 1;

            int vx = widthSegments + 1;
            int vy = heightSegments + 1;
            int vertexCount = vx * vy;
            int indexCount = widthSegments * heightSegments * 6;

            var verts = new VertexPositionNormalTexture[vertexCount];
            var indices = new short[indexCount];

            // Centered at origin on XY plane
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            Vector2 origin = new Vector2(-halfWidth, -halfHeight);

            float stepX = width / widthSegments;
            float stepY = height / heightSegments;

            var normal = new Vector3(0f, 0f, 1f);

            int k = 0;
            for (int y = 0; y < vy; y++)
            {
                float fy = origin.Y + y * stepY;
                float v = (1f - (y / (float)heightSegments)) * uvTilesY; // bottom -> top : uvTilesY -> 0

                for (int x = 0; x < vx; x++)
                {
                    float fx = origin.X + x * stepX;
                    float u = (x / (float)widthSegments) * uvTilesX;      // left -> right : 0 -> uvTilesX

                    verts[k++] = new VertexPositionNormalTexture(
                        new Vector3(fx, fy, 0f),
                        normal,
                        new Vector2(u, v));
                }
            }

            int t = 0;
            for (int y = 0; y < heightSegments; y++)
            {
                for (int x = 0; x < widthSegments; x++)
                {
                    int i0 = y * vx + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + vx;
                    int i3 = i2 + 1;

                    // Same CW winding as the other quads (left-handed)
                    indices[t++] = (short)i2; // TL
                    indices[t++] = (short)i1; // BR
                    indices[t++] = (short)i0; // BL

                    indices[t++] = (short)i3; // TR
                    indices[t++] = (short)i1; // BR
                    indices[t++] = (short)i2; // TL
                }
            }

            var mf = new MeshFilter();
            mf.SetGeometry(device, verts, indices, PrimitiveType.TriangleList);
            return mf;
        }

        /// <summary>
        /// Creates a subdivided textured quad on the XY plane centered at the origin.
        /// Unlit version: positions + UVs only (VertexPositionTexture).
        /// Lets you:
        /// 1) Control the number of segments across width/height.
        /// 2) Control how many times the UVs tile across the whole quad.
        ///
        /// Ideal for large ground planes (e.g. grass) using an unlit shader.
        /// </summary>
        /// <param name="device">Graphics device for buffer creation.</param>
        /// <param name="widthSegments">Number of segments along the X axis (minimum 1).</param>
        /// <param name="heightSegments">Number of segments along the Y axis (minimum 1).</param>
        /// <param name="width">World-space width of the quad (in X).</param>
        /// <param name="height">World-space height of the quad (in Y).</param>
        /// <param name="uvTilesX">How many times the texture repeats across X.</param>
        /// <param name="uvTilesY">How many times the texture repeats across Y.</param>
        public static MeshFilter CreateQuadGridTexturedUnlit(
            GraphicsDevice device,
            int widthSegments,
            int heightSegments,
            float width,
            float height,
            float uvTilesX,
            float uvTilesY)
        {
            if (widthSegments < 1)
                widthSegments = 1;
            if (heightSegments < 1)
                heightSegments = 1;

            int vx = widthSegments + 1;
            int vy = heightSegments + 1;
            int vertexCount = vx * vy;
            int indexCount = widthSegments * heightSegments * 6;

            var verts = new VertexPositionTexture[vertexCount];
            var indices = new short[indexCount];

            // Centered at origin on XY plane
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            Vector2 origin = new Vector2(-halfWidth, -halfHeight);

            float stepX = width / widthSegments;
            float stepY = height / heightSegments;

            int k = 0;
            for (int y = 0; y < vy; y++)
            {
                float fy = origin.Y + y * stepY;
                float v = (1f - (y / (float)heightSegments)) * uvTilesY; // bottom -> top : uvTilesY -> 0

                for (int x = 0; x < vx; x++)
                {
                    float fx = origin.X + x * stepX;
                    float u = (x / (float)widthSegments) * uvTilesX;      // left -> right : 0 -> uvTilesX

                    verts[k++] = new VertexPositionTexture(
                        new Vector3(fx, fy, 0f),
                        new Vector2(u, v));
                }
            }

            int t = 0;
            for (int y = 0; y < heightSegments; y++)
            {
                for (int x = 0; x < widthSegments; x++)
                {
                    int i0 = y * vx + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + vx;
                    int i3 = i2 + 1;

                    // Same CW winding as the other quads (left-handed)
                    indices[t++] = (short)i2; // TL
                    indices[t++] = (short)i1; // BR
                    indices[t++] = (short)i0; // BL

                    indices[t++] = (short)i3; // TR
                    indices[t++] = (short)i1; // BR
                    indices[t++] = (short)i2; // TL
                }
            }

            var mf = new MeshFilter();
            mf.SetGeometry(device, verts, indices, PrimitiveType.TriangleList);
            return mf;
        }


        /// <summary>
        /// Creates a textured 1x1x1 cube centered at the origin, with per-face normals
        /// (VertexPositionNormalTexture). UVs are (0,1) BL, (1,1) BR, (1,0) TR, (0,0) TL per face.
        /// </summary>
        public static MeshFilter CreateCubeTexturedLit(GraphicsDevice device)
        {
            float halfLength = 0.5f;

            // 24 unique verts (4 per face) so each face has its own normal + UVs
            var v = new VertexPositionNormalTexture[24];

            // UVs (BL, BR, TR, TL)
            Vector2 uvBL = new Vector2(0f, 1f);
            Vector2 uvBR = new Vector2(1f, 1f);
            Vector2 uvTR = new Vector2(1f, 0f);
            Vector2 uvTL = new Vector2(0f, 0f);

            int k = 0;

            // +Z (front)
            Vector3 nF = new Vector3(0, 0, 1);
            v[k++] = new VertexPositionNormalTexture(new Vector3(-halfLength, -halfLength, halfLength), nF, uvBL); // 0
            v[k++] = new VertexPositionNormalTexture(new Vector3(halfLength, -halfLength, halfLength), nF, uvBR); // 1
            v[k++] = new VertexPositionNormalTexture(new Vector3(halfLength, halfLength, halfLength), nF, uvTR); // 2
            v[k++] = new VertexPositionNormalTexture(new Vector3(-halfLength, halfLength, halfLength), nF, uvTL); // 3

            // -Z (back)
            Vector3 nB = new Vector3(0, 0, -1);
            v[k++] = new VertexPositionNormalTexture(new Vector3(halfLength, -halfLength, -halfLength), nB, uvBR); // 4
            v[k++] = new VertexPositionNormalTexture(new Vector3(-halfLength, -halfLength, -halfLength), nB, uvBL); // 5
            v[k++] = new VertexPositionNormalTexture(new Vector3(-halfLength, halfLength, -halfLength), nB, uvTL); // 6
            v[k++] = new VertexPositionNormalTexture(new Vector3(halfLength, halfLength, -halfLength), nB, uvTR); // 7

            // +X (right)
            Vector3 nR = new Vector3(1, 0, 0);
            v[k++] = new VertexPositionNormalTexture(new Vector3(halfLength, -halfLength, halfLength), nR, uvBL); // 8
            v[k++] = new VertexPositionNormalTexture(new Vector3(halfLength, -halfLength, -halfLength), nR, uvBR); // 9
            v[k++] = new VertexPositionNormalTexture(new Vector3(halfLength, halfLength, -halfLength), nR, uvTR); // 10
            v[k++] = new VertexPositionNormalTexture(new Vector3(halfLength, halfLength, halfLength), nR, uvTL); // 11

            // -X (left)
            Vector3 nL = new Vector3(-1, 0, 0);
            v[k++] = new VertexPositionNormalTexture(new Vector3(-halfLength, -halfLength, -halfLength), nL, uvBR); // 12
            v[k++] = new VertexPositionNormalTexture(new Vector3(-halfLength, -halfLength, halfLength), nL, uvBL); // 13
            v[k++] = new VertexPositionNormalTexture(new Vector3(-halfLength, halfLength, halfLength), nL, uvTL); // 14
            v[k++] = new VertexPositionNormalTexture(new Vector3(-halfLength, halfLength, -halfLength), nL, uvTR); // 15

            // +Y (top)
            Vector3 nT = new Vector3(0, 1, 0);
            v[k++] = new VertexPositionNormalTexture(new Vector3(-halfLength, halfLength, halfLength), nT, uvBL); // 16
            v[k++] = new VertexPositionNormalTexture(new Vector3(halfLength, halfLength, halfLength), nT, uvBR); // 17
            v[k++] = new VertexPositionNormalTexture(new Vector3(halfLength, halfLength, -halfLength), nT, uvTR); // 18
            v[k++] = new VertexPositionNormalTexture(new Vector3(-halfLength, halfLength, -halfLength), nT, uvTL); // 19

            // -Y (bottom)
            Vector3 nD = new Vector3(0, -1, 0);
            v[k++] = new VertexPositionNormalTexture(new Vector3(-halfLength, -halfLength, -halfLength), nD, uvTL); // 20
            v[k++] = new VertexPositionNormalTexture(new Vector3(halfLength, -halfLength, -halfLength), nD, uvTR); // 21
            v[k++] = new VertexPositionNormalTexture(new Vector3(halfLength, -halfLength, halfLength), nD, uvBR); // 22
            v[k++] = new VertexPositionNormalTexture(new Vector3(-halfLength, -halfLength, halfLength), nD, uvBL); // 23

            // Indices with CW winding (visible outside with CullCounterClockwiseFace)
            short[] iArr =
            {
                // front  (+Z)
                0,2,1,  0,3,2,
                // back   (-Z)
                4,6,5,  4,7,6,
                // right  (+X)
                8,10,9, 8,11,10,
                // left   (-X)
                12,14,13, 12,15,14,
                // top    (+Y)
                16,18,17, 16,19,18,
                // bottom (-Y)
                20,22,21, 20,23,22
            };

            var mf = new MeshFilter();
            mf.SetGeometry(device, v, iArr, PrimitiveType.TriangleList);
            return mf;
        }


        #endregion

        #region Solid Colored
        /// <summary>
        /// Loads a compiled FBX content asset (MonoGame <see cref="Model"/>) and extracts one mesh part into a new <see cref="MeshFilter"/>.
        /// Copies vertex and index data into fresh GPU buffers owned by the returned <see cref="MeshFilter"/>.
        /// </summary>
        /// <param name="content">Content manager used to load the compiled model (e.g., "Models/Cube").</param>
        /// <param name="device">Graphics device for buffer creation.</param>
        /// <param name="assetName">Content pipeline asset name (without extension).</param>
        /// <param name="meshIndex">Which ModelMesh to use (default 0).</param>
        /// <param name="partIndex">Which ModelMeshPart to use within the mesh (default 0).</param>
        public static MeshFilter CreateFromModel(ContentManager content,
                                              GraphicsDevice device,
                                              string assetName,
                                              int meshIndex = 0,
                                              int partIndex = 0)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (string.IsNullOrWhiteSpace(assetName))
                throw new ArgumentException("Asset name must be non-empty.", nameof(assetName));

            // Load model and select mesh/part
            var model = content.Load<Model>(assetName);
            if (meshIndex < 0 || meshIndex >= model.Meshes.Count)
                throw new ArgumentOutOfRangeException(nameof(meshIndex), "meshIndex outside range of Model.Meshes.");

            var mesh = model.Meshes[meshIndex];
            if (partIndex < 0 || partIndex >= mesh.MeshParts.Count)
                throw new ArgumentOutOfRangeException(nameof(partIndex), "partIndex outside range of ModelMesh.MeshParts.");

            var part = mesh.MeshParts[partIndex];

            // Vertex copy (slice the part's vertex range into a standalone buffer)
            var vertexDecl = part.VertexBuffer.VertexDeclaration;
            int vertexStride = vertexDecl.VertexStride;
            int vertexCount = part.NumVertices;
            int vertexOffsetBytes = part.VertexOffset * vertexStride;

            var vertexBytes = new byte[vertexStride * vertexCount];
            part.VertexBuffer.GetData(vertexOffsetBytes, vertexBytes, 0, vertexBytes.Length, vertexStride);

            var vb = new VertexBuffer(device, vertexDecl, vertexCount, BufferUsage.WriteOnly);
            vb.SetData(vertexBytes);

            // Index copy (slice indices for this part and rebase to zero because we won't use baseVertex)
            int indexCount = part.PrimitiveCount * 3;
            int indexStartByte = part.StartIndex * (part.IndexBuffer.IndexElementSize == IndexElementSize.SixteenBits ? 2 : 4);

            IndexBuffer ib;
            if (part.IndexBuffer.IndexElementSize == IndexElementSize.SixteenBits)
            {
                var src = new ushort[indexCount];
                part.IndexBuffer.GetData(indexStartByte, src, 0, indexCount);

                // Rebase to local vertex 0
                short[] dst = new short[indexCount];
                for (int i = 0; i < indexCount; i++)
                    dst[i] = (short)(src[i] - part.VertexOffset);

                ib = new IndexBuffer(device, IndexElementSize.SixteenBits, indexCount, BufferUsage.WriteOnly);
                ib.SetData(dst);
            }
            else
            {
                var src = new int[indexCount];
                part.IndexBuffer.GetData(indexStartByte, src, 0, indexCount);

                // Rebase to local vertex 0
                for (int i = 0; i < indexCount; i++)
                    src[i] = src[i] - part.VertexOffset;

                ib = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, indexCount, BufferUsage.WriteOnly);
                ib.SetData(src);
            }

            // Package into MeshFilter
            var mf = new MeshFilter();
            mf.SetGeometry(vb, ib, PrimitiveType.TriangleList, indexCount);
            return mf;
        }

        /// <summary>
        /// Returns a list of MeshFilters, one for each part of a model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public static List<MeshFilter> CreateAllFromModel(Model model, GraphicsDevice device)
        {
            var result = new List<MeshFilter>();

            for (int mi = 0; mi < model.Meshes.Count; mi++)
            {
                var mesh = model.Meshes[mi];
                for (int pi = 0; pi < mesh.MeshParts.Count; pi++)
                {
                    result.Add(CreateFromModel(model, device, mi, pi));
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a single <see cref="MeshFilter"/> from all meshes/parts in a <see cref="Model"/>.
        /// Concatenates all vertex/index data into one VB/IB,
        /// rebasing indices appropriately.
        /// </summary>
        public static MeshFilter CreateFromMultiMeshModel(
            Model model,
            GraphicsDevice device)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            // Collect all parts
            var parts = new List<ModelMeshPart>();
            foreach (var mesh in model.Meshes)
            {
                foreach (var part in mesh.MeshParts)
                    parts.Add(part);
            }

            if (parts.Count == 0)
                throw new InvalidOperationException("Model contains no mesh parts.");

            // Assume all parts share the same vertex declaration (typical for FBX → MonoGame pipeline).
            var vertexDecl = parts[0].VertexBuffer.VertexDeclaration;
            int vertexStride = vertexDecl.VertexStride;

            // Compute totals
            int totalVertexCount = 0;
            int totalIndexCount = 0;
            bool require32BitIndices = false;

            foreach (var part in parts)
            {
                totalVertexCount += part.NumVertices;
                totalIndexCount += part.PrimitiveCount * 3;

                if (part.IndexBuffer.IndexElementSize == IndexElementSize.ThirtyTwoBits)
                    require32BitIndices = true;
            }

            // If we would overflow 16-bit indices, force 32-bit
            if (totalVertexCount > ushort.MaxValue)
                require32BitIndices = true;

            // Combined vertex buffer
            int totalVertexBytes = totalVertexCount * vertexStride;
            var combinedVertexBytes = new byte[totalVertexBytes];

            int vertexBase = 0;              // in vertices
            int vertexBaseBytes = 0;         // in bytes

            foreach (var part in parts)
            {
                int partVertexCount = part.NumVertices;
                int partVertexOffsetBytes = part.VertexOffset * vertexStride;
                int partVertexBytes = partVertexCount * vertexStride;

                var temp = new byte[partVertexBytes];

                // Copy raw bytes for this slice
                part.VertexBuffer.GetData(
                    offsetInBytes: partVertexOffsetBytes,
                    data: temp,
                    startIndex: 0,
                    elementCount: partVertexBytes);

                Buffer.BlockCopy(
                    src: temp,
                    srcOffset: 0,
                    dst: combinedVertexBytes,
                    dstOffset: vertexBaseBytes,
                    count: partVertexBytes);

                vertexBase += partVertexCount;
                vertexBaseBytes += partVertexBytes;
            }

            var vb = new VertexBuffer(device, vertexDecl, totalVertexCount, BufferUsage.WriteOnly);
            vb.SetData(combinedVertexBytes);

            // Combined index buffer
            IndexBuffer ib;

            if (!require32BitIndices)
            {
                var combinedIndices = new short[totalIndexCount];

                int indexCursor = 0;
                int vertexBaseForPart = 0;

                foreach (var part in parts)
                {
                    int partIndexCount = part.PrimitiveCount * 3;
                    bool partSixteenBit = part.IndexBuffer.IndexElementSize == IndexElementSize.SixteenBits;
                    int indexStartByte = part.StartIndex * (partSixteenBit ? 2 : 4);

                    if (partSixteenBit)
                    {
                        var src = new ushort[partIndexCount];
                        part.IndexBuffer.GetData(indexStartByte, src, 0, partIndexCount);

                        for (int i = 0; i < partIndexCount; i++)
                        {
                            // Rebase relative to the part's VertexOffset (as in CreateFromMeshPart)
                            int local = src[i] - part.VertexOffset;
                            combinedIndices[indexCursor++] = (short)(vertexBaseForPart + local);
                        }
                    }
                    else
                    {
                        var src = new int[partIndexCount];
                        part.IndexBuffer.GetData(indexStartByte, src, 0, partIndexCount);

                        for (int i = 0; i < partIndexCount; i++)
                        {
                            int local = src[i] - part.VertexOffset;
                            combinedIndices[indexCursor++] = (short)(vertexBaseForPart + local);
                        }
                    }

                    vertexBaseForPart += part.NumVertices;
                }

                ib = new IndexBuffer(device, IndexElementSize.SixteenBits, totalIndexCount, BufferUsage.WriteOnly);
                ib.SetData(combinedIndices);
            }
            else
            {
                var combinedIndices = new int[totalIndexCount];

                int indexCursor = 0;
                int vertexBaseForPart = 0;

                foreach (var part in parts)
                {
                    int partIndexCount = part.PrimitiveCount * 3;
                    bool partSixteenBit = part.IndexBuffer.IndexElementSize == IndexElementSize.SixteenBits;
                    int indexStartByte = part.StartIndex * (partSixteenBit ? 2 : 4);

                    if (partSixteenBit)
                    {
                        var src = new ushort[partIndexCount];
                        part.IndexBuffer.GetData(indexStartByte, src, 0, partIndexCount);

                        for (int i = 0; i < partIndexCount; i++)
                        {
                            int local = src[i] - part.VertexOffset;
                            combinedIndices[indexCursor++] = vertexBaseForPart + local;
                        }
                    }
                    else
                    {
                        var src = new int[partIndexCount];
                        part.IndexBuffer.GetData(indexStartByte, src, 0, partIndexCount);

                        for (int i = 0; i < partIndexCount; i++)
                        {
                            int local = src[i] - part.VertexOffset;
                            combinedIndices[indexCursor++] = vertexBaseForPart + local;
                        }
                    }

                    vertexBaseForPart += part.NumVertices;
                }

                ib = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, totalIndexCount, BufferUsage.WriteOnly);
                ib.SetData(combinedIndices);
            }

            // Package into MeshFilter
            var mf = new MeshFilter();
            mf.SetGeometry(vb, ib, PrimitiveType.TriangleList, totalIndexCount);
            return mf;
        }


        /// <summary>
        /// Extracts a single <see cref="ModelMeshPart"/> from an already-loaded
        /// MonoGame <see cref="Model"/> and returns a new <see cref="MeshFilter"/>
        /// with standalone GPU buffers (no dependency on the original Model).
        /// </summary>
        /// <param name="model">An already-loaded Model (e.g., from the dictionary).</param>
        /// <param name="device">Graphics device for buffer creation.</param>
        /// <param name="meshIndex">Which ModelMesh to use (default 0).</param>
        /// <param name="partIndex">Which ModelMeshPart within that mesh (default 0).</param>
        public static MeshFilter CreateFromModel(Model model,
                                                 GraphicsDevice device,
                                                 int meshIndex = 0,
                                                 int partIndex = 0)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            if (meshIndex < 0 || meshIndex >= model.Meshes.Count)
                throw new ArgumentOutOfRangeException(nameof(meshIndex), "meshIndex outside range of Model.Meshes.");

            var mesh = model.Meshes[meshIndex];
            if (partIndex < 0 || partIndex >= mesh.MeshParts.Count)
                throw new ArgumentOutOfRangeException(nameof(partIndex), "partIndex outside range of ModelMesh.MeshParts.");

            var part = mesh.MeshParts[partIndex];
            return CreateFromMeshPart(device, part);
        }

        /// <summary>
        /// Core worker: copies one <see cref="ModelMeshPart"/> into fresh VB/IB,
        /// rebasing indices to start at 0 (since we won't use baseVertex when drawing).
        /// </summary>
        public static MeshFilter CreateFromMeshPart(GraphicsDevice device, ModelMeshPart part)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (part == null)
                throw new ArgumentNullException(nameof(part));

            // --- Vertex slice (copy raw bytes) ---
            var vertexDecl = part.VertexBuffer.VertexDeclaration;
            int vertexStride = vertexDecl.VertexStride;
            int vertexCount = part.NumVertices;
            int vertexOffsetBytes = part.VertexOffset * vertexStride;

            int totalVertexBytes = vertexStride * vertexCount;
            var vertexBytes = new byte[totalVertexBytes];

            // IMPORTANT: use the overload WITHOUT 'vertexStride' when copying to byte[]
            // elementCount == number of T elements (bytes, here), not vertices
            part.VertexBuffer.GetData(
                offsetInBytes: vertexOffsetBytes,
                data: vertexBytes,
                startIndex: 0,
                elementCount: totalVertexBytes
            );

            var vb = new VertexBuffer(device, vertexDecl, vertexCount, BufferUsage.WriteOnly);
            vb.SetData(vertexBytes);

            // --- Index slice (copy + rebase to 0) ---
            int indexCount = part.PrimitiveCount * 3;
            bool sixteenBit = part.IndexBuffer.IndexElementSize == IndexElementSize.SixteenBits;
            int indexStartByte = part.StartIndex * (sixteenBit ? 2 : 4);

            IndexBuffer ib;
            if (sixteenBit)
            {
                var src = new ushort[indexCount];
                part.IndexBuffer.GetData(indexStartByte, src, 0, indexCount);

                // Rebase relative to VertexOffset because we won't draw with a baseVertex
                var dst = new short[indexCount];
                for (int i = 0; i < indexCount; i++)
                    dst[i] = (short)(src[i] - part.VertexOffset);

                ib = new IndexBuffer(device, IndexElementSize.SixteenBits, indexCount, BufferUsage.WriteOnly);
                ib.SetData(dst);
            }
            else
            {
                var src = new int[indexCount];
                part.IndexBuffer.GetData(indexStartByte, src, 0, indexCount);

                for (int i = 0; i < indexCount; i++)
                    src[i] = src[i] - part.VertexOffset;

                ib = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, indexCount, BufferUsage.WriteOnly);
                ib.SetData(src);
            }

            // --- Package into MeshFilter ---
            var mf = new MeshFilter();
            mf.SetGeometry(vb, ib, PrimitiveType.TriangleList, indexCount);
            return mf;
        }


        #endregion

        #endregion

        #region Housekeeping Methods
        /// <summary>
        /// Disposes all cached MeshFilters in the registry and clears it.
        /// Call this when shutting down the application or changing scenes.
        /// </summary>
        public static void ClearRegistry()
        {
            foreach (var kvp in registry)
            {
                kvp.Value?.Dispose();
            }
            registry.Clear();
        }
        #endregion
    }
}
