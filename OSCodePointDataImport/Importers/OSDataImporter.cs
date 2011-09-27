using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDPG.GeoCoordConversion;
using System.Data.SqlClient;
using System.Data;

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

        /// <summary>
        /// Loads the supplied data to the newly created table in the database.
        /// </summary>
        /// <param name="connection">SQL Server database connection</param>
        /// <param name="data">DataTable containing the data to load</param>
        /// <param name="schemaName">name of schema the table belongs to</param>
        /// <param name="tableName">table to load the data to</param>
        protected int LoadRowsToDatabase(SqlConnection connection, DataTable data, string schemaName, string tableName)
        {
            Console.WriteLine("Bulk loading the data into {0}.{1}...", schemaName, tableName);

            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, null))
            {
                bulkCopy.BulkCopyTimeout = 600;
                bulkCopy.DestinationTableName = String.Format("[{0}].[{1}]", schemaName, tableName);
                foreach(DataColumn column in data.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }
                bulkCopy.WriteToServer(data);
            }
            
            Console.WriteLine("Done! {0} rows inserted", data.Rows.Count.ToString());
            return data.Rows.Count;
            
        }

        /// <summary>
        /// Executes the supplied SQL.
        /// </summary>
        /// <param name="connection">db connection to execute against</param>
        /// <param name="commandText">SQL to execute</param>
        private void ExecCommand(SqlConnection connection, string commandText)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = connection;
                cmd.CommandTimeout = 600;
                
                cmd.CommandText = commandText;
                cmd.ExecuteNonQuery();
                Console.WriteLine("Done!");
            }
        }

        /// <summary>
        /// Set the GeoLocation column in the supplied table, to a GEOGRAPHY point based on the Latitude and Longitude columns
        /// </summary>
        /// <param name="connection">db connection to use</param>
        /// <param name="schemaName">name of schema the table belongs to</param>
        /// <param name="tableName">name of table to update</param>
        protected void SetGeoColumn(SqlConnection connection, string schemaName, string tableName)
        {
            Console.WriteLine("Setting GeoLocation GEOGRAPHY column to point based on Latitude and Longitude...");

            ExecCommand(connection, String.Format(@"UPDATE [{0}].[{1}] SET GeoLocation = geography::Point([Latitude], [Longitude], 4326);",
                        schemaName, tableName));                
        }

        /// <summary>
        /// Sets the primary key on the supplied table
        /// </summary>
        /// <param name="connection">db connection to use</param>
        /// <param name="schemaName">name of schema the table belongs to</param>
        /// <param name="tableName">name of the table to add the PK constraint on</param>
        /// <param name="pkColumnNames">column or columns that form the PK</param>
        protected void SetPrimaryKey(SqlConnection connection, string schemaName, string tableName, string[] pkColumnNames)
        {
            string pkCols = "[" + String.Join("],[", pkColumnNames) + "]";
            Console.WriteLine("Setting PRIMARY KEY on table: {0}...", tableName);

            ExecCommand(connection, String.Format("ALTER TABLE [{0}].[{1}] ADD CONSTRAINT [PK_{1}] PRIMARY KEY CLUSTERED ({2});",
                schemaName, tableName, pkCols));                
        }

        /// <summary>
        /// Adds a foreign key constraint.
        /// </summary>
        /// <param name="connection">db connection to run the command against</param>
        /// <param name="schemaName">schema name</param>
        /// <param name="primaryKeyTableName">name of the primary key table</param>
        /// <param name="primaryKeyColumnName">name of the primary key column</param>
        /// <param name="foreignKeyTableName">name of the foreign key table</param>
        /// <param name="foreignKeyColumnName">name of the foreign key column</param>
        protected void SetForeignKey(SqlConnection connection, string schemaName, string primaryKeyTableName, string primaryKeyColumnName, string foreignKeyTableName, string foreignKeyColumnName)
        {
            Console.WriteLine("Setting FOREIGN KEY from {0} to {1}...", foreignKeyTableName, primaryKeyTableName);

            ExecCommand(connection, String.Format("ALTER TABLE [{0}].[{1}] ADD CONSTRAINT [FK_{1}_{2}] FOREIGN KEY ([{3}]) REFERENCES [{0}].{2}({4});",
                schemaName, foreignKeyTableName, primaryKeyTableName, foreignKeyColumnName, primaryKeyColumnName));                
        }

        /// <summary>
        /// Creates a spatial index on the GeoLocation column.
        /// </summary>
        /// <param name="connection">db conneciton to run the command against</param>
        /// <param name="schemaName">schema name</param>
        /// <param name="tableName">table name</param>
        protected void CreateSpatialIndex(SqlConnection connection, string schemaName, string tableName)
        {
            Console.WriteLine("Creating spatial index on GeoLocation column in table: {0}...", tableName);

            ExecCommand(connection, String.Format("CREATE SPATIAL INDEX [IX_{0}_GeoLocation] ON [{1}].[{0}] ([GeoLocation]);", tableName, schemaName));
        }
    }
}
