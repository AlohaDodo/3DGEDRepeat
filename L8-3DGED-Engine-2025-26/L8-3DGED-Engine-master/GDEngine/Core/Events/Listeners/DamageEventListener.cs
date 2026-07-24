using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using GDEngine.Core.Services;

namespace GDEngine.Core.Events
{
    public class DamageEventListener : Component
    {
        private Scene? _scene;
        private EngineContext? _context;
        private EventBus? _eventBus;

        protected override void Awake()
        {
            base.Awake();

            if (GameObject == null)
                throw new NullReferenceException(nameof(GameObject));

            // Get scene + context from the GameObject we are attached to
            _scene = GameObject.Scene;

            if (_scene == null)
                throw new NullReferenceException(nameof(_scene));
            _context = _scene.Context;

            // Handle to the EventBus
            _eventBus = _context.Events;

            // Subscribe to DamageEvent via the EventBus
            //_eventBus.Subscribe<DamageEvent>(
            //    (e) => System.Diagnostics.Debug.WriteLine(e.Amount),
            //    0,
            //    (e) => e.Amount > 5,
            //    true);

            _eventBus.On<DamageEvent>()
                .When(e => e.Amount > 0)
                .Once()
                .Do((e) =>
                { 
                    System.Diagnostics.Debug.WriteLine(e.TargetName);
                    //play sound
                    //store to file
                    //instantiate an enemy
                   // Time.TimeScale = 0;
                });
        }

        private void HandleDamage(DamageEvent e)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[DamageListener] Target={e.TargetName}, " +
                $"Source={e.SourceName}, Amount={e.Amount}, " +
                $"Type={e.Type}, Critical={e.IsCritical}, " +
                $"HitPos={e.HitPosition}");
        }
    }
}
