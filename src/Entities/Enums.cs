namespace PlcGhostBuster.Entities
{
    public enum TagType
    {
        //custom
        UDT_PACKD_BOOL = 1409,

        //regular

        BOOL = 193,
        BOOL_ARRAY_1D = 8403,
        SBYTE = 194,
        SBYTE_ARRAY_1D = 8386,
        SHORT = 195,
        SHORT_ARRAY_1D = 8387,
        INT = 196,
        INT_ARRAY_1D = 8388,
        LONG = 197,
        LONG_ARRAY_1D = 8389,

        //USINT = 198,
        //USINT_ARRAY_1D = 8390,
        //UINT = 199,
        //UINT_ARRAY_1D = 8390,
        //UDINT = 200,
        //UDINT_ARRAY_1D = 8392,
        //ULINT = 201,
        //ULINT_ARRAY_1D = 8393,

        FLOAT = 202,
        FLOAT_ARRAY_ID = 8394,
        DOUBLE = 203,
        DOUBLE_ARRAY_ID = 8395,
        STRING = 36814,
        STRING_ARRAY_1D = 45006
    }

    public enum TagSyncPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
    }
}