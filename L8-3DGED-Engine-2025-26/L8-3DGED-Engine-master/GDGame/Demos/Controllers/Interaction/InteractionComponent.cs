using System;
using GDEngine.Core.Audio;
using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using GDEngine.Core.Rendering.Base;
using GDEngine.Core.Services;
using GDEngine.Core.Systems;
using GDEngine.Core.Utilities;
using Microsoft.Xna.Framework.Input;

namespace GDGame.Demos.Controllers
{
    /// <summary>
    /// When I LMC if object is Interactable then remove it!
    /// </summary>
    public class InteractionComponent : Component
    {
        private Scene _scene;
        private PhysicsSystem _physicsSystem;

        private float _maxDistance = 100;
        private LayerMask _hitMask = LayerMask.All;
        private bool _hitTriggers = false;
        private MouseState _currentMouseState;
        private MouseState _oldMouseState;

        public float MaxDistance { get => _maxDistance; set => _maxDistance = value; }
        public LayerMask HitMask { get => _hitMask; set => _hitMask = value; }
        public bool HitTriggers { get => _hitTriggers; set => _hitTriggers = value; }

        protected override void Start()
        {
            if (GameObject == null)
                throw new NullReferenceException(nameof(GameObject));

            _scene = GameObject.Scene
                        ?? throw new NullReferenceException(nameof(GameObject.Scene));

            _physicsSystem = _scene.GetSystem<PhysicsSystem>()
                            ?? throw new InvalidOperationException(
                                "UIPickerInfoRenderer requires a PhysicsSystem in the Scene.");

        }
        protected override void Update(float deltaTime)
        {
            _currentMouseState = Mouse.GetState();

            if (GameObject == null)
                return;

            var scene = GameObject.Scene;
            if (scene == null)
                return;

            // Re-acquire active camera in case the scene switched cameras.
            var camera = scene.ActiveCamera ?? null;
            if (camera == null)
                return;

            var device = scene.Context.GraphicsDevice;
            var viewport = camera.GetViewport(device);

            // Reticle is at screen center (same as UIReticleRenderer).
            var center = viewport.GetCenter();

            //do a raycast
            RaycastHit hitInfo;
            if (_physicsSystem.RaycastFromScreen(
                    camera,
                    center.X,
                    center.Y,
                    MaxDistance,
                    HitMask,
                    out hitInfo,
                    HitTriggers))
            {
                System.Diagnostics.Debug.WriteLine($"{hitInfo.Body.GameObject.Name}");

                //ok, this thing is interesting!!!


                if (_currentMouseState.LeftButton == ButtonState.Pressed
                    && _oldMouseState.LeftButton == ButtonState.Released)
                {
                    //play sound
                    EngineContext.Instance.Events.Publish(new PlaySfxEvent("SFX_UI_Click_Designed_Pop_Generic_1", 1, false, null));
                    //check mouse click

                    _scene.Remove(hitInfo.Body.GameObject);
                }
            }

            _oldMouseState = _currentMouseState;
        }
    }
}
