using Microsoft.Xna.Framework;
using System.Text;

namespace GDEngine.Core.MSTests
{
    /// <summary>
    /// Numeric assertion helpers with readable failure diffs for float, Vector3, Quaternion, and Matrix.
    /// </summary>
    public static class AssertEx
    {
        #region Static Fields
        /// <summary>Default absolute tolerance for equality checks.</summary>
        public const float Epsilon = 1e-4f;
        #endregion

        #region Methods
        /// <summary>
        /// Assert two floats are approximately equal (abs or relative).
        /// <see cref="AreEqual(Vector3, Vector3, float, string?)"/>
        /// </summary>
        public static void AreEqual(float expected, float actual, float eps = Epsilon, string? because = null)
        {
            var abs = Math.Abs(actual - expected);
            var denom = Math.Max(1f, Math.Abs(expected));
            var rel = abs / denom;

            if (abs <= eps || rel <= eps) return;

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(because)) sb.AppendLine(because);
            sb.AppendLine("Float approx equality failed:")
              .AppendLine($"  expected: {expected:G9}")
              .AppendLine($"  actual  : {actual:G9}")
              .AppendLine($"  abs err : {abs:G9}")
              .AppendLine($"  rel err : {rel:G9}  (eps={eps:G9})");

            Assert.Fail(sb.ToString());
        }

        /// <summary>
        /// Assert two Vector3 are approximately equal; prints per-component deltas.
        /// </summary>
        public static void AreEqual(Vector3 expected, Vector3 actual, float eps = Epsilon, string? because = null)
        {
            if (Close(expected.X, actual.X, eps) &&
                Close(expected.Y, actual.Y, eps) &&
                Close(expected.Z, actual.Z, eps)) return;

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(because)) sb.AppendLine(because);
            sb.AppendLine("Vector3 approx equality failed:");
            AppendVecDiff(sb, "X", expected.X, actual.X, eps);
            AppendVecDiff(sb, "Y", expected.Y, actual.Y, eps);
            AppendVecDiff(sb, "Z", expected.Z, actual.Z, eps);
            sb.AppendLine("  expected: " + Format(expected))
              .AppendLine("  actual  : " + Format(actual));

