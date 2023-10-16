using libplctag;
using libplctag.DataTypes;
using libplctag.DataTypes.Simple;
using PlcGhostBuster.Interfaces;
using PlcGhostBuster.Entities;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using PlcGhostBuster.Extensions;
using PlcGhostBuster.Mappers;

namespace PlcGhostBuster.Services
{
    public class PlcGhostBusterService : IPlcGhostBusterService
    {
        private ITag GetMapper(TagType tagType)
        {
            ITag result;

            switch (tagType)
            {
                case TagType.BOOL:
                    result = new Tag<NewBoolPlcMapper, bool>();
                    break;

                case TagType.BOOL_ARRAY_1D:
                    result = new Tag<BoolPlcMapper, bool[]>();
                    break;

                case TagType.SBYTE:
                    result = new Tag<SintPlcMapper, sbyte>();
                    break;

                case TagType.SBYTE_ARRAY_1D:
                    result = new Tag<SintPlcMapper, sbyte[]>();
                    break;

                case TagType.SHORT:
                    result = new Tag<IntPlcMapper, short>();
                    break;

                case TagType.SHORT_ARRAY_1D:
                    result = new Tag<IntPlcMapper, short[]>();
                    break;

                case TagType.INT:
                    result = new Tag<DintPlcMapper, int>();
                    break;

                case TagType.INT_ARRAY_1D:
                    result = new Tag<DintPlcMapper, int[]>();
                    break;

                case TagType.LONG:
                    result = new Tag<LintPlcMapper, long>();
                    break;

                case TagType.LONG_ARRAY_1D:
                    result = new Tag<LintPlcMapper, long[]>();
                    break;

                case TagType.FLOAT:
                    result = new Tag<RealPlcMapper, float>();
                    break;

                case TagType.FLOAT_ARRAY_ID:
                    result = new Tag<RealPlcMapper, float[]>();
                    break;

                case TagType.DOUBLE:
                    result = new Tag<LrealPlcMapper, double>();
                    break;

                case TagType.DOUBLE_ARRAY_ID:
                    result = new Tag<LrealPlcMapper, double[]>();
                    break;

                case TagType.STRING:
                    result = new TagString();
                    break;

                case TagType.STRING_ARRAY_1D:
                    result = new TagString1D();
                    break;

                default:
                    result = null;
                    break;
            }

            return result;
        }

        private bool DecodeUdtBool(QuantumTag tag)
        {
            var controllerTag = GetControllerTag(tag.Gateway, tag.Path, tag.PlcType, tag.Protocol, tag.BaseName);
            var packedBools = controllerTag.GetInt32((int)tag.Offset);
            var bools = new BitArray(new int[] { packedBools });
            return bools[(int)tag.Index];
        }

