#nullable enable
using GDEngine.Core.Components;
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;
using System;

namespace GDGame.Demos.Controllers
{
    public class PositionRotationController : Component
    {
        #region Fields
        private AnimationCurve3D? _positionCurve;
        private AnimationCurve3D? _rotationCurve;

        private float totalElapsedTimeSecs;
        private Quaternion _originalLocalRotation;
        private Vector3 _oldYawPitchRoll;

        #endregion
        #region Properties
        public AnimationCurve3D? PositionCurve { get => _positionCurve; set => _positionCurve = value; }
        public AnimationCurve3D? RotationCurve { get => _rotationCurve; set => _rotationCurve = value; }

        #endregion
        protected override void Update(float deltaTime)
        {
            if (Transform == null || _positionCurve == null || _rotationCurve == null)
                return;

            totalElapsedTimeSecs += deltaTime;

            //absolute movement of the object to position(s) defined on curve
            Transform.TranslateTo(_positionCurve.Evaluate(totalElapsedTimeSecs)); 

            //FIXED - gimbal lock on XY rotation - get delta between two calls to evaluate and use delta as update
            var nextYawPitchRoll = _rotationCurve.Evaluate(totalElapsedTimeSecs);

            var deltaYawPitchRoll = nextYawPitchRoll - _oldYawPitchRoll;
            if(deltaYawPitchRoll.LengthSquared() > 0)
                Transform.RotateEulerBy(deltaYawPitchRoll, true); //45.1f

            _oldYawPitchRoll = nextYawPitchRoll;
        }

        protected override void Awake()
        {
            if (Transform == null)
                throw new NullReferenceException(nameof(Transform));

            _originalLocalRotation = Transform.LocalRotation;
            _oldYawPitchRoll = Vector3.Zero;
        }
    }
}
