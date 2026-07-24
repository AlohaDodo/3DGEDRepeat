using System;

namespace GDEngine.Core.Gameplay
{
    /// <summary>
    /// Contract for a reusable, composable game condition.
    /// </summary>
    public interface IGameCondition
    {
        bool IsSatisfied();

        string Description { get; }
    }

    /// <summary>
    /// Leaf condition that wraps an arbitrary predicate.
    /// </summary>
    public sealed class PredicateCondition : IGameCondition
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly Func<bool> _predicate;
        private readonly string _description;
        #endregion

        #region Properties
        public string Description
        {
            get { return _description; }
        }
        #endregion

        #region Constructors
        public PredicateCondition(string description, Func<bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _predicate = predicate;
            _description = string.IsNullOrWhiteSpace(description)
                ? "PredicateCondition"
                : description;
        }
        #endregion

        #region Methods
        public bool IsSatisfied()
        {
            return _predicate();
        }
        #endregion

        #region Lifecycle Methods
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return "PredicateCondition(" + _description + ")";
        }
        #endregion
    }

    /// <summary>
    /// Base class for conditions composed of child conditions (Composite pattern).
    /// </summary>
    /// <see cref="IGameCondition"/>
    public abstract class CompositeCondition : IGameCondition
    {
        #region Fields
        private readonly IGameCondition[] _children;
        private readonly string _description;
        #endregion

        #region Properties
        /// <summary>
        /// Human-readable description of this condition.
        /// </summary>
        public string Description
        {
            get { return _description; }
        }

        /// <summary>
        /// Child conditions in this composite, exposed for debug / inspection.
        /// </summary>
        public IReadOnlyList<IGameCondition> ChildConditions
        {
            get { return _children; }
        }

        /// <summary>
        /// Internal child span used by subclasses.
        /// </summary>
        protected ReadOnlySpan<IGameCondition> Children
        {
            get { return _children; }
        }
        #endregion

        #region Constructors
        protected CompositeCondition(string description, params IGameCondition[] children)
        {
            if (children == null)
                throw new ArgumentNullException(nameof(children));

            _children = children;
            _description = string.IsNullOrWhiteSpace(description)
                ? GetType().Name
                : description;
        }
        #endregion

        #region Methods
        public abstract bool IsSatisfied();
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return GetType().Name + "(" + _description + ")";
        }
        #endregion
    }

    /// <summary>
    /// Logical AND over a set of child conditions.
    /// </summary>
    public sealed class AndCondition : CompositeCondition
    {
        #region Constructors
        public AndCondition(string description, params IGameCondition[] children)
            : base(description, children)
        {
        }
        #endregion

        #region Methods
        public override bool IsSatisfied()
        {
            ReadOnlySpan<IGameCondition> span = Children;
            for (int i = 0; i < span.Length; i++)
            {
                if (!span[i].IsSatisfied())
                    return false;
            }

            return true;
        }
        #endregion
    }

    /// <summary>
    /// Logical OR over a set of child conditions.
    /// </summary>
    public sealed class OrCondition : CompositeCondition
    {
        #region Constructors
        public OrCondition(string description, params IGameCondition[] children)
            : base(description, children)
        {
        }
        #endregion

        #region Methods
        public override bool IsSatisfied()
        {
            ReadOnlySpan<IGameCondition> span = Children;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i].IsSatisfied())
                    return true;
            }

            return false;
        }
        #endregion
    }

    /// <summary>
    /// Logical NOT wrapper for a single condition.
    /// </summary>
    public sealed class NotCondition : IGameCondition
    {
        #region Fields
        private readonly IGameCondition _inner;
        private readonly string _description;
        #endregion

        #region Properties
        public string Description
        {
            get { return _description; }
        }
        #endregion

        #region Constructors
        public NotCondition(IGameCondition inner, string? description = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _description = string.IsNullOrWhiteSpace(description)
                ? "NOT(" + inner.Description + ")"
                : description;
        }
        #endregion

        #region Methods
        public bool IsSatisfied()
        {
            return !_inner.IsSatisfied();
        }
        #endregion

        #region Lifecycle Methods
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return "NotCondition(" + _description + ")";
        }
        #endregion
    }
}
