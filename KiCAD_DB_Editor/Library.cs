using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor
{
    public class Library : NotifyObject
    {
        private string? _name = null;
        [JsonPropertyName("name")]
        public string Name {
            get { Debug.Assert(_name is not null); return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _description = null;
        [JsonPropertyName("description")]
        public string Description
        {
            get { Debug.Assert(_description is not null); return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;

                    InvokePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Exists only to get the WPF designer to believe I can use this object as DataContext
        /// </summary>
        public Library()
        {
            Name = "";
            Description = "";
        }

        public Library(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
