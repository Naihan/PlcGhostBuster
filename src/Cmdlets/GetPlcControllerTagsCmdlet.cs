using libplctag;
using PlcGhostBuster.Interfaces;
using PlcGhostBuster.Services;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PlcGhostBuster.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "PlcControllerTags")]
    public class GetPlcControllerTagsCmdlet : PSCmdlet
    {
        private readonly IPlcGhostBusterService _service;

        public GetPlcControllerTagsCmdlet()
        {
            _service = new PlcGhostBusterService();
        }

        [Parameter(Position = 0)]
        public string Gateway { get; set; }

        [Parameter(Position = 1)]
        public string Path { get; set; }

        [Parameter(Position = 2)]
        public PlcType PlcType { get; set; }

        [Parameter(Position = 3)]
        public Protocol Protocol { get; set; }

        [Parameter(Position = 4)]
        public string Pattern { get; set; }

        protected override void EndProcessing()
        {
            var results = _service.GetControllerTags(this.Gateway,
                                                        this.Path,
                                                        this.PlcType,
                                                        this.Protocol,
                                                        this.Pattern);

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