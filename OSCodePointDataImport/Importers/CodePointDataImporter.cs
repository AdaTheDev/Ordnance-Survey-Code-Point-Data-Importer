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
    class CodePointDataImporter : OSDataImporter
    {
        private const string PostCodeColumnCode = "PC";
        private const string EastingColumnCode = "EA";
        private const string NorthingColumnCode = "NO";

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
        /// <param name="columnHeadersCsvFile">CSV file containing the column header definition for the Code-Point CSV data files.
        /// Should be in ..\Doc\Code-Point_Open_column_headers.csv</param>
        /// <returns>number of rows loaded</returns>
        public int LoadData(string serverName, string databaseName, string schemaName, string tableName, string dataFileDirectory, string columnHeadersCsvFile)
        {
            // Basic validation
            if (String.IsNullOrWhiteSpace(serverName)) throw new ArgumentException("ServerName must be supplied", "serverName");
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentException("DatabaseName must be supplied", "databaseName");
            if (String.IsNullOrWhiteSpace(schemaName)) throw new ArgumentException("SchemaName must be supplied", "schemaName");
            if (String.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("TableName must be supplied", "tableName");
            if (String.IsNullOrWhiteSpace(dataFileDirectory)) throw new ArgumentException("DataFileDirectory must be supplied", "dataFileDirectory");
            if (!Directory.Exists(dataFileDirectory)) throw new DirectoryNotFoundException("DataFileDirectory does not exist");
            if (String.IsNullOrWhiteSpace(columnHeadersCsvFile)) throw new ArgumentException("ColumnHeadersCsvFile must be supplied", "columnHeadersCsvFile");
            if (!File.Exists(columnHeadersCsvFile)) throw new FileNotFoundException("ColumnHeadersCsvFile does not exist");

            Dictionary<string, int> columns = ReadColumnHeaders(columnHeadersCsvFile);
            if (!columns.ContainsKey(PostCodeColumnCode)) throw new Exception("Could not find PostCode column in ColumnHeadersCsvFile");
            if (!columns.ContainsKey(EastingColumnCode)) throw new Exception("Could not find Easting column in ColumnHeadersCsvFile");
            if (!columns.ContainsKey(NorthingColumnCode)) throw new Exception("Could not find Northing column in ColumnHeadersCsvFile");

            int result = 0;
            using (SqlConnection connection = new SqlConnection(String.Format("Data Source={0};Initial Catalog={1};Integrated Security=SSPI;", serverName, databaseName)))
            {
                connection.Open();
                // Create the table to import the data into. If it already exists, or the schema is not valid this will throw an exception.
                PrepareTable(connection, schemaName, tableName);                
                // Read all the data in from the data files into a single DataTable in memory
                using (DataTable data = ReadDataFromFiles(dataFileDirectory, columns))
                {
                    // Now load the data to the DB
                    result = LoadRowsToDatabase(connection, data, schemaName, tableName);
                }
                // Calculate and create rows for each postcode district (e.g. AB12) and sector (e.g. AB12 3), defining the Lon/Lat
                // coordinates as the straight average of all the postcodes within that area.
                CalculateDistrictsAndSectors(connection, schemaName, tableName);
                // Now set the GeoLocation column (Geography type) based on the Latitude/Longitude
                SetGeoColumn(connection, schemaName, tableName);
                // Set PK on table
                SetPrimaryKey(connection, schemaName, tableName, new string[] { "OutwardCode", "InwardCode" });
                // Create spatial index on GeoLocation column
                CreateSpatialIndex(connection, schemaName, tableName);
            }
            
            return result;
        }

        /// <summary>
        /// Looks in the CSV headers file supplied with the Code-Point data download, and extracts
        /// all the columns defined with their index position.
        /// </summary>
        /// <param name="columnHeadersCsvFile">CSV file containing the column header listing</param>
        /// <returns>dictionary of headers</returns>
        private Dictionary<string, int> ReadColumnHeaders(string columnHeadersCsvFile)
        {
            using (StreamReader reader = new StreamReader(columnHeadersCsvFile))
            {
                string[] headers = reader.ReadLine().Split(',');
                Dictionary<string, int> columnHeaders =
                    headers.Select((header, index) => new { Header = header, Index = index }).ToDictionary(x => x.Header, x => x.Index);
                return columnHeaders;                
            }            
        }

        public int LoadData(CodePointOptions options)
        {
            return LoadData(options.ServerName, options.DBName, options.SchemaName, options.TableName, options.DataFileDirectory, options.ColumnHeadersCsvFile);
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
                                OutwardCode VARCHAR(4) NOT NULL,
                                InwardCode VARCHAR(3) NOT NULL,
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
        private DataTable ReadDataFromFiles(string directory, Dictionary<string, int> columns)
        {
            DataTable data = new DataTable();
            
            data.Columns.Add("OutwardCode", typeof(string));
            data.Columns.Add("InwardCode", typeof(string));
            data.Columns.Add("Longitude", typeof(double));
            data.Columns.Add("Latitude", typeof(double));
            long easting, northing;

            Console.WriteLine("Loading postcode data from files into memory...");      
            string postCode;
            string outwardCode;
            string inwardCode;
            DataRow row;
            PolarGeoCoordinate polarCoord;

            int eastingColumnIndex = columns[EastingColumnCode];
            int northingColumnIndex = columns[NorthingColumnCode];
            int postCodeColumnIndex = columns[PostCodeColumnCode];

            foreach (string fileName in Directory.GetFiles(directory, "*.csv"))
            {
                string[] lineData;                
                using (StreamReader stream = new StreamReader(fileName))
                {
                    while (stream.Peek() >= 0)
                    {                       
                        lineData = stream.ReadLine().Split(',');
                        easting = long.Parse(lineData[eastingColumnIndex]);
                        northing = long.Parse(lineData[northingColumnIndex]);

                        polarCoord = ConvertToLonLat(northing, easting);                   
                        row = data.NewRow();
                        postCode = lineData[postCodeColumnIndex].Replace("\"", ""); // 1st column is the PostCode and is contained within double quotes (remove them).
                        if (postCode.Contains(' '))
                        {
                            outwardCode = postCode.Substring(0, postCode.IndexOf(' '));
                            inwardCode = postCode.Substring(postCode.LastIndexOf(' ') + 1);
                        }
                        else
                        {
                            outwardCode = postCode.Substring(0, 4);
                            inwardCode = postCode.Substring(4, 3);
                        }
                        
                        row["OutwardCode"] = outwardCode;
                        row["InwardCode"] = inwardCode;
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
        /// Calculates a simple average Lon/Lat for each postcode district (e.g. AB12) and sector (e.g. AB12 3),
        /// and creates a row in the db table for each one.
        /// </summary>
        /// <param name="connection">SQL Server database connection</param>
        /// <param name="schemaName">name of schema the table belongs to</param>
        /// <param name="tableName">table to load the Post Code data to</param>
        private void CalculateDistrictsAndSectors(SqlConnection connection, string schemaName, string tableName)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = connection;
                cmd.CommandTimeout = 600;
                Console.WriteLine("Calculating averages for postcode districts and sectors...");
                cmd.CommandText = String.Format(@";WITH CTEDistrictsAndSectors AS
                    (
	                    SELECT OutwardCode, '' AS InwardCode, AVG(Longitude) AS AvgLongitude, AVG(Latitude) AS AvgLatitude
	                    FROM [{0}].[{1}]
	                    GROUP BY OutwardCode
	                    UNION ALL
	                    SELECT OutwardCode, LEFT(InwardCode, 1), AVG(Longitude), AVG(Latitude)
	                    FROM [{0}].[{1}]
	                    GROUP BY OutwardCode, LEFT(InwardCode, 1)
                    )
                    INSERT [{0}].[{1}] (OutwardCode, InwardCode, Longitude, Latitude, GeoLocation)
                    SELECT OutwardCode, InwardCode, AvgLongitude, AvgLatitude, geography::Point(AvgLatitude, AvgLongitude, 4326)
                    FROM CTEDistrictsAndSectors", schemaName, tableName);
                cmd.ExecuteNonQuery();
                Console.WriteLine("Done!");
            }

        }        
    }
}
