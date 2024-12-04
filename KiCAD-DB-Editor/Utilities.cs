using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor
{
    public static class Utilities
    {
        public static HashSet<char> SafeCategoryCharacters = new HashSet<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghjiklmnopqrstuvwxyz0123456789_-& ");
        public static HashSet<char> SafeParameterCharacters = new HashSet<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghjiklmnopqrstuvwxyz0123456789_-& ");
    }
}
