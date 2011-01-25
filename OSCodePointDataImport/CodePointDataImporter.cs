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
using System.Data.SqlClient;
using System.IO;
using System.Data;
using TDPG.GeoCoordConversion;

namespace OSCodePointDataImport
{    
    /// <summary>
    /// Functionality to load Ordnance Survey Code-Point data files to SQL Server.
    /// The data files are available for download from: https://www.ordnancesurvey.co.uk/opendatadownload/products.html
    /// </summary>
    class CodePointDataImporter
    {
        /// <summary>
        /// Loads Code-Point CSV data files into a new table in SQL Server, converting the provided Eastings & Northings coordinates
        /// into WGS84 Lon/Lat coordinates.
        /// </summary>
        /// <param name="serverName">SQL Server name</param>
        /// <param name="databaseName">database to load the data into</param>
        /// <param name="schemaName">schema to create table in</param>
        /// <param name="tableName">table name to create and load the data into. The table must not already exist. If it does already
        /// exist, an exception will be thrown.</param>
        /// <param name="dataFileDirectory">directory where the Code-Point CSV data files are</param>
        /// <returns>number of rows loaded</returns>
        public int LoadData(string serverName, string databaseName, string schemaName, string tableName, string dataFileDirectory)
        {
            // Basic validation
            if (String.IsNullOrWhiteSpace(serverName)) throw new ArgumentException("ServerName must be supplied", "serverName");
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentException("DatabaseName must be supplied", "databaseName");
            if (String.IsNullOrWhiteSpace(schemaName)) throw new ArgumentException("SchemaName must be supplied", "schemaName");
            if (String.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("TableName must be supplied", "tableName");
            if (String.IsNullOrWhiteSpace(dataFileDirectory)) throw new ArgumentException("DataFileDirectory must be supplied", "dataFileDirectory");
            if (!Directory.Exists(dataFileDirectory)) throw new DirectoryNotFoundException("DataFileDirectory does not exist");

            int result = 0;
            using (SqlConnection connection = new SqlConnection(String.Format("Data Source={0};Initial Catalog={1};Integrated Security=SSPI;", serverName, databaseName)))
            {
                connection.Open();
                // Create the table to import the data into. If it already exists, or the schema is not valid this will throw an exception.
                PrepareTable(connection, schemaName, tableName);
                // Read all the data in from the data files into a single DataTable in memory
                using (DataTable data = ReadDataFromFiles(dataFileDirectory))
                {
                    // Now load the data to the DB
                    LoadRowsToDatabase(connection, data, schemaName, tableName);
                    result = data.Rows.Count;
                }
                // Now set the GeoLocation column (Geography type) based on the Latitude/Longitude
                SetGeoDataAndFinaliseTable(connection, schemaName, tableName);
            }
            
            return result;
        }

        /// <summary>
        /// Creates a new table in the database ready to receive the Code-Point data.
        /// </summary>
        /// <param name="connection">database connection to SQL Server</param>
        /// <param name="schemaName">schema in which to create the table</param>
        /// <param name="tableName">name of table to create</param>
        private void PrepareTable(SqlConnection connection, string schemaName, string tableName)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = connection;                
                    cmd.CommandText =
                   String.Format(@"IF NOT EXISTS(SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = @TableName AND s.name = @SchemaName)
                        BEGIN
                            CREATE TABLE [{0}].[{1}]
                            (
                                PostCode VARCHAR(7) NOT NULL,
                                Longitude FLOAT,
                                Latitude FLOAT,
                                GeoLocation GEOGRAPHY
                            )
                            SET @Created = 1
                        END", schemaName, tableName);
                    cmd.Parameters.Add("@TableName", System.Data.SqlDbType.NVarChar,128).Value = tableName;
                    cmd.Parameters.Add("@SchemaName", System.Data.SqlDbType.NVarChar,128).Value = schemaName;
                    cmd.Parameters.Add("@Created", System.Data.SqlDbType.Bit, 1).Direction = ParameterDirection.Output;
                    cmd.ExecuteNonQuery();
                    if (cmd.Parameters["@Created"].Value == DBNull.Value)
                    {
                        throw new Exception("Cannot create new table to load to because the table already exists");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error attemping to create table to load data into [{0}].[{1}]: {2}",schemaName, tableName, ex.Message), ex);
            }
        }

        /// <summary>
        /// Reads in all .csv files in the supplied directory, converts the Eastings & Northings coordinates into
        /// the WGS84 Lon/Lat system and stores each Post Code with the Lon/Lat coordinates in memory in a DataTable.
        /// </summary>
        /// <param name="directory">directory containing the Ordnance Survey Code-Point CSV data files</param>
        /// <returns>DataTable containing each Post Code with convert Lon/Lat coordinates</returns>
        private DataTable ReadDataFromFiles(string directory)
        {
            DataTable data = new DataTable();
            
            data.Columns.Add("PostCode", typeof(string));
            data.Columns.Add("Longitude", typeof(double));
            data.Columns.Add("Latitude", typeof(double));
            long easting, northing;

            Console.WriteLine("Loading postcode data from files into memory...");

            foreach (string fileName in Directory.GetFiles(directory, "*.csv"))
            {
                string[] lineData;
                DataRow row;
                using (StreamReader stream = new StreamReader(fileName))
                {
                    while (stream.Peek() >= 0)
                    {                       
                        lineData = stream.ReadLine().Split(',');
                        easting = long.Parse(lineData[10]); // 11th column is Eastings
                        northing = long.Parse(lineData[11]); // 12th column is Northings

                        // Use GeoCoordConversion DLL to convert the Eastings & Northings to Lon/Lat
                        // coordinates in the WGS84 system. The DLL was pulled from: http://code.google.com/p/geocoordconversion/
                        // and is available under the GNU General Public License v3: http://www.gnu.org/licenses/gpl.html
                        GridReference gridRef = new GridReference(easting, northing);
                        PolarGeoCoordinate polarCoord = GridReference.ChangeToPolarGeo(gridRef);
                        polarCoord = PolarGeoCoordinate.ChangeCoordinateSystem(polarCoord, CoordinateSystems.WGS84);                       

                        row = data.NewRow();                            
                        row["PostCode"] = lineData[0].Replace("\"", ""); // 1st column is the PostCode and is contained within double quotes (remove them)
                        row["Longitude"] = polarCoord.Lon;
                        row["Latitude"] = polarCoord.Lat;
                        data.Rows.Add(row);
                    }
                }
            }

            Console.WriteLine("Done! {0} rows of data prepared", data.Rows.Count.ToString());
            return data;
        }

        /// <summary>
        /// Loads the supplied data to the newly created table in the database.
        /// </summary>
        /// <param name="connection">SQL Server database connection</param>
        /// <param name="data">DataTable containing the Post Codes with their Lon/Lat coordinates</param>
        /// <param name="schemaName">name of schema the table belongs to</param>
        /// <param name="tableName">table to load the Post Code data to</param>
        private void LoadRowsToDatabase(SqlConnection connection, DataTable data, string schemaName, string tableName)
        {
            Console.WriteLine("Bulk loading the data into {0}.{1}...", schemaName, tableName);

            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, null))
            {
                bulkCopy.BulkCopyTimeout = 60;
                bulkCopy.DestinationTableName = String.Format("[{0}].[{1}]", schemaName, tableName);
                bulkCopy.ColumnMappings.Add("PostCode", "PostCode");
                bulkCopy.ColumnMappings.Add("Longitude", "Longitude");
                bulkCopy.ColumnMappings.Add("Latitude", "Latitude");
                bulkCopy.WriteToServer(data);
            }

