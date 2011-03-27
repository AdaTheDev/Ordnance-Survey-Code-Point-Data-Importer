using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OSCodePointDataImport
{
    class ScaleGazetteerArgParser : CommandLineArgParser<ScaleGazetteerOptions>
    {
      
        internal override void Validate()
        {
            base.Validate();
            if (String.IsNullOrWhiteSpace(_options.CountyLookupTableName)) throw new ArgumentException("CountyLookupTableName argument must be supplied");
            if (String.IsNullOrWhiteSpace(_options.FeatureLookupTableName)) throw new ArgumentException("FeatureLookupTableName argument must be supplied");
            if (String.IsNullOrWhiteSpace(_options.DataFileName)) throw new ArgumentException("Datafile argument must be supplied");
            if (!File.Exists(_options.DataFileName)) throw new ArgumentException("Datafile argument is not valid. File does not exist");            
        }

        public override void Parse(string[] args)
        {
            base.Parse(args);
            if (args.Length >= 6) _options.CountyLookupTableName = args[5];
            if (args.Length >= 7) _options.FeatureLookupTableName = args[6];
            if (args.Length >= 8) _options.DataFileName = args[7];
        }
    }
}