 public static string getRefreshToken(string accessToken, string connectionString)
        {
            string refreshToken = "";
            if (connectionString == string.Empty)
                connectionString = "name=BuiltSpaceDB";

            string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(ConfigConstants.BuiltSpaceLondonHydroLoginId + ":" + ConfigConstants.BuiltSpaceLodonHydroPass));
            using (BuiltSpaceDB db = new BuiltSpaceDB(connectionString))
            {
                tblGreenButtonMapping_LH lh = db.tblGreenButtonMapping_LH.Where(l => l.access_token == accessToken).FirstOrDefault();
                refreshToken = lh.refresh_token;
            }
            string tokenUrl = string.Format(@"{0}/oauth/token?grant_type=refresh_token&refresh_token={1}", ConfigConstants.londonHydroUri, refreshToken);
            WebRequest request = (HttpWebRequest)WebRequest.Create(tokenUrl);
            request.Method = WebRequestMethods.Http.Post;
            request.Headers.Add("Authorization", "Basic " + credentials);
            request.ContentLength = 0;
            request.ContentType = "application/json";
			//prevent ssl error
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    // Get the stream containing content returned by the server.
                    Stream dataStream = response.GetResponseStream();
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(RefreshTokenInfo));
                    RefreshTokenInfo info = (RefreshTokenInfo)ser.ReadObject(dataStream);
                    response.Close();
                    updateGBMappingWithRefreshedToken(info, accessToken, connectionString);

                    return info.access_token;
                }
            }
            catch (Exception ex)
            {
                Common.LogException("Exception on GreenButtonManager-London Hydro-getRefreshToken - Could not get refesh token", ex);
                return "";
            }
        }