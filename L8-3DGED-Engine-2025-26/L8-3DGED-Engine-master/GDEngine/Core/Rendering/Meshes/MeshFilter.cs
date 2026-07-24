using GDEngine.Core.Components;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Holds mesh geometry buffers (vertex/index) and draw topology for a <see cref="GameObject"/>.
    /// Attach alongside a MeshRenderer to render (e.g., with <see cref="BasicEffect"/>).
    /// </summary>
    /// <see cref="MeshRenderer"/>
    /// <see cref="GameObject"/>
    public sealed class MeshFilter : Component, IDisposable
    {
        #region Fields
        private VertexBuffer? _vertexBuffer;
        private IndexBuffer? _indexBuffer;
        private PrimitiveType _primitiveType;
        private int _primitiveCount;
        private int _vertexCount;
        private int _indexCount;
        private bool _disposed = false;

        #endregion

        #region Properties
        public VertexBuffer? VertexBuffer => _vertexBuffer;
        public IndexBuffer? IndexBuffer => _indexBuffer;
        public PrimitiveType PrimitiveType => _primitiveType;
        public int PrimitiveCount => _primitiveCount;
        public int VertexCount => _vertexCount;
        public int IndexCount => _indexCount;
        #endregion

        #region Core Methods
        /// <summary>
        /// Assigns vertex/index buffers from raw arrays (generic vertex type).
        /// </summary>
        public void SetGeometry<T>(GraphicsDevice device,
                                   T[] vertices,
                                   short[] indices,
                                   PrimitiveType primitiveType)
            where T : struct, IVertexType
        {
            //TODO - Wk5 
            _vertexCount = vertices.Length;
            _indexCount = indices.Length;
            _primitiveType = primitiveType;
            _primitiveCount = CalculatePrimitiveCount(_indexCount, primitiveType);

            //make reservation on GFX card in VRAM
            _vertexBuffer = new VertexBuffer(device,
                typeof(T), _vertexCount, BufferUsage.WriteOnly);
            //move vertices to VRAM
            _vertexBuffer.SetData(vertices);

            //make reservation on GFX card in VRAM
            _indexBuffer = new IndexBuffer(device,
                IndexElementSize.SixteenBits, _indexCount,
                BufferUsage.WriteOnly);
            //move indiced to VRAM
            _indexBuffer.SetData(indices);
        }

        /// <summary>
        /// Assigns pre-built buffers to this filter.
        /// </summary>
        public void SetGeometry(VertexBuffer vb, IndexBuffer ib, PrimitiveType primitiveType, int indexCount)
        {
            _vertexBuffer = vb;
            _indexBuffer = ib;
            _primitiveType = primitiveType;
            _indexCount = indexCount;
            _vertexCount = vb.VertexCount;
            _primitiveCount = CalculatePrimitiveCount(_indexCount, _primitiveType);
        }

        private static int CalculatePrimitiveCount(int indexCount, PrimitiveType type)
        {
            if (type == PrimitiveType.TriangleList)
                return indexCount / 3;
            if (type == PrimitiveType.TriangleStrip)
                return indexCount - 2 < 0 ? 0 : indexCount - 2;
            if (type == PrimitiveType.LineList)
                return indexCount / 2;
            if (type == PrimitiveType.LineStrip)
                return indexCount - 1 < 0 ? 0 : indexCount - 1;
            return 0;
        }

        public void BindBuffers(GraphicsDevice device)
        {
            device.SetVertexBuffer(_vertexBuffer);
            device.Indices = _indexBuffer;
        }

        #endregion

        #region Housekeeping Methods
  
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources (GPU buffers)
                _vertexBuffer?.Dispose();
                _vertexBuffer = null;

                _indexBuffer?.Dispose();
                _indexBuffer = null;
            }

            _disposed = true;
        }

        ~MeshFilter()
        {
            Dispose(false);
        }

        // Override OnDestroy to ensure disposal when component is destroyed
        protected override void OnDestroy()
        {
            Dispose();
            base.OnDestroy();
        }
        #endregion
    }
}