        private IEnumerable<QuantumTag> UdtCutter(string gateway, string path, PlcType plcType, Protocol protocol, string pattern, UdtFieldInfo fieldInfo)
        {
            var readList = new List<QuantumTag>();

            //create the travers list
            var traversList = new List<UdtFieldInfo>();

            //create bool dictonary to handle bools
            var boolDictionary = new Dictionary<uint, List<UdtFieldInfo>>();

            //add first element
            traversList.Add(fieldInfo);
            // travers DF
            while (traversList.Count != 0)
            {
                //take the first element
                var parentNode = traversList[0];
                traversList.RemoveAt(0);

                //get the udt
                var udtTag = GetUdtFromType(gateway, path, plcType, protocol, parentNode.Type);

                //if the udt is an array
                if (parentNode.Metadata > 0)
                {
                    //insert members to traverse list
                    for (int i = 0; i < parentNode.Metadata; i++)
                    {
                        var memberName = $"{parentNode.Name}[{i}]";
                        traversList.Add(new UdtFieldInfo()
                        {
                            Name = memberName,
                            Type = parentNode.Type,
                            Offset = parentNode.Offset
                        });
                    }
                }
                else
                {
                    //fix tag prefix for all paths
                    foreach (var udt in udtTag.Value.Fields)
                        udt.Name = $"{parentNode.Name}.{udt.Name}";

                    //get the regular tags and the udt that dont need distruct
                    var regularFieldTags = udtTag.Value.Fields.Where(f => !TagIsUdt(f.Type) || TagIsUdt(f.Type) && !f.ShouldDestruct(f.Type));
                    var udtsNeedProcessing = udtTag.Value.Fields.Where(f => TagIsUdt(f.Type) && f.ShouldDestruct(f.Type));

                    //add to travers list
                    traversList.InsertRange(0, udtsNeedProcessing);

                    //handle UDT

                    foreach (var field in regularFieldTags)
                    {
                        //skip junk
                        if (field.Name.Contains("ZZZZZZZZZZ"))
                            continue;

                        //if dose not match patten
                        if (!string.IsNullOrEmpty(pattern))
                            if (!field.Name.ToLower().Contains(pattern.ToLower()))
                                continue;

                        //get the tag dype
                        var tagType = (TagType)Enum.ToObject(typeof(TagType), field.Type);

                        if (tagType == TagType.BOOL) //get all combined bools
                        {
                            if (!boolDictionary.ContainsKey(field.Offset))
                                boolDictionary.Add(field.Offset, new List<UdtFieldInfo>());
                            //change field type for parser handler
                            field.Type = (ushort)TagType.UDT_PACKD_BOOL;

                            boolDictionary[field.Offset].Add(field);
                        }
                        else if (tagType == TagType.BOOL_ARRAY_1D) //bool array needs to expand
                        {
                            var tagCounter = 0;
                            for (int i = 0; i < field.Metadata; i++)
                                for (int j = 0; j < 32; j++)
                                    readList.Add(new QuantumTag()
                                    {
                                        Gateway = gateway,
                                        Path = path,
                                        PlcType = plcType,
                                        Protocol = protocol,
                                        BaseName = field.GetParentName(),
                                        Name = $"{field.Name}[{tagCounter++}]",
                                        TagType = TagType.UDT_PACKD_BOOL,
                                        Dimensions = 0,
                                        Index = j,
                                        Offset = (int)field.Offset
                                    });
                        }
                        else if (field.Metadata > 0) //if array of some sort
                        {
                            //add array sign to base name
                            for (int i = 0; i < field.Metadata; i++)
                                readList.Add(new QuantumTag()
                                {
                                    Gateway = gateway,
                                    Path = path,
                                    PlcType = plcType,
                                    Protocol = protocol,
                                    BaseName = $"{field.Name}[0]",
                                    Name = $"{field.Name}[{i}]",
                                    TagType = tagType,
                                    Dimensions = field.Metadata,
                                    Index = i,
                                });
                        }
                        else //all others
                        {
                            readList.Add(new QuantumTag()
                            {
                                Gateway = gateway,
                                Path = path,
                                PlcType = plcType,
                                Protocol = protocol,
                                BaseName = field.Name,
                                Name = field.Name,
                                TagType = tagType,
                                Dimensions = 0
                            });
                        }
                    }
                }
            }

            //process bools
            foreach (var kv in boolDictionary)
            {
                for (int i = 0; i < kv.Value.Count(); i++)
                {
                    var field = kv.Value[i];
                    readList.Add(new QuantumTag()
                    {
                        Gateway = gateway,
                        Path = path,
                        PlcType = plcType,
                        Protocol = protocol,
                        BaseName = field.GetParentName(),
                        Name = field.Name,
                        TagType = TagType.UDT_PACKD_BOOL,
                        Dimensions = 0,
                        Index = i,
                        Offset = (int)field.Offset
                    });
                }
            }

            return readList;
        }

