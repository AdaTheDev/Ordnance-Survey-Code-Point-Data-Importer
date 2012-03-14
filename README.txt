This project requires .NET 4.0. 
You will need to have downloaded and extracted the appropriate Ordnance Survey data file(s) to your machine prior 
to running. They are available from: https://www.ordnancesurvey.co.uk/opendatadownload/products.html


==========
Background
==========
A quick background to this project is on my blog post:
http://www.adathedev.co.uk/2011/01/gb-post-code-geographic-data-load-to.html

See analysis of accuracy of the conversion process from Eastings/Northings to Latitude/Longitude:
http://www.adathedev.co.uk/2012/03/ordnance-survey-data-importer.html


==============
Example usage:
==============
------------------
CODEPOINT data
------------------
To load code point data that has been downloaded and extracted to C:\OSCodePointData (with data file in "Data" subfolder and supporting docs in "doc" subfolder), 
into dbo.PostCodeData table in MyDatabase on server SQLServerA, run the following from the command prompt:

OSCodePointDataImport.exe CODEPOINT SQLServerA MyDatabase dbo PostCodeData "C:\OS Code-Point Data\Data" "c:\OS Code-Point Data\doc\Code-Point_Open_column_headers.csv"

The resulting database table will consist of the following columns:
OutwardCode VARCHAR(4) [first part of post code]
InwardCode VARCHAR(3) [second part of post code]
Longitude FLOAT
Latitude FLOAT
GeoLocation GEOGRAPHY

It will also calculate a basic average location for each post code district (e.g. AB12) and sector (e.g. AB12 3)
and create rows for those. For districts, the InwardCode will be an empty string.

-----------------------------
1:50000 SCALE GAZETTEER data
-----------------------------
To load the 1:50000 scale gazetteer data file that has been downloaded and extracted to C:\OSGazetteerData\50kgaz2010.txt,
into dbo.Gazetteer table in MyDatabase on server SQLServerA, and load county codes into table: County and feature codes
into table: Feature, run the following from the command prompt:

OSCodePointDataImport.exe GAZETTEER SQLServerA MyDatabase dbo Gazetteer County Feature "C:\OSGazetteerData\50kgaz2010.txt"

The Gazetteer database table will consist of the following columns:
SeqNo INTEGER PRIMARY KEY
PlaceName VARCHAR(60)
CountyCode CHAR(2) FK to County.Code
FeatureCode VARCHAR(3) FK to Feature.Code
Longitude FLOAT
Latitude FLOAT
GeoLocation GEOGRAPHY

The County lookup table will consist of the following columns:
Code CHAR(2) PRIMARY KEY
Name VARCHAR(60)

The Feature lookup table will consist of the following columns:
Code VARCHAR(3) PRIMARY KEY
Description VARCHAR(50)