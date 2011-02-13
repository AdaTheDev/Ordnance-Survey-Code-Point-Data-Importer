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

namespace OSCodePointDataImport
{
    /// <summary>
    /// Console app.
    /// Prerequisites:
    /// - download the Code-Point data from: https://www.ordnancesurvey.co.uk/opendatadownload/products.html
    ///   and extract the files to a directory on your machine.
    /// </summary>
    /// <example>
    /// OSCodePointDataImport.exe CODEPOINT MySqlServerName MyDbName dbo PostCodeData "C:\OS Code-Point Data"
    /// </example>
    class Program
    {        
        static void Main(string[] args)
        {
            try
            {
                if (args == null || args.Length == 0) throw new Exception("No arguments were provided");


                // Currently, just for importing Code-Point data files. In future, could import other Ordnance Survey 
                // data sets such as Boundary Data dependent on command line params.
                switch (args[0].ToUpperInvariant())
                {
                    case "CODEPOINT":
                        RunCodePointDataImport(args);
                        break;                
                    default:
                        throw new Exception("Invalid first argument: import type must be CODEPOINT");
                }                       

                Console.WriteLine("The import process is complete. Press any key to exit.");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message.ToString());
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }            
        }

        /// <summary>
        /// Import Code-Point data.
        /// </summary>
        /// <param name="args">command line args</param>
        static void RunCodePointDataImport(string[] args)
        {
            // This deals with parsing out the command line args specifically for Code-Point data importer.
            CodePointArgParser argParser = new CodePointArgParser();
            argParser.Parse(args);

            CodePointDataImporter importer = new CodePointDataImporter();
            importer.LoadData(argParser.ServerName, argParser.DBName, argParser.SchemaName, argParser.TableName, argParser.DataFileDirectory);
        }       
    }
}
