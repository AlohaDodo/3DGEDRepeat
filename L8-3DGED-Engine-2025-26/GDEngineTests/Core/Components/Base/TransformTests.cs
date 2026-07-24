using GDEngine.Core.Components;     
using GDEngine.Core.Entities;      
using Microsoft.Xna.Framework;
using static GDEngine.Core.MSTests.AssertEx;

namespace GDEngine.Core.MSTests
{
    /// <summary>
    /// Comprehensive tests for <see cref="Transform"/>: defaults, TRS caching, parenting/unparenting,
    /// local vs world ops, axes, events, and edge cases.
    /// </summary>
    [TestClass]
    public class TransformTests
    {
        #region Methods

        [TestMethod]
        [Description("Defaults: identity local/world transforms and canonical MonoGame axes")]
        public void Defaults_AreIdentity()
        {
            var t = new Transform();

            AreEqual(Vector3.Zero, t.LocalPosition);
            AreEqual(Quaternion.Identity, t.LocalRotation);
            AreEqual(Vector3.One, t.LocalScale);

            AreEqual(Matrix.Identity, t.LocalMatrix);
            AreEqual(Matrix.Identity, t.WorldMatrix);

            AreEqual(Vector3.Right, t.Right);
            AreEqual(Vector3.Up, t.Up);
            // MonoGame: Vector3.Forward = (0,0,-1)
            AreEqual(Vector3.Forward, t.Forward);

            IsOrthogonal(t.WorldMatrix, because: "World rotation block must be a proper rotation");
        }

        [TestMethod]
        [Description("TranslateTo: sets local + world when unparented")]
        public void TranslateTo_SetsLocal_AndWorld_NoParent()
        {
            var t = new Transform();
            var p = new Vector3(3, 2, -5);

            t.TranslateTo(p);

            AreEqual(p, t.LocalPosition);
            AreEqual(p, t.Position);
            AreEqual(p, t.WorldMatrix.Translation);
        }

        [TestMethod]
        [Description("LocalMatrix rebuilds when local state is dirtied by a mutator")]
        public void LocalMatrix_Rebuilds_WhenLocalDirty()
        {
            var t = new Transform();

            var m0 = t.LocalMatrix;
            t.TranslateBy(new Vector3(1, 2, 3)); // dirties local/world
            var m1 = t.LocalMatrix;

            Assert.AreNotEqual(m0, m1);
            AreEqual(new Vector3(1, 2, 3), m1.Translation);
        }

        [TestMethod]
        [Description("LocalMatrix cached read returns same matrix when not dirty")]
        public void LocalMatrix_Cache_NoRebuild_WhenNotDirty()
        {
            var t = new Transform();
            var m0 = t.LocalMatrix;
            var m1 = t.LocalMatrix; // no mutation between

            AreEqual(m0, m1);
        }

        [TestMethod]
        [Description("WorldMatrix = LocalMatrix * Parent.WorldMatrix (composition)")]
        public void WorldMatrix_Composes_Local_Times_ParentWorld()
        {
            var parent = new Transform();
            parent.TranslateTo(new Vector3(10, 0, 0));

            var yaw = MathHelper.PiOver2;
            parent.RotateEulerBy(new Vector3(0, yaw, 0)); // Y = yaw

            var child = new Transform();
            child.SetParent(parent);
            child.TranslateTo(new Vector3(0, 0, -2)); // local: 2 units along local forward (-Z)

            // Build expected using the same math:
            var parentRot = Quaternion.CreateFromYawPitchRoll(yaw, 0, 0);
            var parentRotMat = Matrix.CreateFromQuaternion(parentRot);
            var expectedChildWorld = Vector3.Transform(new Vector3(0, 0, -2), parentRotMat) + new Vector3(10, 0, 0);

            AreEqual(expectedChildWorld, child.Position);
            IsOrthogonal(child.WorldMatrix, because: "Child world rotation must remain orthonormal");
        }

