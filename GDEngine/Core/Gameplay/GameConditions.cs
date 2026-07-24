using Microsoft.Xna.Framework;

namespace GDEngine.Core.Gameplay
{
    /// <summary>
    /// Factory helpers for composing common game conditions.
    /// </summary>
    public static class GameConditions
    {
        #region Static Fields
        #endregion

        #region Methods
        public static IGameCondition FromPredicate(string description, Func<bool> predicate)
        {
            return new PredicateCondition(description, predicate);
        }

        public static IGameCondition And(string description, params IGameCondition[] conditions)
        {
            return new AndCondition(description, conditions);
        }

        public static IGameCondition Or(string description, params IGameCondition[] conditions)
        {
            return new OrCondition(description, conditions);
        }

        public static IGameCondition Not(IGameCondition inner, string? description = null)
        {
            return new NotCondition(inner, description);
        }

        public static IGameCondition FloatLessThan(
            string description,
            Func<float> valueProvider,
            float threshold)
        {
            return new FloatComparisonCondition(
                description,
                valueProvider,
                threshold,
                FloatComparison.LessThan);
        }

        public static IGameCondition FloatGreaterThan(
            string description,
            Func<float> valueProvider,
            float threshold)
        {
            return new FloatComparisonCondition(
                description,
                valueProvider,
                threshold,
                FloatComparison.GreaterThan);
        }

        public static IGameCondition DistanceGreaterThan(
            string description,
            Func<Vector3> a,
            Func<Vector3> b,
            float threshold)
        {
            return new FloatComparisonCondition(
                description,
                () =>
                {
                    Vector3 pa = a();
                    Vector3 pb = b();
                    return Vector3.Distance(pa, pb);
                },
                threshold,
                FloatComparison.GreaterThan);
        }
        #endregion
    }
}
