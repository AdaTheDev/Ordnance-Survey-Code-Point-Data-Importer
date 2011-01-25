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
    /// Parses / holds Command Line arguments.
    /// </summary>
    class CommandLineArgs
    {
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
        /// Directory containing the downloaded Ordnance Survey Code-Point CSV data files.
        /// </summary>
        public string DataFileDirectory { get; set; }

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
        public void Parse(string[] args)
        {
            for (int argNo = 0; argNo < args.Length; argNo++)
            {
                switch (argNo)
                {
                    case 0:
                        ServerName = args[argNo];
                        break;
                    case 1:
                        DBName = args[argNo];
                        break;
                    case 2:
                        SchemaName = args[argNo];
                        break;
                    case 3:
                        TableName = args[argNo];
                        break;
                    case 4:
                        DataFileDirectory = args[argNo];
                        break;
                }
            }
            
            if (String.IsNullOrWhiteSpace(ServerName)) throw new ArgumentException("ServerName argument must be supplied");
            if (String.IsNullOrWhiteSpace(DBName)) throw new ArgumentException("DBName argument must be supplied");
            if (String.IsNullOrWhiteSpace(TableName)) throw new ArgumentException("TableName argument must be supplied");
            if (String.IsNullOrWhiteSpace(DataFileDirectory)) throw new ArgumentException("DataFileDirectory argument must be supplied");
        }
    }
}