        private IEnumerable<QuantumTag> TagCutter(string gateway, string path, PlcType plcType, Protocol protocol, string pattern, TagInfo tag)
        {
            //if dose not match patten
            if (!string.IsNullOrEmpty(pattern))
                if (!tag.Name.ToLower().Contains(pattern.ToLower()))
                    return Enumerable.Empty<QuantumTag>();

            var readList = new List<QuantumTag>();
            var tagType = (TagType)Enum.ToObject(typeof(TagType), tag.Type);
            //if array
            if (tag.Dimensions[0] > 0)
            {
                //if its a bool array we need  to multiply by 32
                var dimensions = tagType == TagType.BOOL_ARRAY_1D ? tag.Dimensions[0] * 32 : tag.Dimensions[0];
                var baseName = $"{tag.Name}[0]";
                for (int i = 0; i < dimensions; i++)
                {
                    var tagName = $"{tag.Name}[{i}]";
                    readList.Add(new QuantumTag()
                    {
                        Gateway = gateway,
                        Path = path,
                        PlcType = plcType,
                        Protocol = protocol,
                        BaseName = baseName,
                        Name = tagName,
                        TagType = tagType,
                        Dimensions = (int)dimensions,
                        Index = i,
                    });
                }
            }
            else
                readList.Add(new QuantumTag()
                {
                    Gateway = gateway,
                    Path = path,
                    PlcType = plcType,
                    Protocol = protocol,
                    BaseName = tag.Name,
                    Name = tag.Name,
                    TagType = tagType,
                    Dimensions = 0
                });

            return readList;
        }

        private Tag<UdtInfoPlcMapper, UdtInfo> GetUdtFromType(string gateway, string path, PlcType plcType, Protocol protocol, ushort tagType)
        {
            //get the root udt DataType
            var udtTag = new Tag<UdtInfoPlcMapper, UdtInfo>()
            {
                Gateway = gateway,
                Path = path,
                PlcType = plcType,
                Protocol = protocol,
                Name = $"@udt/{GetUdtId(tagType)}",
            };
            udtTag.Read();

            return udtTag;
        }

        private Tag GetControllerTag(string gateway, string path, PlcType plcType, Protocol protocol, string tagName)
        {
            var tag = new Tag()
            {
                Gateway = gateway,
                Path = path,
                PlcType = plcType,
                Protocol = protocol,
                Name = tagName
            };

            tag.Initialize();

            return tag;
        }

        private IEnumerable<TagInfo> GetAllControllerTags(string gateway, string path, PlcType plcType, Protocol protocol)
        {
            var tags = new Tag<TagInfoPlcMapper, TagInfo[]>()
            {
                Gateway = gateway,
                Path = path,
                PlcType = plcType,
                Protocol = protocol,
                Name = "@tags",
                Timeout = TimeSpan.FromSeconds(10)
            };

            tags.Read();
            //filter out theprograms
            return tags.Value
                .Where(x => !TagIsProgram(x));
        }

        private bool TagIsUdt(ushort tagType)
        {
            const ushort TYPE_IS_STRUCT = 0x8000;
            const ushort TYPE_IS_SYSTEM = 0x1000;

            return (tagType & TYPE_IS_STRUCT) != 0 && !((tagType & TYPE_IS_SYSTEM) != 0);
        }

        private int GetUdtId(ushort tagType)
        {
            const ushort TYPE_UDT_ID_MASK = 0x0FFF;
            return tagType & TYPE_UDT_ID_MASK;
        }

        private bool TagIsProgram(TagInfo tag) => tag.Name.StartsWith("Program:");

        public Task<IEnumerable<QuantumTag>> GetControllerTags(string gateway, string path, PlcType plcType, Protocol protocol, string pattern)
        {
            var readList = new List<QuantumTag>();

            var controllerTags = GetAllControllerTags(gateway, path, plcType, protocol);
            foreach (var tag in controllerTags)
            {
                try
                {
                    if (TagIsUdt(tag.Type))
                    {
                        //transform root level udt to udtfieldinfo :-)
                        var fieldInfo = new UdtFieldInfo()
                        {
                            Name = tag.Name,
                            Type = tag.Type,
                            Metadata = (ushort)tag.Dimensions[0]
                        };

                        var results = UdtCutter(gateway, path, plcType, protocol, pattern, fieldInfo);
                        readList.AddRange(results);
                    }
                    else
                    {
                        var results = TagCutter(gateway, path, plcType, protocol, pattern, tag);
                        readList.AddRange(results);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"------Error processing tag {tag.Name} of type {tag.Type} of id {tag.Id} -- {ex.Message}");
                }
            }
            return Task.FromResult<IEnumerable<QuantumTag>>(readList);
        }

