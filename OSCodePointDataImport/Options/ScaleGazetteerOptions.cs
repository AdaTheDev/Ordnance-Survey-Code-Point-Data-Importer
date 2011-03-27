using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSCodePointDataImport
{
    class ScaleGazetteerOptions : Options
    {
        /// <summary>
        /// Scale Gazetteer data file
        /// </summary>
        public string DataFileName { get; set; }

        /// <summary>
        /// Name of the lookup table to create to store the county codes/names
        /// </summary>
        public string CountyLookupTableName { get; set; }

        /// <summary>
        /// Name of the lookup table to create to store the feature codes/names
        /// </summary>
        public string FeatureLookupTableName { get; set; }
    }
}
