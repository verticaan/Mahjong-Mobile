namespace Watermelon
{
    public class ManipulateDockBuff : CardBuffEffectBase
    {
        private DockBehavior dockBehavior;
        public override void Init()
        {
            dockBehavior = LevelController.Dock;
        }

        protected override void OnApplyBuff()
        {
            throw new System.NotImplementedException();
        }
    }
}