        public async Task<dynamic> GetTagValue(QuantumTag tag)
        {
            //handle packed bool in udt
            if (tag.TagType == TagType.UDT_PACKD_BOOL)
            {
                var boolValue = DecodeUdtBool(tag);
                return boolValue;
            }

            //get the tag mapper
            var mapper = GetMapper(tag.TagType);
            if (mapper == null)
                throw new Exception($"Missing mapper for tag {tag.Name} of type {tag.TagType}");

            //update mapper params
            mapper.Name = tag.BaseName;
            mapper.Gateway = tag.Gateway;
            mapper.Path = tag.Path;
            mapper.Protocol = tag.Protocol;
            mapper.PlcType = tag.PlcType;

            //if array of data
            if (tag.Dimensions != 0)
                mapper.ArrayDimensions = new int[] { tag.Dimensions };

            await mapper.ReadAsync();

            dynamic printValue;

            switch (mapper.Value)
            {
                case bool boolValue:
                    printValue = boolValue;
                    break;

                case bool[] boolArrayValue:
                    printValue = boolArrayValue[(int)tag.Index];
                    break;

                case sbyte sbyteValue:
                    printValue = sbyteValue;
                    break;

                case sbyte[] sbyteArrayValue:
                    printValue = sbyteArrayValue[(int)tag.Index];
                    break;

                case short shortValue:
                    printValue = shortValue;
                    break;

                case short[] shortArrayValue:
                    printValue = shortArrayValue[(int)tag.Index];
                    break;

                case int intValue:
                    printValue = intValue;
                    break;

                case int[] intArrayValue:
                    printValue = intArrayValue[(int)tag.Index];
                    break;

                case long longValue:
                    printValue = longValue;
                    break;

                case long[] longArrayValue:
                    printValue = longArrayValue[(int)tag.Index];
                    break;

                case float floatValue:
                    printValue = floatValue;
                    break;

                case float[] floatArrayValue:
                    printValue = floatArrayValue[(int)tag.Index];
                    break;

                case double doubleValue:
                    printValue = doubleValue;
                    break;

                case double[] doubleArrayValue:
                    printValue = doubleArrayValue[(int)tag.Index];
                    break;

                case string stringValue:
                    printValue = stringValue;
                    break;

                case string[] stringArrayValue:
                    printValue = stringArrayValue[(int)tag.Index];
                    break;

                default:
                    throw new NotImplementedException("missing type");
            }

            return printValue;
        }

        public async Task<bool> GetTagBoolValue(QuantumTag tag)
        {
            //handle packed bool in udt
            if (tag.TagType == TagType.UDT_PACKD_BOOL)
            {
                var boolValue = DecodeUdtBool(tag);
                return boolValue;
            }
            else
            {
                //get the tag mapper
                var mapper = GetMapperForTag(tag);
                //read
                await mapper.ReadAsync();

                return mapper.Value.GetType() == typeof(bool[]) ?
                     ((bool[])mapper.Value)[(int)tag.Index] :
                     (bool)mapper.Value;
            }
        }

        public async Task<sbyte> GetTagSbyteValue(QuantumTag tag)
        {
            //get the tag mapper
            var mapper = GetMapperForTag(tag);
            //read
            await mapper.ReadAsync();

            return mapper.Value.GetType() == typeof(sbyte[]) ?
                 ((sbyte[])mapper.Value)[(int)tag.Index] :
                 (sbyte)mapper.Value;
        }

        public async Task<short> GetTagShortValue(QuantumTag tag)
        {
            //get the tag mapper
            var mapper = GetMapperForTag(tag);
            //read
            await mapper.ReadAsync();

            return mapper.Value.GetType() == typeof(short[]) ?
                 ((short[])mapper.Value)[(int)tag.Index] :
                 (short)mapper.Value;
        }

        public async Task<int> GetTagIntValue(QuantumTag tag)
        {
            //get the tag mapper
            var mapper = GetMapperForTag(tag);
            //read
            await mapper.ReadAsync();

            return mapper.Value.GetType() == typeof(int[]) ?
                 ((int[])mapper.Value)[(int)tag.Index] :
                 (int)mapper.Value;
        }

