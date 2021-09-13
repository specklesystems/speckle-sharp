using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.ExceptionServices;
using ETABSv1;

namespace ConnectorETABSShared.Util
{
    public class ConnectorETABSDocument : IDisposable
    {
        private bool disposed = false;
        public cSapModel Document;

        ~ConnectorETABSDocument()
        {
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        [HandleProcessCorruptedStateExceptions]
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                Document = null;
            }
        }
    }
}

