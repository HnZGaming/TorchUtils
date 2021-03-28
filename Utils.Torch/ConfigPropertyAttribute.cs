using System;
using VRage.Game.ModAPI;

namespace Utils.Torch
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigPropertyAttribute : Attribute
    {
        public ConfigPropertyAttribute(MyPromoteLevel level)
        {
            Level = level;
        }

        public MyPromoteLevel Level { get; }

        public bool IsVisibleTo(MyPromoteLevel promoLevel)
        {
            return promoLevel >= Level;
        }
    }
}