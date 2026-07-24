using GDEngine.Core.Entities;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Components
{

    /// <summary>
    /// Hierarchical transform with cached TRS, dirty-flag propagation, and convenience mutators.
    /// </summary>
    /// <see cref="Component"/>
    /// <see cref="GameObject"/>
    public sealed class Transform : Component
    {
        #region Enums
        [Flags]
        public enum ChangeFlags : sbyte
        {
            None = 0,
            Position = 1 << 0,
            Rotation = 1 << 1,
            Scale = 1 << 2,
            Parent = 1 << 3,
            Local = 1 << 4,
            World = 1 << 5,
            FromParent = 1 << 6
        }
        #endregion

        #region Fields
        private Vector3 _localPosition = Vector3.Zero;
        private Quaternion _localRotation = Quaternion.Identity;
        private Vector3 _localScale = Vector3.One;

        private Transform? _parent;
        private readonly List<Transform> _children = new();

        // Caches
        private Matrix _localMatrix = Matrix.Identity;
        private Matrix _worldMatrix = Matrix.Identity;

        // Dirty flags
        private bool _localDirty = true;
        private bool _worldDirty = true;

        // Raised after any local or parent-driven change that affects world.</summary>
        public event Action<Transform, ChangeFlags>? Changed;

        #endregion

        #region Properties
        public Transform? Parent => _parent;
        public IReadOnlyList<Transform> Children => _children;

        public Vector3 LocalPosition
        {
            get => _localPosition;
            private set
            {
                if (_localPosition == value)
                    return;
                _localPosition = value;
                MarkLocalDirty(ChangeFlags.Position);
            }
        }

        public Quaternion LocalRotation
        {
            get => _localRotation;
            private set
            {
                if (_localRotation == value)
                    return;
                _localRotation = value;
                MarkLocalDirty(ChangeFlags.Rotation);
            }
        }

        public Vector3 LocalScale
        {
            get => _localScale;
            private set
            {
                if (_localScale == value)
                    return;
                _localScale = value;
                MarkLocalDirty(ChangeFlags.Scale);
            }
        }

        /// <summary>
        /// Local TRS matrix (cached).
        /// </summary>
        public Matrix LocalMatrix
        {
            get
            {
                if (_localDirty)
                {
                    _localMatrix =
                        Matrix.CreateScale(_localScale) *
                        Matrix.CreateFromQuaternion(_localRotation) *
                        Matrix.CreateTranslation(_localPosition);
                    _localDirty = false;
                    _worldDirty = true;
                }
                return _localMatrix;
            }
        }

        /// <summary>
        /// World matrix = Local * ParentWorld (cached).
        /// </summary>
        public Matrix WorldMatrix
        {
            get
            {
                if (_worldDirty)
                {
                    _worldMatrix = _parent == null ? LocalMatrix : LocalMatrix * _parent.WorldMatrix;
                    _worldDirty = false;
                }
                return _worldMatrix;
            }
        }

        /// <summary>World-space position convenience (from WorldMatrix).</summary>
        public Vector3 Position
        {
            get => WorldMatrix.Translation;
            private set
            {
                if (_parent == null)
                {
                    LocalPosition = value;
                }
                else
                {
                    var invParent = Matrix.Invert(_parent.WorldMatrix);
                    var local = Vector3.Transform(value, invParent);
                    LocalPosition = local;
                }
            }
        }

        /// <summary>World-space rotation convenience (local ∘ parent).</summary>
        public Quaternion Rotation
        {
            get => _parent == null
                ? _localRotation
                : Quaternion.Normalize(Quaternion.Concatenate(_localRotation, _parent.Rotation));
            private set
            {
                if (_parent == null)
                {
                    LocalRotation = value;
                }
                else
                {
                    var invParent = Quaternion.Inverse(_parent.Rotation);
                    LocalRotation = Quaternion.Normalize(Quaternion.Concatenate(value, invParent));
                }
            }
        }

        //// Column-based extraction
        //public Vector3 Right
        //{
        //    get { var m = WorldMatrix; return Vector3.Normalize(new Vector3(m.M11, m.M21, m.M31)); }
        //}
        //public Vector3 Up
        //{
        //    get { var m = WorldMatrix; return Vector3.Normalize(new Vector3(m.M12, m.M22, m.M32)); }
        //}
        //public Vector3 Forward
        //{
        //    // If you consider +Z forward, keep as below.
        //    get { var m = WorldMatrix; return Vector3.Normalize(new Vector3(m.M13, m.M23, m.M33)); }
        //}

        // Axis extraction based on world rotation (Quaternion) so that
        // Right/Up/Forward are simply the rotated basis vectors.
        public Vector3 Right
        {
            get
            {
                var rotMatrix = Matrix.CreateFromQuaternion(Rotation);
                var v = Vector3.Transform(Vector3.Right, rotMatrix);
                return Vector3.Normalize(v);
            }
        }

        public Vector3 Up
        {
            get
            {
                var rotMatrix = Matrix.CreateFromQuaternion(Rotation);
                var v = Vector3.Transform(Vector3.Up, rotMatrix);
                return Vector3.Normalize(v);
            }
        }

        public Vector3 Forward
        {
            get
            {
                var rotMatrix = Matrix.CreateFromQuaternion(Rotation);
                var v = Vector3.Transform(Vector3.Forward, rotMatrix);
                return Vector3.Normalize(v);
            }
        }

        public int ChildCount
        {
            get
            {
                return Children.Count;
            }
        }

        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new <see cref="Transform"/>.
        /// </summary>
        public Transform() { }
        #endregion

        #region Core Methods
        /// <summary>
        /// Sets the parent transform. Keeps local TRS unchanged; world will follow parent.
        /// </summary>
        /// <param name="gameObject">Parent containing transform.</param>
        public void SetParent(GameObject gameObject)
        {
            SetParent(gameObject?.Transform);
        }

        /// <summary>
        /// Sets the parent transform. Keeps local TRS unchanged; world will follow parent.
        /// </summary>
        /// <param name="newParent">New parent or null to unparent.</param>
        public void SetParent(Transform? newParent)
        {
            if (newParent == _parent)
                return;

            _parent?._children.Remove(this);
            _parent = newParent;
            _parent?._children.Add(this);

            MarkWorldDirty(ChangeFlags.Parent);
        }

        /// <summary>
        /// Moves this transform to a target position in local space.
        /// </summary>
        /// <param name="target">Scale to value for new localScale.</param>
        public void TranslateTo(in Vector3 target)
        {
            _localPosition = target;
            MarkLocalDirty(ChangeFlags.Position);
        }

        /// <summary>
        /// Translates this transform by a delta. If worldSpace is true, the delta is interpreted in world space.
        /// </summary>
        /// <param name="delta">Translation delta.</param>
        /// <param name="worldSpace">If true, delta is in world space; otherwise local space.</param>
        public void TranslateBy(in Vector3 delta, bool worldSpace = false)
        {
            if (!worldSpace)
            {
                _localPosition += delta;
                MarkLocalDirty(ChangeFlags.Position);
                return;
            }

            // Convert world delta to local-space delta (ignore translation)
            if (_parent == null)
            {
                _localPosition += delta;
            }
            else
            {
                var invParent = Matrix.Invert(_parent.WorldMatrix);
                var localDelta = Vector3.TransformNormal(delta, invParent);
                _localPosition += localDelta;
            }

            MarkLocalDirty(ChangeFlags.Position);
        }

        /// <summary>
        /// Rotates this transform by a quaternion delta. If worldSpace is true, the rotation is applied in world space.
        /// </summary>
        /// <param name="delta">Rotation delta quaternion.</param>
        /// <param name="worldSpace">If true, apply delta in world space; otherwise local space.</param>
        public void RotateBy(in Quaternion delta, bool worldSpace = false)
        {
            Quaternion localDelta = delta;

            if (worldSpace && _parent != null)
            {
                // Convert world-space delta into local-space: invParent * delta * parent
                var invParent = Quaternion.Inverse(_parent.Rotation);
                localDelta = Quaternion.Normalize(Quaternion.Concatenate(
                    Quaternion.Concatenate(invParent, delta), _parent.Rotation));
            }

            // Pre-multiply to apply delta before current local orientation
            _localRotation = Quaternion.Normalize(Quaternion.Concatenate(localDelta, _localRotation));
            MarkLocalDirty(ChangeFlags.Rotation);
        }

        /// <summary>
        /// Rotates this transform by Euler angles (radians). If worldSpace is true, angles are applied in world space.
        /// </summary>
        /// <param name="eulerRadians">XYZ Euler angles in radians.</param>
        /// <param name="worldSpace">If true, apply in world space; otherwise local space.</param>
        public void RotateEulerBy(in Vector3 eulerRadians, bool worldSpace = false)
        {
            RotateBy(Quaternion.CreateFromYawPitchRoll(eulerRadians.Y, eulerRadians.X, eulerRadians.Z),
                worldSpace);
        }


        /// <summary>
        /// Sets this transform's world-space rotation directly.
        /// Useful for camera look-at controllers that want to avoid incremental drift.
        /// </summary>
        /// <param name="worldRotation">Desired world-space rotation.</param>
        public void RotateToWorld(in Quaternion worldRotation)
        {
            Rotation = Quaternion.Normalize(worldRotation);
        }


        /// <summary>
        /// Scales this transform to a non-uniform vector in local space.
        /// </summary>
        /// <param name="scaleTo">Scale to value for new localScale.</param>
        public void ScaleTo(in Vector3 scaleTo)
        {
            _localScale = scaleTo;
            MarkLocalDirty(ChangeFlags.Scale);
        }

        /// <summary>
        /// Scales this transform <b>by</b> a non-uniform vector in local space.
        /// </summary>
        /// <param name="scaleBy">Per-axis scale-by multiplier.</param>
        public void ScaleBy(in Vector3 scaleBy)
        {
            _localScale *= scaleBy;
            MarkLocalDirty(ChangeFlags.Scale);
        }

        /// <summary>
        /// Scales this transform <b>by</b> a uniform scalar in local space.
        /// </summary>
        /// <param name="scaleBy">Scalar scale-by factor.</param>
        public void ScaleBy(float scaleBy)
        {
            _localScale *= scaleBy;
            MarkLocalDirty(ChangeFlags.Scale);
        }

        // Dirty helpers

        private void MarkLocalDirty(ChangeFlags reason)
        {
            _localDirty = true;
            MarkWorldDirty(reason | ChangeFlags.Local);
        }

        private void MarkWorldDirty(ChangeFlags reason)
        {
            if (!_worldDirty)
                _worldDirty = true;

            Changed?.Invoke(this, reason | ChangeFlags.World);

            for (int i = 0; i < _children.Count; i++)
                _children[i].MarkWorldDirty(reason | ChangeFlags.FromParent);
        }
        #endregion

        #region Lifecycle Methods
        // None
        #endregion

        #region Housekeeping Methods
        // None
        #endregion
    }
}
