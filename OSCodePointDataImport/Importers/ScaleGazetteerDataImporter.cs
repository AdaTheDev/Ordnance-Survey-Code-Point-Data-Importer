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
using System.IO;
using System.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Collections;
using TDPG.GeoCoordConversion;

namespace OSCodePointDataImport
{
    /// <summary>
    /// Functionality to load Ordnance Survey 1:50000 Scale Gazetteer data files to SQL Server.
    /// The data files are available for download from: https://www.ordnancesurvey.co.uk/opendatadownload/products.html
    /// </summary>
    class ScaleGazetteerDataImporter : OSDataImporter
    {
        /// <summary>
        /// Loads Scale Gazetteer CSV data file into a new table in SQL Server, creating 2 related lookup tables also 
        /// for the county codes and feature codes
        /// </summary>
        /// <param name="options">options</param>
        /// <returns>number of rows loaded</returns>
        public int LoadData(ScaleGazetteerOptions options)
        {
            return LoadData(options.ServerName, options.DBName, options.SchemaName, options.TableName, options.DataFileName, options.CountyLookupTableName, options.FeatureLookupTableName);
        }

        public int LoadData(string serverName, string databaseName, string schemaName, string tableName, string dataFile, 
            string countyLookupTableName, string featureLookupTableName)
        {
            // Basic validation
            if (String.IsNullOrWhiteSpace(serverName)) throw new ArgumentException("ServerName must be supplied", "serverName");
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentException("DatabaseName must be supplied", "databaseName");
            if (String.IsNullOrWhiteSpace(schemaName)) throw new ArgumentException("SchemaName must be supplied", "schemaName");
            if (String.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("TableName must be supplied", "tableName");
            if (String.IsNullOrWhiteSpace(dataFile)) throw new ArgumentException("DataFile must be supplied", "dataFile");
            if (String.IsNullOrWhiteSpace(countyLookupTableName)) throw new ArgumentException("CountyLookupTableName must be supplied", "countyLookupTableName");
            if (String.IsNullOrWhiteSpace(featureLookupTableName)) throw new ArgumentException("FeatureLookupTableName must be supplied", "featureLookupTableName");
            if (!File.Exists(dataFile)) throw new DirectoryNotFoundException("DataFile does not exist");
            int result = 0;

            using (SqlConnection connection = new SqlConnection(String.Format("Data Source={0};Initial Catalog={1};Integrated Security=SSPI;", serverName, databaseName)))
            {
                connection.Open();
                // Create the table to import the data into. If it already exists, or the schema is not valid this will throw an exception.
                PrepareTable(connection, schemaName, tableName, countyLookupTableName, featureLookupTableName);
                // Read all the data in from the data files into a single DataTable in memory
                using (DataSet data = ReadDataFromFile(dataFile))
                {
                    // Load the point data to the DB
                    result = LoadRowsToDatabase(connection, data.Tables["Point"], schemaName, tableName);

                    // Load the County reference data to the DB
                    LoadRowsToDatabase(connection, data.Tables["County"], schemaName, countyLookupTableName);

                    // Load the Feature code reference data to the DB
                    LoadRowsToDatabase(connection, data.Tables["Feature"], schemaName, featureLookupTableName);
                }              
                // Now set the GeoLocation column (Geography type) based on the Latitude/Longitude
                SetGeoColumn(connection, schemaName, tableName);
                // Set PK on the point data table.
                SetPrimaryKey(connection, schemaName, tableName, new string[] { "SeqNo" });
                // Set PK on the County lookup table
                SetPrimaryKey(connection, schemaName, countyLookupTableName, new string[] { "Code" });
                // Set PK on the Feature lookup table
                SetPrimaryKey(connection, schemaName, featureLookupTableName, new string[] { "Code" });
                // Set FK on point data table -> County lookup table
                SetForeignKey(connection, schemaName, countyLookupTableName, "Code", tableName, "CountyCode");
                // Set FK on point data table -> Feature lookup table
                SetForeignKey(connection, schemaName, featureLookupTableName, "Code", tableName, "FeatureCode");
                // Create spatial index on GeoLocation column
                CreateSpatialIndex(connection, schemaName, tableName);
            }

            return result;
        }

        /// <summary>
        /// Reads the data in from the Scale Gazetteer data file into a DataSet containing 3 datatables:
        /// 1) main point data
        /// 2) county lookup data
        /// 3) feature lookup data
        /// </summary>
        /// <param name="dataFile">1:50000 scale gazetteer data file as supplied by Ordance Survey at
        /// https://www.ordnancesurvey.co.uk/opendatadownload/products.html</param>
        /// <returns>Dataset</returns>
        private DataSet ReadDataFromFile(string dataFile)
        {
            DataSet ds = new DataSet();
            DataTable pointData = new DataTable("Point");
            DataTable countyData = new DataTable("County");
            DataTable featureData = new DataTable("Feature");

            pointData.Columns.Add("SeqNo", typeof(int));
            pointData.Columns.Add("PlaceName", typeof(string));
            pointData.Columns.Add("CountyCode", typeof(string));
            pointData.Columns.Add("FeatureCode", typeof(string));
            pointData.Columns.Add("Longitude", typeof(double));
            pointData.Columns.Add("Latitude", typeof(double));

            countyData.Columns.Add("Code", typeof(string));
            countyData.Columns.Add("Name", typeof(string));

            featureData.Columns.Add("Code", typeof(string));
            featureData.Columns.Add("Description", typeof(string));

            Console.WriteLine("Loading scale gazetteer data from file into memory...");
            int sequenceNumber;
            string placeName, featureCode, gmt;
            double latDegrees, latMinutes, lonDegrees, lonMinutes, latitude, longitude;

            DataRow row;
            string[] lineData;
            Dictionary<string, string> counties = new Dictionary<string, string>();
            string countyCode, countyName;

            using (StreamReader stream = new StreamReader(dataFile, Encoding.UTF7))
            {
                while (stream.Peek() >= 0)
                {
                    lineData = stream.ReadLine().Split(':');
                    sequenceNumber = int.Parse(lineData[0]);
                    placeName = lineData[2];
                    latDegrees = double.Parse(lineData[4]);
                    latMinutes = double.Parse(lineData[5]);
                    lonDegrees = double.Parse(lineData[6]);
                    lonMinutes = double.Parse(lineData[7]);                    
                    gmt = lineData[10];
                    countyCode = lineData[11];
                    countyName = lineData[13];
                    featureCode = lineData[14];

                    latitude = latDegrees + ((latMinutes != 0) ? latMinutes / 60.0 : 0.0);
                    longitude = lonDegrees + ((lonMinutes != 0) ? lonMinutes / 60.0 : 0.0);                                        

                    row = pointData.NewRow();
                    row["SeqNo"] = int.Parse(lineData[0]);
                    row["PlaceName"] = lineData[2];
                    row["CountyCode"] = countyCode;
                    row["FeatureCode"] = lineData[14];
                    row["Latitude"] = latitude;
                    row["Longitude"] = (String.Compare(gmt,"W") == 0) ? -longitude : longitude;                    
                    pointData.Rows.Add(row);

                    // Maintain a distinct list of county codes & names
                    if (!counties.ContainsKey(countyCode))                  
                        counties.Add(countyCode, countyName);                    
                }
            }

            // Create datatable of counties
            foreach (var county in counties)
            {
                countyData.Rows.Add(county.Key, county.Value);
            }

            // Create datatable of feature codes & descriptions from resx - taken from Ordance Survey download documentation
            var featureResource = Resources.FeatureCodes.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
            foreach (DictionaryEntry resourceItem in featureResource)
            {
                featureData.Rows.Add(resourceItem.Key, resourceItem.Value);
            }
            ds.Tables.Add(pointData);
            ds.Tables.Add(countyData);
            ds.Tables.Add(featureData);
            Console.WriteLine("Done! {0} point data rows and {1} county data rows of data prepared", pointData.Rows.Count.ToString(), countyData.Rows.Count.ToString());
            
            return ds;
        }

        /// <summary>
        /// Creates a new table in the database ready to receive the Code-Point data.
        /// </summary>
        /// <param name="connection">database connection to SQL Server</param>
        /// <param name="schemaName">schema in which to create the table</param>
        /// <param name="tableName">name of table to create</param>
        private void PrepareTable(SqlConnection connection, string schemaName, string tableName, string countyTableName, string featureTableName)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = connection;
                    cmd.CommandText =
                   String.Format(@"IF NOT EXISTS(SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name IN (@TableName, @CountyTableName, @FeatureTableName) AND s.name = @SchemaName)
                        BEGIN
                            CREATE TABLE [{0}].[{1}]
                            (
                                SeqNo INTEGER NOT NULL,
                                PlaceName VARCHAR(60) NOT NULL,
                                CountyCode CHAR(2) NOT NULL,
                                FeatureCode VARCHAR(3) NOT NULL,
                                Longitude FLOAT,
                                Latitude FLOAT,
                                GeoLocation GEOGRAPHY
                            );
                            CREATE TABLE [{0}].[{2}]
                            (
                                Code CHAR(2) NOT NULL,
                                Name VARCHAR(60) NOT NULL
                            );
                            CREATE TABLE [{0}].[{3}]
                            (
                                Code VARCHAR(3) NOT NULL,
                                Description VARCHAR(50) NOT NULL
                            )                   
                            SET @Created = 1
                        END;", schemaName, tableName, countyTableName, featureTableName);
                    cmd.Parameters.Add("@TableName", System.Data.SqlDbType.NVarChar, 128).Value = tableName;
                    cmd.Parameters.Add("@SchemaName", System.Data.SqlDbType.NVarChar, 128).Value = schemaName;
                    cmd.Parameters.Add("@CountyTableName", System.Data.SqlDbType.NVarChar, 128).Value = countyTableName;
                    cmd.Parameters.Add("@FeatureTableName", System.Data.SqlDbType.NVarChar, 128).Value = featureTableName;
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
                throw new Exception(String.Format("Error attemping to create table to load data into [{0}].[{1}]: {2}", schemaName, tableName, ex.Message), ex);
            }
        }
    }
}
