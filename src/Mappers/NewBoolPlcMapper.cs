using libplctag;
using libplctag.DataTypes;
using System;
using System.Linq;

namespace PlcGhostBuster.Mappers
{
    public class NewBoolPlcMapper : IPlcMapper<bool>
    {
        public PlcType PlcType
        {
            get;
            set;
        }

        public int? ElementSize => 1;

        public int[] ArrayDimensions
        {
            get;
            set;
        }

        public bool Decode(Tag tag)
        {
            return tag.GetInt8(0) == 1;
        }

        public void Encode(Tag tag, bool value)
        {
            tag.SetInt8(0, Convert.ToSByte(value ? 1 : 0));
        }

        public int? GetElementCount()
        {
            if (ArrayDimensions == null)
            {
                return null;
            }

            return (int)Math.Ceiling(ArrayDimensions.Aggregate(1, (x, y) => x * y) / 32.0);
        }
    }
}