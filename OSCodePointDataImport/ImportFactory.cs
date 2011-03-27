using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSCodePointDataImport
{
    class ImportFactory
    {
        //public static IArgParser GetArgParser(string importType)
        //{
        //    switch (importType.ToUpperInvariant())
        //    {
        //        case "CODEPOINT":
        //            return new CodePointArgParser();
        //        case "GAZETTEER":
        //            return new ScaleGazetteerArgParser<ScaleGazetteerOptions>();
        //        default:
        //            throw new ArgumentException("Unsupported import type. Must be: CODEPOINT or GAZETTEER");
        //    }
        //}

        public static OSDataImporter GetDataImporter(string importType)
        {
            switch (importType.ToUpperInvariant())
            {
                case "CODEPOINT":
                    return new CodePointDataImporter();
                case "GAZETTEER":
                    return new ScaleGazetteerDataImporter();
                default:
                    throw new ArgumentException("Unsupported import type. Must be: CODEPOINT or GAZETTEER");
            }
        }
    }
}