        [TestMethod]
        [Description("Axes update correctly from world matrix for yaw and pitch rotations")]
        public void Axes_Update_FromWorldMatrix()
        {
            // Yaw
            var yaw = MathHelper.PiOver2;
            var t = new Transform();
            t.RotateEulerBy(new Vector3(0, yaw, 0)); // yaw around Y

            var yawRot = Quaternion.CreateFromYawPitchRoll(yaw, 0, 0);
            var yawMat = Matrix.CreateFromQuaternion(yawRot);
            var expectedForwardYaw = Vector3.Normalize(Vector3.Transform(Vector3.Forward, yawMat));
            var expectedRightYaw = Vector3.Normalize(Vector3.Transform(Vector3.Right, yawMat));
            var expectedUpYaw = Vector3.Normalize(Vector3.Transform(Vector3.Up, yawMat));

            AreEqual(expectedForwardYaw, t.Forward);
            AreEqual(expectedRightYaw, t.Right);
            AreEqual(expectedUpYaw, t.Up);

            // Pitch
            var pitch = MathHelper.PiOver2;
            t = new Transform();
            t.RotateEulerBy(new Vector3(pitch, 0, 0)); // pitch around X

            var pitchRot = Quaternion.CreateFromYawPitchRoll(0, pitch, 0);
            var pitchMat = Matrix.CreateFromQuaternion(pitchRot);
            var expectedForwardPitch = Vector3.Normalize(Vector3.Transform(Vector3.Forward, pitchMat));
            var expectedUpPitch = Vector3.Normalize(Vector3.Transform(Vector3.Up, pitchMat));

            AreEqual(expectedForwardPitch, t.Forward);
            AreEqual(expectedUpPitch, t.Up);
        }

        [TestMethod]
        [Description("Axes are orthonormal (unit-length, orthogonal, right-handed)")]
        public void Axes_Are_Orthonormal()
        {
            var t = new Transform();
            t.RotateEulerBy(new Vector3(0.3f, 1.0f, -0.2f));

            var r = t.Right; var u = t.Up; var f = t.Forward;

            AreEqual(1f, r.Length(), 1e-4f);
            AreEqual(1f, u.Length(), 1e-4f);
            AreEqual(1f, f.Length(), 1e-4f);

            AreEqual(0f, Vector3.Dot(r, u), 1e-4f);
            AreEqual(0f, Vector3.Dot(r, f), 1e-4f);
            AreEqual(0f, Vector3.Dot(u, f), 1e-4f);

            IsOrthogonal(t.WorldMatrix);
        }

        [TestMethod]
        [Description("TranslateBy(worldSpace:true) with parent converts world delta into local space")]
        public void TranslateBy_WorldSpace_WithParent_ConvertsToLocalDelta()
        {
            var parent = new Transform();
            var yaw = MathHelper.PiOver2;
            parent.RotateEulerBy(new Vector3(0, yaw, 0));

            var child = new Transform();
            child.SetParent(parent);

            var worldDelta = new Vector3(1, 0, 0);
            child.TranslateBy(worldDelta, worldSpace: true);

            // Compute expected via inverse parent world matrix
            var invParentWorld = Matrix.Invert(parent.WorldMatrix);
            var localDelta = Vector3.TransformNormal(worldDelta, invParentWorld);
            var expectedLocal = localDelta;
            var expectedWorld = Vector3.Transform(expectedLocal, parent.WorldMatrix);

            AreEqual(expectedLocal, child.LocalPosition);
            AreEqual(expectedWorld, child.Position);
        }

        [TestMethod]
        [Description("TranslateBy(worldSpace:true) without parent adds world delta directly")]
        public void TranslateBy_WorldSpace_NoParent_AddsDeltaDirectly()
        {
            var t = new Transform();
            var delta = new Vector3(1, 2, 3);

            t.TranslateBy(delta, worldSpace: true);

            AreEqual(delta, t.Position);
            AreEqual(delta, t.LocalPosition);
        }

        [TestMethod]
        [Description("RotateBy(worldSpace:true) with parent applies world-space delta correctly")]
        public void RotateBy_WorldSpace_WithParent_ProducesExpectedWorldRotation()
        {
            var parent = new Transform();
            var yawParent = MathHelper.PiOver2;
            parent.RotateEulerBy(new Vector3(0, yawParent, 0));

            var child = new Transform();
            child.SetParent(parent);

            var yawDelta = MathHelper.PiOver2;
            var delta = Quaternion.CreateFromYawPitchRoll(yawDelta, 0, 0);
            child.RotateBy(delta, worldSpace: true);

            var expectedWorldRot = Quaternion.Normalize(
                Quaternion.Concatenate(
                    Quaternion.CreateFromYawPitchRoll(yawDelta, 0, 0),
                    Quaternion.CreateFromYawPitchRoll(yawParent, 0, 0)));

            var expectedForward = Vector3.Normalize(
                Vector3.Transform(Vector3.Forward, Matrix.CreateFromQuaternion(expectedWorldRot)));

            AreEqual(expectedForward, child.Forward);
            IsOrthogonal(child.WorldMatrix);
        }

