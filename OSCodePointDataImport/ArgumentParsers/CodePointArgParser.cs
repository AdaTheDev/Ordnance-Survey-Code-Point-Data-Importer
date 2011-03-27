using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSCodePointDataImport
{
    class CodePointArgParser : CommandLineArgParser<CodePointOptions>
    {               
        /// <summary>
        /// Directory containing the downloaded Ordnance Survey Code-Point CSV data files.
        /// </summary>
        public string DataFileDirectory { get; set; }

        /// <summary>
        /// Parses the command line args supplied for loading the Code-Point data. 
        /// Common args are parsed by <see cref="CommandLineArgs.Parse"/>base class</see>.
        /// </summary>
        /// <param name="args">command line args</param>
        public override void Parse(string[] args)
        {
            base.Parse(args);
            if (args.Length >= 6) ImportOptions.DataFileDirectory = args[5];
        }

        /// <summary>
        /// Validate the arguments supplied for loading the Code-Point data.
        /// </summary>
        internal override void Validate()
        {
            base.Validate();
            if (String.IsNullOrWhiteSpace(DataFileDirectory)) throw new ArgumentException("DataFileDirectory argument must be supplied");
        }        
    }
}
