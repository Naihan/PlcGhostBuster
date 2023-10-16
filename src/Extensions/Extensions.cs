using libplctag.DataTypes;
using System.Linq;

namespace PlcGhostBuster.Extensions
{
    public static class Extensions
    {
        public static bool ShouldDestruct(this UdtFieldInfo info, ushort type)
        {
            var returnValue = true;

            if (type == 36814)//if string do not nest
                returnValue = false;

            if (type == 45006)//if string[] do not nest
                returnValue = false;

            return returnValue;
        }

        public static string GetParentName(this UdtFieldInfo info)
        {
            //fix tag name
            var tagName = info.Name;
            var tagNameParts = tagName.Split('.');
            return tagName.Substring(0, tagName.Length - tagNameParts[tagNameParts.Count() - 1].Length - 1);
        }
    }
}