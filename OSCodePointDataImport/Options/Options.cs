using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSCodePointDataImport
{
    abstract class Options
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
    }

}
