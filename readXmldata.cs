   public static List<IntervalReading> readXmldata(XmlDocument xDoc, string usagePointUrl)
        {
            try
            {
                List<IntervalReading> dailyConsumptionList = new List<IntervalReading>();
                //get thw root node
                XmlNode root = xDoc.DocumentElement;
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(xDoc.NameTable);
                nsMgr.AddNamespace("espi", "http://naesb.org/espi");
                nsMgr.AddNamespace("doc", "http://www.w3.org/2005/Atom");

                // 1- Read the ReadingType node for the specified unit of measurement and flow direction
                //72 = Watt-hours (see tblGBUomType).  flowdirection 1 = ???
                XmlNode readingTypeNode = root.SelectSingleNode(@"/doc:feed/doc:entry[doc:content/espi:ReadingType[espi:uom = '72' and espi:flowDirection = '1']]", nsMgr);

                // 2- Read "href" attribute of the "link" node under the ReadingType node we found in step 1
                string readingTypeLinkHref = readingTypeNode.SelectSingleNode(@"doc:link[@rel = 'self']", nsMgr).Attributes["href"].Value;

                // 3- Read the "href" attribute of the "link" node that is the sibling node of the "link" node (under an "entry" node) 
                //    that had the same "href" attribute as what we found in previous step
                string intervalBlockHref = root.SelectSingleNode(String.Format("/doc:feed/doc:entry[doc:link[@rel = 'related' and @href='{0}'] and doc:link[@href='{1}' and @rel='up']]/doc:link[contains(@href,'/IntervalBlock') and @rel = 'related' ]",
                    readingTypeLinkHref, usagePointUrl + "/MeterReading"), nsMgr).Attributes["href"].Value;

                // XmlNodeList allenrtieswithdata = root.SelectNodes(String.Format("/doc:feed/doc:entry[doc:link[@rel = 'up' and contains(@href,'{0}')]]", readingType), nsMgr);
                XmlNodeList allenrtieswithdata = root.SelectNodes(String.Format("/doc:feed/doc:entry[doc:link[@rel = 'up' and @href='{0}']]", intervalBlockHref), nsMgr);
                // all the entry 
                decimal dailyconsumption = 0;
                //decimal[] detailReadingArr = new decimal[288];
                object[,] detailReadingArr = new object[288, 2];
                object[] Timevalues = new object[288];
                object[] values = new object[288];
                int i = 0;
                foreach (XmlNode xNode in allenrtieswithdata)
                {
                    dailyconsumption = 0;
                    i = 0;
                    //get the date of the usage
                    //var sdate = DateTime.Parse();
                    //string consumptionDate = sdate.ToString(format, CultureInfo.InvariantCulture);
                    string consumptionDate = GeneralHelper.GetEstFromUnixTimeStamp(int.Parse(xNode.SelectSingleNode("doc:content/espi:IntervalBlock/espi:interval/espi:start", nsMgr).InnerText));//GeneralHelper.GetDateTimeFromUnixTimestamp(int.Parse(xNode.SelectSingleNode("doc:content/espi:IntervalBlock/espi:interval/espi:start", nsMgr).InnerText));

                    // all the Value tags for each interval
                    values = xNode.SelectNodes("doc:content/espi:IntervalBlock/espi:IntervalReading/espi:value", nsMgr).Cast<XmlNode>().Select(node => node.InnerText).ToArray();
                    Timevalues = xNode.SelectNodes("doc:content/espi:IntervalBlock/espi:IntervalReading/espi:timePeriod/espi:start", nsMgr).Cast<XmlNode>().Select(node => node.InnerText).ToArray();

                    for (i = 0; i < 288; i++)
                    {
                        detailReadingArr[i, 0] = values[i];
                        detailReadingArr[i, 1] = /*GeneralHelper.GetEstFromUnixTimeStamp(int.Parse(Timevalues[i].ToString()));*/GeneralHelper.GetDateTimeFromUnixTimestamp(int.Parse(Timevalues[i].ToString()));
                        dailyconsumption = decimal.Parse(values[i].ToString()) + dailyconsumption;
                    }
                    dailyConsumptionList.Add(new IntervalReading(288) { Value = dailyconsumption, Date = consumptionDate, DetailReading = detailReadingArr });
                }

                return dailyConsumptionList;
            }
            catch (Exception ex)
            {
                Common.LogException("Exception on GreenButtonManager-readXmldata", ex);
                return null;
            }
        }       // The datetime in the intervals are GMT's