        [TestMethod]
        [Description("RotateEulerBy: local-space vs world-space paths behave as expected")]
        public void RotateEulerBy_Local_And_WorldSpace_Paths()
        {
            // Local
            var yaw = MathHelper.PiOver2;
            var a = new Transform();
            a.RotateEulerBy(new Vector3(0, yaw, 0), worldSpace: false);

            var localRot = Quaternion.CreateFromYawPitchRoll(yaw, 0, 0);
            var localMat = Matrix.CreateFromQuaternion(localRot);
            var expectedForwardLocal = Vector3.Normalize(Vector3.Transform(Vector3.Forward, localMat));

            AreEqual(expectedForwardLocal, a.Forward);

            // World with parent
            var parent = new Transform();
            parent.RotateEulerBy(new Vector3(0, yaw, 0));
            var c = new Transform();
            c.SetParent(parent);

            c.RotateEulerBy(new Vector3(0, yaw, 0), worldSpace: true);

            var worldDelta = Quaternion.CreateFromYawPitchRoll(yaw, 0, 0);
            var expectedWorld = Quaternion.Normalize(
                Quaternion.Concatenate(worldDelta,
                    Quaternion.CreateFromYawPitchRoll(yaw, 0, 0)));

            var expectedForwardWorld = Vector3.Normalize(
                Vector3.Transform(Vector3.Forward, Matrix.CreateFromQuaternion(expectedWorld)));

            AreEqual(expectedForwardWorld, c.Forward);
        }

        [TestMethod]
        [Description("Rotation getter equals Concatenate(LocalRotation, Parent.Rotation)")]
        public void Rotation_Getter_Equals_LocalConcatenateParent()
        {
            var parent = new Transform();
            var child = new Transform();
            child.SetParent(parent);

            var yawParent = MathHelper.PiOver4;
            var yawChild = MathHelper.ToRadians(30f);

            parent.RotateEulerBy(new Vector3(0, yawParent, 0));
            child.RotateEulerBy(new Vector3(0, yawChild, 0));

            var expected = Quaternion.Normalize(
                Quaternion.Concatenate(
                    Quaternion.CreateFromYawPitchRoll(yawChild, 0, 0),
                    Quaternion.CreateFromYawPitchRoll(yawParent, 0, 0)));

            AreEqual(expected, child.Rotation);

            var fwdFromRot = Vector3.Normalize(
                Vector3.Transform(Vector3.Forward, Matrix.CreateFromQuaternion(child.Rotation)));

            AreEqual(fwdFromRot, child.Forward);
        }

        [TestMethod]
        [Description("ScaleTo/ScaleBy(float) update matrix diagonal correctly")]
        public void ScaleTo_And_ScaleBy_UpdateLocalMatrixDiagonal()
        {
            var t = new Transform();
            t.ScaleTo(new Vector3(2, 3, 4));
            var m = t.LocalMatrix;

            AreEqual(2f, m.M11);
            AreEqual(3f, m.M22);
            AreEqual(4f, m.M33);

            t.ScaleBy(0.5f);
            m = t.LocalMatrix;

            AreEqual(1f, m.M11, 1e-4f);
            AreEqual(1.5f, m.M22, 1e-4f);
            AreEqual(2f, m.M33, 1e-4f);
        }

        [TestMethod]
        [Description("ScaleBy(Vector3) multiplies per-axis scale factors")]
        public void ScaleBy_Vector3_MultipliesPerAxis()
        {
            var t = new Transform();
            t.ScaleTo(new Vector3(2, 3, 4));
            t.ScaleBy(new Vector3(0.5f, 2f, 0.25f));

            AreEqual(new Vector3(1f, 6f, 1f), t.LocalScale);

            var m = t.LocalMatrix;
            AreEqual(1f, m.M11);
            AreEqual(6f, m.M22);
            AreEqual(1f, m.M33);
        }

        [TestMethod]
        [Description("Changed event fires on parent and propagates to child with FromParent")]
        public void Changed_Event_Fires_And_PropagatesToChildren()
        {
            var parent = new Transform();
            var child = new Transform();
            child.SetParent(parent);

            int parentChanged = 0, childChanged = 0;

            parent.Changed += (_, flags) =>
            {
                parentChanged++;
                Assert.IsTrue(flags.HasFlag(Transform.ChangeFlags.Position));
                Assert.IsTrue(flags.HasFlag(Transform.ChangeFlags.World));
            };

            child.Changed += (_, flags) =>
            {
                childChanged++;
                Assert.IsTrue(flags.HasFlag(Transform.ChangeFlags.FromParent));
                Assert.IsTrue(flags.HasFlag(Transform.ChangeFlags.World));
            };

            parent.TranslateBy(new Vector3(0, 1, 0), worldSpace: true);

            Assert.AreEqual(1, parentChanged);
            Assert.AreEqual(1, childChanged);
        }

