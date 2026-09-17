using System;

namespace Watermelon
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RegisterRewardAttribute : Attribute
    {
        public Type ViewType { get; }

        public RegisterRewardAttribute(Type viewType)
        {
            ViewType = viewType;
        }
    }
}
