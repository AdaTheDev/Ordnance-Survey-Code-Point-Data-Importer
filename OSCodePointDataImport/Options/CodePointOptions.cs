using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSCodePointDataImport
{
    class CodePointOptions : Options
    {
        /// <summary>
        /// Directory containing the downloaded Ordnance Survey Code-Point CSV data files.
        /// </summary>
        public string DataFileDirectory { get; set; }

        /// <summary>
        /// File containing the column header definition for the Code-Point data files.
        /// </summary>
        public string ColumnHeadersCsvFile { get; set; }
    }
}
