   protected void btnPDFPrint_Click(object sender, EventArgs e)
        {
            // 1- Create folder for the building
            // 2- Download the XML document into the folder
            // 3- Read XSN file's path from XML
            // 4- Download XSN file into the folder
            // 5- Extract the XSN file in the same folder using WiX toolset:
            //    Download the binaries zip file (e.g.: wix38-binaries.zip) and under the "sdk" folder find the following 2 files:
            //    "Microsoft.Deployment.Compression.Cab.dll" and "Microsoft.Deployment.Compression.dll"
            //    Add these to your project and GAC
            // 6- Transform XML using view1.XSL (extracted from XSN) and create HTML in the same folder
            // 7- Use iTextSharp to create PDF from HTML - add the library to GAC
            // 8- Send PDF back by writing it to the Response
            // 9- Delete temporary folder

            SPWeb web = SPContext.Current.Web;
            bool foundFile = true;
            string DefaultPrintViewName = "";
            string htmlContent = "";
            string xmlLocation = "";
            string xsnLocation = "";
            string PdfLoc1 = "";
            string PdfLoc2 = "";
            string xmlFilePath = "";
            string xmlContent = "";
            string xsltContent = "";
            string pathToTempFolder = "";
            SPFile spFileTemplate = null;
            SPFile spFile = null;
            List<PromotedFields> pfList = new List<PromotedFields>();

            int printPdf = 0;
            if (!string.IsNullOrEmpty(Request.QueryString["XmlLocation"]))
                xmlLocation = Request.QueryString["XmlLocation"].ToString();
            if (!string.IsNullOrEmpty(Request.QueryString["XsnLocation"]))
                xsnLocation = Request.QueryString["XsnLocation"].ToString();
            if (!string.IsNullOrEmpty(Request.QueryString["PrintPDF"]))
                int.TryParse(Request.QueryString["PrintPDF"], out printPdf);
            if (!string.IsNullOrEmpty(Request.QueryString["PdfLoc1"]))
                PdfLoc1 = Request.QueryString["PdfLoc1"].ToString();
            if (!string.IsNullOrEmpty(Request.QueryString["PdfLoc2"]))
                PdfLoc1 = Request.QueryString["PdfLoc2"].ToString();

            try
            {
                //Uri uri = new Uri();
                if (xmlLocation == "")// if xml location not in url
                {
                    string fileName = "";
                    if (!string.IsNullOrEmpty(Request.QueryString["SaveLocation"]))
                    {
                        xmlLocation = Request.QueryString["SaveLocation"];
                        fileName = null;
                    }
                    XmlFormView XmlFormView1 = (XmlFormView)pnlFormView.Controls[1];
                    string url = getSendEmailUrl(XmlFormView1, xmlLocation, txtViewName.Value, fileName);
                    if (url != "")
                    {
                        Uri myUri = new Uri(url);
                        xmlLocation = HttpUtility.ParseQueryString(myUri.Query).Get("XmlLocation");
                    }
                    else
                    {
                        string err = string.Format("<script>alert('Unable to locate the file. Please save the file before converting to PDF');</script>");
                        Response.Write(err);
                        return;
                    }
                }

                // 1- Create folder for the building
                pathToTempFolder = InfoPathFormsManager.CreateFolderInTheDirectory();

                // 2- Download the XML document into the folder


                spFile = web.GetFile(xmlLocation);
                if (!spFile.Exists)
                {
                    string err = string.Format("<script>alert('Unable to locate the file. Please save the file before converting to PDF');</script>");
                    Response.Write(err);
                    return;
                }

                if (printPdf == 1) DefaultPrintViewName = InfoPathFormsManager.ReadFieldFromInfoPath(spFile, "my:DefaultPrintViewName");
                if (DefaultPrintViewName == "")
                    DefaultPrintViewName = txtViewName.Value;

                pfList = InfoPathFormsManager.readPromotedFields(spFile);
                xmlFilePath = string.Format(@"{0}{1}", pathToTempFolder, spFile.Name);

                InfoPathFormsManager.CopyFileInThePath(spFile, xmlFilePath);

                // 3- Read XSN file's path from XML
				// 4- Download XSN file into the folder
                spFileTemplate = InfoPathFormsManager.GetInfoPathFileTemplate(xmlFilePath);

                string xsnFilePath = string.Format(@"{0}{1}", pathToTempFolder, spFileTemplate.Name);
                InfoPathFormsManager.CopyFileInThePath(spFileTemplate, xsnFilePath);

                // 5- Extract the XSN file in the same folder using WiX toolset
                CabInfo cab = new CabInfo(xsnFilePath);
                cab.Unpack(pathToTempFolder);

                // 6- Transform XML using view1.XSL (extracted from XSN) and create HTML in the same folder
                string xslFilePath = string.Format(@"{0}{1}.xsl", pathToTempFolder, DefaultPrintViewName);
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    // Read XML file's content
                    using (StreamReader reader = new StreamReader(xmlFilePath))
                    {
                        xmlContent = reader.ReadToEnd();
                    }
                    // Read XSL file's path (view1.xsl)
                    try
                    {
                        using (StreamReader xslReader = new StreamReader(xslFilePath))
                        {
                            xsltContent = xslReader.ReadToEnd();
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        string err = string.Format("<script>alert('Could not find file {0}.xsl');</script>", DefaultPrintViewName);
                        Response.Write(err);
                        foundFile = false;
                        return;
                    }
                    // Transform
                    htmlContent = InfoPathFormsManager.Transform(xsltContent, xmlContent);
                    htmlContent = InfoPathFormsManager.CleanUpHTML(htmlContent, pathToTempFolder);
                });

                if (foundFile)
                {
                    // 7- Create PDF from HTML 
                    byte[] pdfBuffer = InfoPathFormsManager.ConvertToPdfUsingHiQ(htmlContent, pathToTempFolder);

                    //----------------------------------------save the pdf file in the library
                    if (printPdf == 1)
                    {
                        if (PdfLoc1 != "") InfoPathFormsManager.SavePdfInLibrary(pdfBuffer, PdfLoc1, InfoPathFormsManager.GetFileNameFromUrl(xmlLocation, xsnLocation), pfList, spFile);
                        if (PdfLoc2 != "") InfoPathFormsManager.SavePdfInLibrary(pdfBuffer, PdfLoc2, InfoPathFormsManager.GetFileNameFromUrl(xmlLocation, xsnLocation), pfList, spFile);
                    }
                    //----------------------------------------

                    // 8- Send PDF back by writing it to the Response

                    // inform the browser about the binary data format
                    Response.AddHeader("Content-Type", "application/pdf");
                    string fileName = InfoPathFormsManager.GetFileNameFromUrl(xmlLocation, xsnLocation).Replace(".xml", "");
                    // let the browser know how to open the PDF document, attachment or inline, and the file name

                    string contentDisposition;
                    if (Request.Browser.Browser == "IE" && (Request.Browser.Version == "7.0" || Request.Browser.Version == "8.0"))
                        contentDisposition = "attachment; filename=\"" + Uri.EscapeDataString(fileName) + "\"" + ".pdf; size=" + pdfBuffer.Length.ToString();
                    else if (Request.Browser.Browser == "Safari")
                        contentDisposition = "attachment; filename=" + fileName + ".pdf; size=" + pdfBuffer.Length.ToString();
                    else if (Request.Browser.Browser.Contains("Firefox"))
                        contentDisposition = "attachment; filename=\"" + fileName + ".pdf" + "\"" + ";size=" + pdfBuffer.Length.ToString();
                    else
                        contentDisposition = "attachment; filename*=UTF-8''" + Uri.EscapeDataString(fileName) + ".pdf; size=" + pdfBuffer.Length.ToString();

                    Response.AddHeader("Content-Disposition", contentDisposition);
                    // Response.AddHeader("Refresh", "12;URL=www.google.com");

                    // write the PDF buffer to HTTP response
                    Response.BinaryWrite(pdfBuffer);
                    Response.Flush();
                }
            }
            catch (Exception ex)
            {
                Common.LogException("Exeption on Print to Pdf- BuiltSpaceInfoPathContainer", ex);
            }
            finally
            {
                //9- Delete Folder
                if (pathToTempFolder != "")
                    SPSecurity.RunWithElevatedPrivileges(delegate() { Directory.Delete(pathToTempFolder, true); });
            }
        }

       