            Console.WriteLine("Done!");
        }

        /// <summary>
        /// Updates the loaded table to set the GeoLocation column to a Point based on the loaded Latitude and Longitude data.
        /// Then makes the PostCode column a clustered primary key.
        /// </summary>
        /// <param name="connection">SQL Server database connection</param>
        /// <param name="schemaName">name of the schema the table is in</param>
        /// <param name="tableName">name of the table containing the data</param>
        private void SetGeoDataAndFinaliseTable(SqlConnection connection, string schemaName, string tableName)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = connection;
                cmd.CommandTimeout = 600;
                Console.WriteLine("Setting GeoLocation GEOGRAPHY column to point based on Latitude and Longitude...");
                cmd.CommandText =
                    String.Format(@"UPDATE [{0}].[{1}] SET GeoLocation = geography::Point([Latitude], [Longitude], 4326);",
                        schemaName, tableName);
                cmd.ExecuteNonQuery();
                Console.WriteLine("Done!");
                
                Console.WriteLine("Setting PRIMARY KEY on table...");
                cmd.CommandText = String.Format("ALTER TABLE [{0}].[{1}] ADD CONSTRAINT [PK_{1}] PRIMARY KEY CLUSTERED ([PostCode]);",
                    schemaName, tableName);
                cmd.ExecuteNonQuery();
                Console.WriteLine("Done!");
            }
        }
    }
}
