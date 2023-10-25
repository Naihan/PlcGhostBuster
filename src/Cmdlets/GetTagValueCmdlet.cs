using PlcGhostBuster.Entities;
using PlcGhostBuster.Interfaces;
using PlcGhostBuster.Services;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PlcGhostBuster.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "PlcTagValue")]
    public class GetPlcTagValueCmdlet : Cmdlet
    {
        private readonly IPlcGhostBusterService _service;

        public GetPlcTagValueCmdlet()
        {
            _service = new PlcGhostBusterService();
        }

        [Parameter(Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            Mandatory = true)]
        public QuantumTag[] Tag { get; set; }

        protected override void ProcessRecord()
        {
            foreach (var tag in Tag)
            {
                var singleResult = _service.GetTagValue(tag);
                singleResult.Wait();

                if (singleResult.IsFaulted)
                    this.WriteError(new ErrorRecord(singleResult.Exception,
                                        ErrorCategory.InvalidOperation.ToString(),
                                        ErrorCategory.InvalidOperation, null));
                else
                    this.WriteObject(singleResult.Result);
            }
        }
    }
}