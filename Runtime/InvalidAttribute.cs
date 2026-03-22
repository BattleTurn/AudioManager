using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
	public sealed class InvalidAttribute : StringRuleAttribute
	{
		public System.Type ProviderType { get; }
		public string MemberName { get; }
		public bool AutoFix { get; }
		public string[] InlineWords { get; }

		/// <summary>
		/// Validate the string against a reserved word collection.
		/// The member can be a static field/property/method that returns IEnumerable&lt;string&gt; (HashSet/List/array are fine).
		/// </summary>
        /// <param name="providerType">The type that provides the reserved word collection.</param>
        /// <param name="memberName">The member name of the reserved word collection.</param>
        /// <param name="autoFix">Whether to automatically fix the input by adding a prefix
		public InvalidAttribute(System.Type providerType, string memberName, bool autoFix = false)
		{
			ProviderType = providerType;
			MemberName = memberName;
			AutoFix = autoFix;
			InlineWords = null;
		}

		/// <summary>
		/// Validate the string against a fixed list of reserved words.
		/// </summary>
        /// <param name="autoFix">Whether to automatically fix the input by adding a prefix</param>
        /// <param name="inlineWords">The reserved words.</param>
		public InvalidAttribute(bool autoFix = false, params string[] inlineWords)
		{
			ProviderType = null;
			MemberName = null;
			AutoFix = autoFix;
			InlineWords = inlineWords;
		}
	}
}