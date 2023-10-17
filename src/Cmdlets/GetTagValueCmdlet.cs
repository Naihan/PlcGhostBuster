using PlcGhostBuster.Entities;
using PlcGhostBuster.Interfaces;
using PlcGhostBuster.Services;
using System.Management.Automation;

namespace PlcGhostBuster.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "PlcTagValue")]
    public class GetPlcTagValueCmdlet : PSCmdlet
    {
        private readonly IPlcGhostBusterService _service;

        public GetPlcTagValueCmdlet()
        {
            _service = new PlcGhostBusterService();
        }

        [Parameter(Position = 0, ValueFromPipeline = true)]
        public QuantumTag Tag { get; set; }

        protected override void EndProcessing()
        {
            var results = _service.GetTagValue(this.Tag);

            //await for results
            results.Wait();

            if (results.IsFaulted)
                this.WriteError(new ErrorRecord(results.Exception,
                                    ErrorCategory.InvalidOperation.ToString(),
                                    ErrorCategory.InvalidOperation, null));
            else
                this.WriteObject(results.Result);

            base.EndProcessing();
        }
    }
}