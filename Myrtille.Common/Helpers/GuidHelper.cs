using System;

namespace Myrtille.Helpers
{
    public static class GuidHelper
    {
        public static Guid ConvertFromString(string value)
        {
            Guid guid = Guid.Empty;
            if (!string.IsNullOrEmpty(value))
            {
                Guid.TryParse(value, out guid);
            }
            return guid;
        }
    }
}