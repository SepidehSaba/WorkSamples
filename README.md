# WorkSamples

This repo contains a few of the files and code snippets of some of the projects on which I have worked recently.

1. ISAPI folder: A REST web service implemented utilizing WCF in order to expose some of the business logic through Sharepoint. This folder contains a Service contract, its implementation and the web service's configuration.
2. GreenButton folder: To integrate with the implementation of ([GreenButton's REST APIs](http://energyos.github.io/OpenESPI-GreenButton-API-Documentation/API/)) from London Hydro and Hydro One ([What is GreenButton?](http://www.greenbuttondata.org/)). This included the integration with providers' oAuth implementation, downloading and processing the consumption data periodically, and converting it to meaningful reports for our consumers.
3. generatePDF.cs: To introduce the "Print to PDF" functionality in online InfoPath forms with multiple views. The code downloads the form that's stored as a XML document in SharePoint, identifies the correct view of the form and downloads and applies the corresponding XSL transformation to generate HTML, and eventually generates the PDF from the HTML using a 3rd party library.
