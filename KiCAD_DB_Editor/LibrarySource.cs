using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor
{
    public class LibrarySource : NotifyObject
    {
        private string? _type = null;
        [JsonPropertyName("type")]
        public string Type
        {
            get { Debug.Assert(_type is not null); return _type; }
            set
            {
                if (_type != value)
                {
                    _type = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _dSN = null;
        [JsonPropertyName("dsn")]
        public string DSN
        {
            get { Debug.Assert(_dSN is not null); return _dSN; }
            set
            {
                if (_dSN != value)
                {
                    _dSN = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _username = null;
        [JsonPropertyName("username")]
        public string Username
        {
            get { Debug.Assert(_username is not null); return _username; }
            set
            {
                if (_username != value)
                {
                    _username = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _password = null;
        [JsonPropertyName("password")]
        public string Password
        {
            get { Debug.Assert(_password is not null); return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;

                    InvokePropertyChanged();
                }
            }
        }

        private int? _timeOutSeconds = null;
        [JsonPropertyName("time_out_seconds")]
        public int TimeOutSeconds
        {
            get { Debug.Assert(_timeOutSeconds is not null); return _timeOutSeconds.Value; }
            set
            {
                if (_timeOutSeconds != value)
                {
                    _timeOutSeconds = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _connectionString = null;
        [JsonPropertyName("connection_string")]
        public string ConnectionString
        {
            get { Debug.Assert(_connectionString is not null); return _connectionString; }
            set
            {
                if (_connectionString != value)
                {
                    _connectionString = value;

                    InvokePropertyChanged();
                }
            }
        }

        public LibrarySource()
        {
            Type = "odbc";
            DSN = "";
            Username = "";
            Password = "";
            TimeOutSeconds = 2;
            ConnectionString = "";
        }

        public LibrarySource(KiCADDBL_Source kiCADDBL_Source) : this()
        {
            if (kiCADDBL_Source.Type is not null) Type = kiCADDBL_Source.Type;
            if (kiCADDBL_Source.DSN is not null) DSN = kiCADDBL_Source.DSN;
            if (kiCADDBL_Source.Username is not null) Username = kiCADDBL_Source.Username;
            if (kiCADDBL_Source.Password is not null) Password = kiCADDBL_Source.Password;
            if (kiCADDBL_Source.TimeOutSeconds is not null) TimeOutSeconds = kiCADDBL_Source.TimeOutSeconds.Value;
            if (kiCADDBL_Source.ConnectionString is not null) ConnectionString = kiCADDBL_Source.ConnectionString;
        }
    }
}
