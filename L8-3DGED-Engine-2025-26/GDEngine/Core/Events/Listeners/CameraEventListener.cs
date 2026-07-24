using GDEngine.Core.Components;
using GDEngine.Core.Entities;

namespace GDEngine.Core.Events
{
    public class CameraEventListener : Component
    {
        private Scene? _scene;
        private EventBus? _events;
        private Camera? _camera;

        protected override void Start()
        {
            if (GameObject == null) 
                throw new NullReferenceException(nameof(GameObject));

            _scene = GameObject.Scene;

            if (_scene == null) 
                throw new NullReferenceException(nameof(_scene));

            _events = _scene.Context.Events;

            _events.On<CameraEvent>()
                .Do(HandleCameraChange);
        }

        private void HandleCameraChange(CameraEvent @event)
        {
            string targetName = @event.TargetCameraName;
            var go = _scene?.Find(go => go.Name.Equals(targetName));
            _camera = go?.GetComponent<Camera>();
            if(_camera != null)
                _scene?.SetActiveCamera(_camera);
        }
    }
}
