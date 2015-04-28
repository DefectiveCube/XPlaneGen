﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XPlaneGenConsole
{
    public abstract class CsvDatapoint<T> : TextDatapoint
        where T : CsvDatapoint<T>, new()
    {
        public static CsvFactory<T> Factory;

        protected static int Key;
        protected static Random r;

        public int Fields { get; protected set; }

        public abstract void Load(string value);

        public abstract void Load(string[] values);
    }

    public sealed class XmlDatapoint : TextDatapoint
    {

    }

    public sealed class JsonDatapoint : TextDatapoint
    {

    }
}
