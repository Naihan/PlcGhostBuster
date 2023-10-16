using PlcGhostBuster.Entities;
using PlcGhostBuster.Interfaces;
using PlcGhostBuster.Services;
using System.Management.Automation;

namespace PlcGhostBuster.Cmdlets
{
    [Cmdlet(VerbsCommon.Set, "PlcTagValue")]
    public class SetPlcTagValueCmdlet : PSCmdlet
    {
        private readonly IPlcGhostBusterService _service;

        public SetPlcTagValueCmdlet()
        {
            _service = new PlcGhostBusterService();
        }

        [Parameter(Position = 0)]
        public QuantumTag Tag { get; set; }

        [Parameter(Position = 1)]
        public dynamic TagValue { get; set; }

        protected override void EndProcessing()
        {
            var results = _service.SetTagValue(this.Tag, this.TagValue);

            //await for results
            results.Wait();

            if (results.IsFaulted)
                this.WriteError(new ErrorRecord(results.Exception,
                                    ErrorCategory.InvalidOperation.ToString(),
                                    ErrorCategory.InvalidOperation, null));

            base.EndProcessing();
        }
    }
}