            Assert.Fail(sb.ToString());
        }

        /// <summary>
        /// Assert two Quaternions are approximately equal; prints component deltas and angle error in degrees.
        /// </summary>
        public static void AreEqual(Quaternion expected, Quaternion actual, float eps = Epsilon, string? because = null)
        {
            if (Close(expected.X, actual.X, eps) &&
                Close(expected.Y, actual.Y, eps) &&
                Close(expected.Z, actual.Z, eps) &&
                Close(expected.W, actual.W, eps)) return;

            var e = Quaternion.Normalize(expected);
            var a = Quaternion.Normalize(actual);

            // Handle q ~ -q double-cover by taking |dot|
            var dot = Math.Abs(e.X * a.X + e.Y * a.Y + e.Z * a.Z + e.W * a.W);
            dot = Math.Clamp(dot, -1f, 1f);
            var angleRad = 2f * MathF.Acos(dot);
            var angleDeg = angleRad * (180f / MathF.PI);

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(because)) sb.AppendLine(because);
            sb.AppendLine("Quaternion approx equality failed:")
              .AppendLine($"  angle error ≈ {angleDeg:G6}°");

            AppendVecDiff(sb, "X", expected.X, actual.X, eps);
            AppendVecDiff(sb, "Y", expected.Y, actual.Y, eps);
            AppendVecDiff(sb, "Z", expected.Z, actual.Z, eps);
            AppendVecDiff(sb, "W", expected.W, actual.W, eps);

            sb.AppendLine("  expected: " + Format(e))
              .AppendLine("  actual  : " + Format(a));

            Assert.Fail(sb.ToString());
        }

        /// <summary>
        /// Assert two 4x4 matrices are approximately equal; prints a compact table of deltas and row max error.
        /// </summary>
        public static void AreEqual(Matrix expected, Matrix actual, float eps = Epsilon, string? because = null)
        {
            if (MatrixClose(expected, actual, eps)) return;

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(because)) sb.AppendLine(because);
            sb.AppendLine("Matrix approx equality failed:")
              .AppendLine("  expected (rows):")
              .AppendLine(Format(expected))
              .AppendLine("  actual   (rows):")
              .AppendLine(Format(actual))
              .AppendLine("  delta |abs(actual - expected)| per element:");

            AppendMatrixDeltaTable(sb, expected, actual);

            // Translation at a glance
            sb.AppendLine("  translation (expected vs actual):")
              .AppendLine($"    {new Vector3(expected.M41, expected.M42, expected.M43)} vs {new Vector3(actual.M41, actual.M42, actual.M43)}");

            Assert.Fail(sb.ToString());
        }
        #endregion

        #region Housekeeping Methods
        private static bool Close(float expected, float actual, float eps)
        {
            var abs = Math.Abs(actual - expected);
            if (abs <= eps) return true;

            var denom = Math.Max(1f, Math.Abs(expected));
            var rel = abs / denom;
            return rel <= eps;
        }

        private static void AppendVecDiff(StringBuilder sb, string label, float expected, float actual, float eps)
        {
            var abs = Math.Abs(actual - expected);
            var denom = Math.Max(1f, Math.Abs(expected));
            var rel = abs / denom;
            sb.AppendLine($"  {label}: exp={expected,10:G9} act={actual,10:G9} abs={abs,10:G9} rel={rel,10:G9} (eps={eps:G9})");
        }

        private static string Format(Vector3 v)
        {
            return $"({v.X:G9}, {v.Y:G9}, {v.Z:G9})";
        }

        private static string Format(Quaternion q)
        {
            return $"({q.X:G9}, {q.Y:G9}, {q.Z:G9}, {q.W:G9})";
        }

        private static string Row(float a, float b, float c, float d)
        {
            return $"    [{a,10:G9}  {b,10:G9}  {c,10:G9}  {d,10:G9}]";
        }

        private static string Format(Matrix m)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Row(m.M11, m.M12, m.M13, m.M14));
            sb.AppendLine(Row(m.M21, m.M22, m.M23, m.M24));
            sb.AppendLine(Row(m.M31, m.M32, m.M33, m.M34));
            sb.Append(Row(m.M41, m.M42, m.M43, m.M44));
            return sb.ToString();
        }

        private static void AppendMatrixDeltaTable(StringBuilder sb, Matrix e, Matrix a)
        {
            AppendMatrixDeltaRow(sb, e.M11, a.M11, e.M12, a.M12, e.M13, a.M13, e.M14, a.M14);
            AppendMatrixDeltaRow(sb, e.M21, a.M21, e.M22, a.M22, e.M23, a.M23, e.M24, a.M24);
            AppendMatrixDeltaRow(sb, e.M31, a.M31, e.M32, a.M32, e.M33, a.M33, e.M34, a.M34);
            AppendMatrixDeltaRow(sb, e.M41, a.M41, e.M42, a.M42, e.M43, a.M43, e.M44, a.M44);
        }

        private static void AppendMatrixDeltaRow(StringBuilder sb,
                                                 float e1, float a1, float e2, float a2, float e3, float a3, float e4, float a4)
        {
            float d1 = Math.Abs(a1 - e1);
            float d2 = Math.Abs(a2 - e2);
            float d3 = Math.Abs(a3 - e3);
            float d4 = Math.Abs(a4 - e4);
            float max = Math.Max(Math.Max(d1, d2), Math.Max(d3, d4));

            sb.AppendLine($"    [{d1,10:G9}  {d2,10:G9}  {d3,10:G9}  {d4,10:G9}]   max row abs={max:G9}");
        }

        private static bool MatrixClose(Matrix e, Matrix a, float eps)
        {
            return Close(e.M11, a.M11, eps) && Close(e.M12, a.M12, eps) && Close(e.M13, a.M13, eps) && Close(e.M14, a.M14, eps) &&
                   Close(e.M21, a.M21, eps) && Close(e.M22, a.M22, eps) && Close(e.M23, a.M23, eps) && Close(e.M24, a.M24, eps) &&
                   Close(e.M31, a.M31, eps) && Close(e.M32, a.M32, eps) && Close(e.M33, a.M33, eps) && Close(e.M34, a.M34, eps) &&
                   Close(e.M41, a.M41, eps) && Close(e.M42, a.M42, eps) && Close(e.M43, a.M43, eps) && Close(e.M44, a.M44, eps);
        }

        /// <summary>
        /// Assert a vector is (near) zero within tolerance.
        /// </summary>
        public static void NearlyZero(Vector3 actual, float eps = Epsilon, string? because = null)
        {
            var len = actual.Length();
            if (len <= eps) return;

            var msg = (because is null ? "" : because + Environment.NewLine) +
                      $"Vector3 should be ~0 but |v|={len:G9}  v={actual}";
            Assert.Fail(msg);
        }

        /// <summary>
        /// Assert a 3x3 rotation block is orthogonal: R^T R = I and det≈+1.
        /// Checks orthonormal columns (Right, Up, Forward) from the matrix,
        /// then verifies dot/cross relationships and determinant sign.
        /// </summary>
        public static void IsOrthogonal(Matrix m, float eps = Epsilon, string? because = null)
        {
            var r = new Vector3(m.M11, m.M12, m.M13);
            var u = new Vector3(m.M21, m.M22, m.M23);
            var f = new Vector3(m.M31, m.M32, m.M33);

            // Unit length
            if (Math.Abs(r.Length() - 1f) > eps ||
                Math.Abs(u.Length() - 1f) > eps ||
                Math.Abs(f.Length() - 1f) > eps)
            {
                var msg = (because is null ? "" : because + Environment.NewLine) +
                          $"Axes not unit length: |r|={r.Length():G6}, |u|={u.Length():G6}, |f|={f.Length():G6}";
                Assert.Fail(msg);
            }

            // Orthogonality (dot ~ 0)
            if (Math.Abs(Vector3.Dot(r, u)) > eps ||
                Math.Abs(Vector3.Dot(r, f)) > eps ||
                Math.Abs(Vector3.Dot(u, f)) > eps)
            {
                var msg = (because is null ? "" : because + Environment.NewLine) +
                          $"Axes not orthogonal: ru={Vector3.Dot(r, u):G6}, rf={Vector3.Dot(r, f):G6}, uf={Vector3.Dot(u, f):G6}";
                Assert.Fail(msg);
            }

            // Right-handedness via cross product and determinant ≈ +1
            var rc = Vector3.Cross(r, u);
            var det = Vector3.Dot(r, Vector3.Cross(u, f));

            if ((rc - f).Length() > 5 * eps || det < 1f - 5 * eps)
            {
                var msg = (because is null ? "" : because + Environment.NewLine) +
                          $"Handedness/determinant issue: |r×u - f|={(rc - f).Length():G6}, det={det:G6}";
                Assert.Fail(msg);
            }
        }

        #endregion
    }
}