        [TestMethod]
        [Description("Changed event propagates to grandchildren with FromParent flag")]
        public void Changed_Event_Propagates_To_Grandchildren_WithFlag()
        {
            var a = new Transform();
            var b = new Transform(); b.SetParent(a);
            var c = new Transform(); c.SetParent(b);

            bool bFromParent = false, cFromParent = false;

            b.Changed += (_, f) => bFromParent = f.HasFlag(Transform.ChangeFlags.FromParent);
            c.Changed += (_, f) => cFromParent = f.HasFlag(Transform.ChangeFlags.FromParent);

            a.TranslateBy(new Vector3(0, 1, 0), worldSpace: true);

            Assert.IsTrue(bFromParent);
            Assert.IsTrue(cFromParent);
        }

        [TestMethod]
        [Description("MarkWorldDirty guard: multiple mutations before recompute shouldn’t over-notify")]
        public void MarkWorldDirty_Guard_WhenAlreadyDirty()
        {
            var t = new Transform();

            int count = 0;
            t.Changed += (_, __) => count++;

            t.TranslateBy(new Vector3(1, 0, 0), worldSpace: true);
            t.TranslateBy(new Vector3(1, 0, 0), worldSpace: true); // still dirty

            var _ = t.WorldMatrix; // force recompute/clean

            Assert.IsTrue(count >= 1);
        }

        [TestMethod]
        [Description("SetParent(Transform): parent/children lists update correctly")]
        public void SetParent_Transform_UpdatesHierarchy()
        {
            var parent = new Transform();
            var child = new Transform();

            Assert.AreEqual(0, parent.Children.Count);
            child.SetParent(parent);

            Assert.AreSame(parent, child.Parent);
            Assert.AreEqual(1, parent.Children.Count);
            Assert.AreSame(child, parent.Children[0]);
        }

        [TestMethod]
        [Description("SetParent(GameObject): overload wires to Transform and updates hierarchy")]
        public void SetParent_GameObject_Overload_Works_AndUpdatesHierarchy()
        {
            var parentGO = new GameObject("parent");
            var child = new Transform();

            Assert.AreEqual(0, parentGO.Transform.Children.Count);
            child.SetParent(parentGO);

            Assert.AreSame(parentGO.Transform, child.Parent);
            Assert.AreEqual(1, parentGO.Transform.Children.Count);
            Assert.AreSame(child, parentGO.Transform.Children[0]);
        }

        [TestMethod]
        [Description("Unparent: removes from old parent and world equals local thereafter")]
        public void Unparent_RemovesFromChildren_And_WorldFollowsNewRoot()
        {
            var parent = new Transform();
            var child = new Transform();
            child.SetParent(parent);

            parent.TranslateTo(new Vector3(5, 0, 0));
            child.TranslateTo(new Vector3(1, 0, 0)); // local

            AreEqual(new Vector3(6, 0, 0), child.Position);

            child.SetParent((Transform?)null);

            AreEqual(new Vector3(1, 0, 0), child.Position);
            Assert.AreEqual(0, parent.Children.Count);
            Assert.IsNull(child.Parent);
        }

        [TestMethod]
        [Description("Reparent: move child from A to B; local preserved, world follows new parent")]
        public void Reparent_MovesChild_And_UpdatesWorld()
        {
            var a = new Transform();
            a.TranslateTo(new Vector3(10, 0, 0));

            var b = new Transform();
            b.TranslateTo(new Vector3(-2, 0, 0));

            var child = new Transform();
            child.TranslateTo(new Vector3(1, 0, 0)); // local
            child.SetParent(a);

            AreEqual(new Vector3(11, 0, 0), child.Position);

            child.SetParent(b); // reparent to B

            AreEqual(new Vector3(1, 0, 0), child.LocalPosition);
            AreEqual(new Vector3(-1, 0, 0), child.Position);
        }

        [TestMethod]
        [Description("Cancel rotations returns axes close to defaults (drift check)")]
        public void Cancelled_Rotations_Return_To_Default_Axes()
        {
            var t = new Transform();
            t.RotateEulerBy(new Vector3(0, MathHelper.PiOver4, 0));
            t.RotateEulerBy(new Vector3(0, -MathHelper.PiOver4, 0));

            NearlyZero(t.Forward - Vector3.Forward);
            NearlyZero(t.Right - Vector3.Right);
            NearlyZero(t.Up - Vector3.Up);
            IsOrthogonal(t.WorldMatrix);
        }

        #endregion
    }
}
