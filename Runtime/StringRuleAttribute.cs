using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    /// <summary>
    /// Base attribute for string validation rules.
    /// Multiple attributes deriving from this type can be combined on the same field,
    /// and a single drawer will enforce all rules.
    /// </summary>
    public abstract class StringRuleAttribute : PropertyAttribute
    {
    }
}
