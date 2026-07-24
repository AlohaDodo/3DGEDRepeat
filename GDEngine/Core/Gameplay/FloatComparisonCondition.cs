namespace GDEngine.Core.Gameplay
{
    /// <summary>
    /// Comparison operator used by <see cref="FloatComparisonCondition"/>.
    /// </summary>
    public enum FloatComparison
    {
        LessThan,
        LessOrEqual,
        GreaterThan,
        GreaterOrEqual,
        Equal,
        NotEqual
    }

    /// <summary>
    /// Generic float comparison condition built from a value provider delegate.
    /// </summary>
    public sealed class FloatComparisonCondition : IGameCondition
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly Func<float> _valueProvider;
        private readonly float _threshold;
        private readonly FloatComparison _comparison;
        private readonly float _epsilon;
        private readonly string _description;
        #endregion

        #region Properties
        public string Description
        {
            get { return _description; }
        }
        #endregion

        #region Constructors
        public FloatComparisonCondition(
            string description,
            Func<float> valueProvider,
            float threshold,
            FloatComparison comparison,
            float epsilon = 0.0001f)
        {
            if (valueProvider == null)
                throw new ArgumentNullException(nameof(valueProvider));

            _valueProvider = valueProvider;
            _threshold = threshold;
            _comparison = comparison;
            _epsilon = epsilon;
            _description = string.IsNullOrWhiteSpace(description)
                ? "FloatComparisonCondition"
                : description;
        }
        #endregion

        #region Methods
        public bool IsSatisfied()
        {
            float value = _valueProvider();

            if (_comparison == FloatComparison.LessThan)
                return value < _threshold;
            if (_comparison == FloatComparison.LessOrEqual)
                return value <= _threshold;
            if (_comparison == FloatComparison.GreaterThan)
                return value > _threshold;
            if (_comparison == FloatComparison.GreaterOrEqual)
                return value >= _threshold;
            if (_comparison == FloatComparison.Equal)
                return Math.Abs(value - _threshold) <= _epsilon;

            return Math.Abs(value - _threshold) > _epsilon;
        }
        #endregion

        #region Lifecycle Methods
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return "FloatComparisonCondition(" + _description + ")";
        }
        #endregion
    }
}
