using libplctag;
using PlcGhostBuster.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlcGhostBuster.Interfaces
{
    public interface IPlcGhostBusterService
    {
        Task<IEnumerable<QuantumTag>> GetControllerTags(string gateway, string path, PlcType plcType, Protocol protocol, string pattern);

        Task<dynamic> GetTagValue(QuantumTag tag);

        Task<bool> GetTagBoolValue(QuantumTag tag);

        Task<sbyte> GetTagSbyteValue(QuantumTag tag);

        Task<short> GetTagShortValue(QuantumTag tag);

        Task<int> GetTagIntValue(QuantumTag tag);

        Task<long> GetTagLongValue(QuantumTag tag);

        Task<float> GetTagFloatValue(QuantumTag tag);

        Task<double> GetTagDoubleValue(QuantumTag tag);

        Task<string> GetTagStringValue(QuantumTag tag);

        Task SetTagValue(QuantumTag tag, dynamic value);

        Task SetTagBoolValue(QuantumTag tag, bool value);

        Task SetTagSbyteValue(QuantumTag tag, sbyte value);

        Task SetTagShortValue(QuantumTag tag, short value);

        Task SetTagIntValue(QuantumTag tag, int value);

        Task SetTagLongValue(QuantumTag tag, long value);

        Task SetTagFloatValue(QuantumTag tag, float value);

        Task SetTagDoubleValue(QuantumTag tag, double value);

        Task SetTagStringValue(QuantumTag tag, string value);
    }
}