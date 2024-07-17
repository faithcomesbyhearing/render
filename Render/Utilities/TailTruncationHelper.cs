namespace Render.Utilities
{
	internal class TailTruncationHelper
	{
		private const int MaximumLength = 32;
		private const string Truncation = "...";

		internal static string AddTailTruncation(string text)
		{
			if (!string.IsNullOrEmpty(text))
			{
				if (text.Length > MaximumLength)
				{
				   return text.Substring(0, MaximumLength) + Truncation;
				}

				return text;
			}

			return string.Empty;
		}
	}
}
