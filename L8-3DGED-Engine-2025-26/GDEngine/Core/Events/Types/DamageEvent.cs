using Microsoft.Xna.Framework;

namespace GDEngine.Core.Events
{
    public class DamageEvent
    {
        public enum DamageType : sbyte
        {
            Strength,
            Speed,
            Shield
        }

        private int _amount;
        private DamageType _damageType;
        private string _sourceName;
        private string _targetName;
        private Vector3 _hitPosition;
        private bool _isCritical;

        public int Amount
        {
            get => _amount;
            set => _amount = value;
        }

        // Nicer name; you can keep DamageType1 if you need backward compat
        public DamageType Type
        {
            get => _damageType;
            set => _damageType = value;
        }

        public string SourceName
        {
            get => _sourceName;
            set => _sourceName = value;
        }

        public string TargetName
        {
            get => _targetName;
            set => _targetName = value;
        }

        public Vector3 HitPosition
        {
            get => _hitPosition;
            set => _hitPosition = value;
        }

        public bool IsCritical
        {
            get => _isCritical;
            set => _isCritical = value;
        }

        public DamageEvent(
           int amount,
           DamageType damageType)
            : this(amount, damageType, null, null,Vector3.Zero)
        {

        }
        public DamageEvent(
            int amount,
            DamageType damageType,
            string? sourceName,
            string? targetName,
            Vector3 hitPosition,
            bool isCritical = false)
        {
            _amount = amount;
            _damageType = damageType;
            _sourceName = sourceName;
            _targetName = targetName;
            _hitPosition = hitPosition;
            _isCritical = isCritical;
        }
    }
}
