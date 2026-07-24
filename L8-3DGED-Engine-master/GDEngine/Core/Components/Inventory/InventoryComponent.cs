using GDEngine.Core.Events;

namespace GDEngine.Core.Components
{
    public enum ItemType : byte
    {
        Weapon, Health, Lore, Info
    }
    public class InventoryComponent : Component
    {
        private EventBus? _eventBus;
        private Dictionary<ItemType, int> _inventory = new();
      //  private Dictionary<ItemType, List<InventoryItem> _inventory = new();
        protected override void Start()
        {
            if (GameObject == null || GameObject.Scene == null)
                throw new NullReferenceException("GameObject or Scene is null");

            _eventBus = GameObject.Scene.Context.Events;

            _eventBus.On<InventoryEvent>()
                .Do(e => Add(e.ItemType, e.Value));
        }
        public void Add(ItemType type, int delta)
        {
            int value;
            _inventory.TryGetValue(type, out value);
            _inventory.Add(type, value + delta);
        }
    }
    public class InventoryEvent
    {
        public ItemType ItemType { get; set; }
        public int Value { get; set; }
    }
}
