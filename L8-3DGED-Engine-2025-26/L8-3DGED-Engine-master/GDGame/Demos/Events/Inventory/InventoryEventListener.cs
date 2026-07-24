#nullable enable
using GDEngine.Core.Components;
using GDEngine.Core.Services;
using System;
using System.Collections.Generic;

namespace GDGame.Demos
{
    /// <summary>
    /// Subscribes to <see cref="InventoryEvent"/> and mutates a simple local inventory model.
    /// </summary>
    /// <see cref="InventoryEvent"/>
    public sealed class InventoryEventListener : Component
    {
        #region Fields
        private IDisposable? _sub;
        private readonly Dictionary<string, int> _items = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region Lifecycle Methods
        /// <summary>
        /// Subscribe to inventory events for this specific player object.
        /// </summary>
        protected override void Awake()
        {
            if (EngineContext.Instance == null)
                throw new NullReferenceException(nameof(EngineContext));

            _sub = EngineContext.Instance.Events.Subscribe<InventoryEvent>(OnInventoryEvent);
        }

        /// <summary>
        /// Unsubscribe on teardown.
        /// </summary>
        protected override void OnDestroy()
        {
            _sub?.Dispose();
            _sub = null;
        }
        #endregion

        #region Methods
        private void OnInventoryEvent(InventoryEvent e)
        {
            if (e.Player != GameObject)
                return;

            // Add/remove by quantity
            if (e.IsAdd)
            {
                if (!_items.ContainsKey(e.ItemId))
                    _items[e.ItemId] = 0;

                _items[e.ItemId] += e.Quantity;
                System.Diagnostics.Debug.WriteLine($"[Inventory] +{e.Quantity} {e.ItemId} (total={_items[e.ItemId]})");
            }
            else
            {
                if (!_items.TryGetValue(e.ItemId, out var count))
                    return;

                var newCount = Math.Max(0, count - e.Quantity);
                if (newCount == 0) _items.Remove(e.ItemId);
                else _items[e.ItemId] = newCount;

                System.Diagnostics.Debug.WriteLine($"[Inventory] -{e.Quantity} {e.ItemId} (total={newCount})");
            }
        }
        #endregion
    }
}
