/*
    Copyright 2010 - Adrian Hills

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSCodePointDataImport
{
    /// <summary>
    /// Parses / holds Command Line arguments. Deals with the common set of required command line args.
    /// Import-type-specific args are parsed by specific derived classes for that specific type.
    /// e.g. currently, importing of Code-Point data is supported - CodePointArgParser is used to parse any Code-Point
    /// specific arguments in addition to this base parsing functionality.
    /// </summary>
    abstract class CommandLineArgs
    {
        /// <summary>
        /// What type of data want to import:
        /// CODEPOINT
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// SQL Server name.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Name of schema the table should belong to.
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Database name.
        /// </summary>
        public string DBName { get; set; }

        /// <summary>
        /// Name of table to load data to.
        /// </summary>
        public string TableName { get; set; }

       
        /// <summary>
        /// Parse the array of command line arguments.
        /// </summary>
        /// <param name="args">command line arguments:
        /// [0] = SQL Server name
        /// [1] = database name
        /// [2] = table schema name
        /// [3] = table name
        /// [4] = directory containing the Ordnance Survey Code-Point CSV data files
        /// </param>
        public virtual void Parse(string[] args)
        {
            int numOfArgs = args.Length;
            if (numOfArgs >= 1) ServerName = args[1];
            if (numOfArgs >= 2) DBName = args[2];
            if (numOfArgs >= 3) SchemaName = args[3];
            if (numOfArgs >= 4) TableName = args[4];                             
        }

        /// <summary>
        /// Validate the command line arguments supplied.
        /// </summary>
        internal virtual void Validate()
        {
            if (String.IsNullOrWhiteSpace(ServerName)) throw new ArgumentException("ServerName argument must be supplied");
            if (String.IsNullOrWhiteSpace(DBName)) throw new ArgumentException("DBName argument must be supplied");
            if (String.IsNullOrWhiteSpace(TableName)) throw new ArgumentException("TableName argument must be supplied");         
        }
    }
}
