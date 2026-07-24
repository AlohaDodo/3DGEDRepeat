using System.Collections.Generic;
using GDEngine.Core.Components;
using GDEngine.Core.Enums;
using GDEngine.Core.Systems;

namespace GDGame.Demos.Systems
{
    public class EnemyCullingSystem : SystemBase
    {
        public EnemyCullingSystem(int order = 0) 
            : base(FrameLifecycle.LateUpdate, order)
        {
        }

        protected override void OnAdded()
        {
            var enemyComponents = new List<Component>();
            for (int i = 0; i < Scene.GameObjects.Count; i++)
            {
                var enemyComponent = Scene.GameObjects[i].GetComponent<Transform>();
                if (enemyComponent != null)
                    enemyComponents.Add(enemyComponent);
            }

            base.OnAdded();
        }
    }
}
