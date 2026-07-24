using GDEngine.Core.Entities;
using GDEngine.Core.Events;
using GDEngine.Core.Impulses;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Services
{
    /// <summary>
    /// Service hub to allow access to engine services and shared resources.
    /// Utilising the SOLI<b>D</b> with Dependency Inversion Principle
    /// </summary>
    /// <see cref="Scene"/>
    /// <see cref="GameObject"/>
    /// <see cref="Camera"/>
    /// <see cref="EventSystem"/>
    /// <see cref="https://www.geeksforgeeks.org/system-design/solid-principle-in-programming-understand-with-real-life-examples/"/>
    public class EngineContext : IDisposable       //TODO - add thread lock
    {
        #region Static Fields
        private static EngineContext? _instance;
        #endregion

        #region Fields
        private bool _disposed = false;
        #endregion

        #region Properties
        public GraphicsDevice GraphicsDevice { get; }
        public ContentManager Content { get; }
        public SpriteBatch SpriteBatch { get; }
        public EventBus Events { get; }
        public ImpulseBus Impulses { get; }

        public static EngineContext? Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("Ensure you call Initialize() first");

                return _instance;
            }
        }
        #endregion

        #region Lifecycle Methods
        public static void Initialize(GraphicsDevice graphicsDevice, ContentManager content)
        {
            if (_instance != null)
                throw new InvalidOperationException("EngineContext already initialized");

            _instance = new EngineContext(graphicsDevice, content);
        }

        private EngineContext(GraphicsDevice graphicsDevice, ContentManager content)
        {
            GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            SpriteBatch = new SpriteBatch(graphicsDevice);
            Events = new EventBus();
            Impulses = new ImpulseBus();
        }
        #endregion

        #region Housekeeping Methods
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources
                SpriteBatch?.Dispose();
                Content?.Dispose();
                Events?.Dispose();
                Impulses?.Dispose();
                // Note: GraphicsDevice is typically owned by the Game class, so we don't dispose it here
            }

            _disposed = true;
        }

        ~EngineContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
