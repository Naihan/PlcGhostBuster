using libplctag;
using System;

namespace PlcGhostBuster.Entities
{
    public struct QuantumTag
    {
        public Int64 TagId { get; set; }

        public string BaseName { get; set; }

        public string Name { get; set; }
        public TagType TagType { get; set; }
        public int Dimensions { get; set; }
        public int Index { get; set; }
        public int Offset { get; set; }

        public string Gateway { get; set; }

        public string Path { get; set; }
        public PlcType PlcType { get; set; }
        public Protocol Protocol { get; set; }
        public TagSyncPriority SyncPriority { get; set; }
    }

    public struct TagResults
    {
        public string BaseName { get; set; }
        public string Name { get; set; }
        public string TagType { get; set; }
        public int Dimensions { get; set; }
        public int Index { get; set; }
        public int Offset { get; set; }
        public string Gateway { get; set; }
        public string Path { get; set; }
        public string PlcType { get; set; }
        public string Protocol { get; set; }
    }

    public struct RegisteredTag
    {
        public Int64 TagId { get; set; }
        public string Name { get; set; }
        public string TagType { get; set; }
        public string SyncPriority { get; set; }
    }
}