        public async Task<long> GetTagLongValue(QuantumTag tag)
        {
            //get the tag mapper
            var mapper = GetMapperForTag(tag);
            //read
            await mapper.ReadAsync();

            return mapper.Value.GetType() == typeof(long[]) ?
                 ((long[])mapper.Value)[(int)tag.Index] :
                 (long)mapper.Value;
        }

        public async Task<float> GetTagFloatValue(QuantumTag tag)
        {
            //get the tag mapper
            var mapper = GetMapperForTag(tag);
            //read
            await mapper.ReadAsync();

            return mapper.Value.GetType() == typeof(float[]) ?
                 ((float[])mapper.Value)[(int)tag.Index] :
                 (float)mapper.Value;
        }

        public async Task<double> GetTagDoubleValue(QuantumTag tag)
        {
            //get the tag mapper
            var mapper = GetMapperForTag(tag);
            //read
            await mapper.ReadAsync();

            return mapper.Value.GetType() == typeof(double[]) ?
                 ((double[])mapper.Value)[(int)tag.Index] :
                 (double)mapper.Value;
        }

        private ITag GetMapperForTag(QuantumTag tag)
        {
            //get the tag mapper
            var mapper = GetMapper(tag.TagType);
            if (mapper == null)
                throw new Exception($"Missing mapper for tag {tag.Name} of type {tag.TagType}");

            //update mapper params
            mapper.Name = tag.BaseName;
            mapper.Gateway = tag.Gateway;
            mapper.Path = tag.Path;
            mapper.Protocol = tag.Protocol;
            mapper.PlcType = tag.PlcType;

            //if array of data
            if (tag.Dimensions != 0)
                mapper.ArrayDimensions = new int[] { tag.Dimensions };

            return mapper;
        }

        public async Task<string> GetTagStringValue(QuantumTag tag)
        {
            //get the tag mapper
            var mapper = GetMapperForTag(tag);
            //read
            await mapper.ReadAsync();

            return mapper.Value.GetType() == typeof(string[]) ?
                 ((string[])mapper.Value)[(int)tag.Index] :
                 (string)mapper.Value;
        }

        private Task SetTagValue<T>(QuantumTag tag, T value)
        {
            //get the tag mapper
            var mapper = GetMapperForTag(tag);
            if (mapper == null)
                throw new Exception($"mapper not found for tag {tag.BaseName}");

            //validate that type is ok
            if (!mapper.Value.GetType().Equals(value.GetType()))
                throw new Exception($"value of type {value.GetType().FullName} cannot be written to tag of type {mapper.Value.GetType().FullName}");

            //write
            mapper.Value = value;
            return mapper.WriteAsync();
        }

        public Task SetTagValue(QuantumTag tag, object value)
        {
            //get the tag mapper
            var mapper = GetMapperForTag(tag);
            if (mapper == null)
                throw new Exception($"mapper not found for tag {tag.BaseName}");

            //validate that type is ok
            if (!mapper.Value.GetType().Equals(value.GetType()))
                throw new Exception($"value of type {value.GetType().FullName} cannot be written to tag of type {mapper.Value.GetType().FullName}");

            //write
            mapper.Value = value;
            return mapper.WriteAsync();
        }

        public Task SetTagBoolValue(QuantumTag tag, bool value) => SetTagValue<bool>(tag, value);

        public Task SetTagSbyteValue(QuantumTag tag, sbyte value) => SetTagValue<sbyte>(tag, value);

        public Task SetTagShortValue(QuantumTag tag, short value) => SetTagValue<short>(tag, value);

        public Task SetTagIntValue(QuantumTag tag, int value) => SetTagValue<int>(tag, value);

        public Task SetTagLongValue(QuantumTag tag, long value) => SetTagValue<long>(tag, value);

        public Task SetTagFloatValue(QuantumTag tag, float value) => SetTagValue<float>(tag, value);

        public Task SetTagDoubleValue(QuantumTag tag, double value) => SetTagValue<double>(tag, value);

        public Task SetTagStringValue(QuantumTag tag, string value) => SetTagValue<string>(tag, value);
    }
}