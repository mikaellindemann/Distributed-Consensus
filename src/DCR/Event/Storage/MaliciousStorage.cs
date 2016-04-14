using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Event.Interfaces;

namespace Event.Storage
{
    public class MaliciousStorage : IMaliciousStorage
    {
        private readonly IEventContext _context;

        /// <summary>
        /// Constructor used for dependency injection (used for testing purposes)
        /// </summary>
        /// <param name="context">Context to be used by EventStorage</param>
        public MaliciousStorage(IEventContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            _context = context;
        }

        public void Dispose()
        {
            _context.Dispose();

        }
    }
}