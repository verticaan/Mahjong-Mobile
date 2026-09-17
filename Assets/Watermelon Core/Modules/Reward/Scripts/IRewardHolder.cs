namespace Watermelon
{
    public interface IRewardHolder
    {
        bool IsDirty { get; }
        void MarkAsDirty();
    }
}