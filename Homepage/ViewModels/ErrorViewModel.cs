using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Homepage.ViewModels
{
    public class ErrorViewModel
    {
        public string RequestId { get; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public ErrorViewModel(string requestId)
        {
            RequestId = requestId;
        }
    }
}
