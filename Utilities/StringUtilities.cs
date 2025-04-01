using System.Text;

namespace PracticeAgent.Utilities
{
    public static class StringUtilities
    {
        // Extension method for StringBuilder to append a line with <br/> as a line break.
        public static string AppendBr(string value)
        {
            StringBuilder sb = new StringBuilder();
            return sb.Append(value).Append("<br/>").ToString();
        }
    }
}