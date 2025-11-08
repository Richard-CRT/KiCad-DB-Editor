using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Model.Loaders
{
    public interface IUpgrader
    {
        public static abstract void Upgrade(string projectFilePath);
    }
}
