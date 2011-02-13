This project requires .NET 4.0. 
You will need to have downloaded and extracted the Code Point data to your machine prior to running. They
are available from: https://www.ordnancesurvey.co.uk/opendatadownload/products.html

Example usage:

To load code point data that has been downloaded and extracted to C:\OSCodePointData, into
dbo.PostCodeData table in MyDatabase on server SQLServerA, run the following from the command prompt:

OSCodePointDataImport.exe CODEPOINT SQLServerA MyDatabase dbo PostCodeData "C:\OS Code-Point Data"

A quick background to this project is on my blog post:
http://www.adathedev.co.uk/2011/01/gb-post-code-geographic-data-load-to.html