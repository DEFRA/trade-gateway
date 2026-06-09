namespace Api.Extensions
{
    internal static class StringExtensions
    {
        private const char PathSeparatorToken = '_';
        private const char PathSeparator = '/';

        internal static string ToNodeId(this string nodePath)
        {
            return nodePath.Replace(PathSeparator, PathSeparatorToken);
        }

        internal static string ToNodePath(this string nodeId)
        {
            return nodeId.Replace(PathSeparatorToken, PathSeparator);
        }
    }
}
