using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDPG.GeoCoordConversion;

namespace OSCodePointDataImport
{
    abstract class OSDataImporter
    {
        /// <summary>
        /// Converts northing and easting coordinates into Longitude/Latitude coordinates in the WGS84 coordinate system.
        /// </summary>
        /// <param name="northing">northing coordinate</param>
        /// <param name="easting">easting coordinate</param>
        /// <returns>converted coordinates</returns>
        protected PolarGeoCoordinate ConvertToLonLat(double northing, double easting)
        {
            // Use GeoCoordConversion DLL to convert the Eastings & Northings to Lon/Lat
            // coordinates in the WGS84 system. The DLL was pulled from: http://code.google.com/p/geocoordconversion/
            // and is available under the GNU General Public License v3: http://www.gnu.org/licenses/gpl.html
            GridReference gridRef = new GridReference((long)easting, (long)northing);
            PolarGeoCoordinate polarCoord = GridReference.ChangeToPolarGeo(gridRef);
            return PolarGeoCoordinate.ChangeCoordinateSystem(polarCoord, CoordinateSystems.WGS84);   
        }
    }
}
