using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Exceptions
{
    // This exception exists purely to tell VS studio not to break on validation exceptions
    // which are used to gracefully tell user of validation problems
    public class ArgumentValidationException : ArgumentException
    {
        public ArgumentValidationException(string message) : base(message) { }
    }
}
