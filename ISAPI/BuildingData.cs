using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.ServiceModel.Activation;
using System.ServiceModel;
using System.CodeDom;
using System.CodeDom.Compiler;
using Jakeda.BuiltSpace.Services.BusinessObjects;
using Microsoft.SharePoint;
using System.Data;
using System.Web.Script.Serialization;
using Jakeda.BuiltSpace.Services.Entity;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.Net;
using System.ServiceModel.Web;
using System.Text.RegularExpressions;
using Jakeda.BuiltSpace.Security.Helpers;
using System.ServiceModel.Description;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Configuration;
using System.Configuration;

namespace Jakeda.BuiltSpace.Platform.ISAPI
{
    [ServiceBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]

    class BuildingData : IBuildingData
    {
        //[System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "4.0.30319.1")]
        //[System.SerializableAttribute()]
        //[System.Diagnostics.DebuggerStepThroughAttribute()]
        //[System.ComponentModel.DesignerCategoryAttribute("code")]
        //[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://omni.infrasofttech.com/")]

        #region Get

        public string Spaces(int BuildingId)
        {
            if (HttpContext.Current != null)
            {
                System.Security.Principal.IPrincipal iuser = HttpContext.Current.User;
                if (iuser != null)
                {
                    DataTable dt = SpaceManager.GetSpaceList(iuser, BuildingId);
                    return ConvertDataTableToJson(dt);
                }
            }
            return null;
        }

        public string Assets(int BuildingId)
        {
            if (HttpContext.Current != null)
            {
                System.Security.Principal.IPrincipal iuser = HttpContext.Current.User;
                if (iuser != null)
                {
                    List<tblAsset> lstAsset = new List<tblAsset>();
                    lstAsset = AssetManager.GetAssetsForBuilding(iuser, BuildingId);
                    JArray result = new JArray();


                    if (lstAsset != null && lstAsset.Count > 0)
                    {
                        foreach (tblAsset asset in lstAsset)
                        {
                            JArray spacearray = new JArray();
                            if (asset.tblAssets_SPListSpaces != null && asset.tblAssets_SPListSpaces.Count > 0)
                            {
                                foreach (tblAssets_SPListSpaces sp in asset.tblAssets_SPListSpaces)
                                {
                                    spacearray.Add(sp.fk_space_SPid);
                                }
                            }

                            result.Add(new JObject(
                                        new JProperty("id", asset.id),
                                        new JProperty("itemName", asset.itemName),
                                        new JProperty("assetStatus", string.IsNullOrEmpty(asset.assetStatus) ? string.Empty : asset.assetStatus),
                                        new JProperty("assignedToName", string.IsNullOrEmpty(asset.AssignedToName) ? string.Empty : asset.AssignedToName),
                                        new JProperty("efficiency", (asset.efficiency == null) ? 0.0 : asset.efficiency),
                                        new JProperty("EfficiencyUnit", string.IsNullOrEmpty(asset.EfficiencyUnit) ? string.Empty : asset.EfficiencyUnit),
                                        new JProperty("equipmentTagNumber", string.IsNullOrEmpty(asset.equipmentTagNumber) ? string.Empty : asset.equipmentTagNumber),
                                        new JProperty("expectedReplacementDate", asset.expectedReplacementDate.HasValue ? asset.expectedReplacementDate : null),
                                        new JProperty("imageUrl", string.IsNullOrEmpty(asset.imageUrl) ? string.Empty : asset.imageUrl),
                                        new JProperty("itemDescription", string.IsNullOrEmpty(asset.itemDescription) ? string.Empty : asset.itemDescription),
                                        new JProperty("make", asset.make),
                                        new JProperty("quantity", asset.quantity),
                                        new JProperty("serialNumber", asset.serialNumber),
                                        new JProperty("listSpaces", spacearray),
                                        new JProperty("Vendor", asset.Vendor),
                                        new JProperty("VendorSKU", asset.VendorSKU),
                                        new JProperty("warrantyDescription", asset.warrantyDescription),
                                        new JProperty("warrantyendDate", asset.warrantyendDate)
                                        ));
                        }
                        return result.ToString();
                    }
                }
            }

            return string.Empty;
        }

        public string AssetCategories()
        {
            if (HttpContext.Current != null)
            {
                System.Security.Principal.IPrincipal iuser = HttpContext.Current.User;
                if (iuser != null)
                {
                    JArray result = new JArray();
                    List<tblAssetCategory> assetCategories = new List<tblAssetCategory>();
                    assetCategories = AssetManager.getAllAssetCategories();

                    foreach (tblAssetCategory ac in assetCategories)
                    {
                        result.Add(new JObject(
                             new JProperty("id", ac.id),
                             new JProperty("categoryAbreviation", ac.AssetCatAbreviation),
                             new JProperty("categoryDescription", ac.AssetCateDescription)

                            ));
                    }

                    return result.ToString();
                }
            }
            return string.Empty;
        }

        #endregion

        #region POST

        public string CreateAsset(NewAsset newasset)
        {
            WebOperationContext ctx = WebOperationContext.Current;
            JContainer jsonResult = new JObject();
            try
            {
                if (HttpContext.Current != null)
                {
                    System.Security.Principal.IPrincipal iuser = HttpContext.Current.User;
                    if (iuser != null)
                    {

                        if (string.IsNullOrEmpty(newasset.name) || string.IsNullOrEmpty(newasset.description) || string.IsNullOrEmpty(newasset.category) || string.IsNullOrEmpty(newasset.buildingId))
                        {
                            // required feild message
                            //must not continue
                            string message = "";

                            if (string.IsNullOrEmpty(newasset.name))
                            {
                                message = "Asset Name is required.";
                            }
                            else if (string.IsNullOrEmpty(newasset.description))
                            {
                                message = "Asset Description is required.";
                            }
                            else if (string.IsNullOrEmpty(newasset.category))
                            {
                                message = "Asset Category is required.";
                            }
                            else if (string.IsNullOrEmpty(newasset.buildingId))
                            {
                                message = "Building ID is required.";
                            }

                            ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                            jsonResult = ReturnError(string.Empty, message);
                            return jsonResult.ToString();
                        }

                        // check for building 
                        //first if exist
                        //if user has permission to the building
                        int buildingid = 0;
                        int.TryParse(newasset.buildingId, out buildingid);

                        if (buildingid == 0)
                        {
                            //wrong buildingname
                            //user wrong Input
                            ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                            jsonResult = ReturnError(string.Empty, "Wrong building ID.");
                            return jsonResult.ToString();
                        }
                        else
                        {
                            if (!BuildingManager.IsUserMemberOfBuilding(iuser, buildingid))
                            {
                                //no permission
                                ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                jsonResult = ReturnError(string.Empty, "No permission to create asset.");
                                return jsonResult.ToString();
                            }
                        }

                        //check for category
                        tblAssetCategory assetCategory = null;
                        using (BuiltSpaceDB DbContext = new BuiltSpaceDB())
                        {
                            assetCategory = DbContext.tblAssetCategories.Where(p => p.AssetCatAbreviation == newasset.category).FirstOrDefault();
                        }
                        if (assetCategory == null)
                        {
                            //wrong category abbreviation
                            //must not conitinue
                            //user wrong Input
                            ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                            jsonResult = ReturnError(string.Empty, "Wrong Category Abbreviation");
                            return jsonResult.ToString();
                        }

                       // string contentTypeHeader = WebOperationContext.Current.IncomingRequest.Headers["Content-Type"];
                        tblUserProfile profile = UserProfileManager.GetUserProfile(iuser);
                        int spaceId = -1;
                        string spaceName = string.Empty;

                        using (SPSite site = new SPSite(BuildingManager.GetBuildingURL(buildingid)))
                        {
                            using (SPWeb web = site.OpenWeb())
                            {
                                SPList list = web.Lists["Space Inventory"];
                                foreach (SPListItem i in list.Items)
                                {
                                    spaceName = i["Suite Number"] == null ? string.Empty : i["Suite Number"].ToString();
                                    if (spaceName.Trim().ToLower() == newasset.space.ToLower())
                                    {
                                        spaceId = i.ID;
                                        break;
                                    }
                                }
                            }
                        }
                        if (spaceId == -1)
                        {
                            //must not continue
                            //must hava at least one space
                            //user wrong Input
                            ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                            jsonResult = ReturnError(string.Empty, "Wrong Space Name.");
                            return jsonResult.ToString();
                        }
                        ////if parent asset not provided
                        //if (newasset.parentAsset == "")
                        //    newasset.parentAsset = null;


                        if (!SecurityManager.IsUserAllowedToAddAsset(iuser, buildingid, spaceId))
                        {
                            // must not continue
                            ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                            jsonResult = ReturnError(string.Empty, "No permission to create asset");
                            return jsonResult.ToString();
                        }

                        tblAsset nasset = AssetManager.AddNewAsset(newasset.name, newasset.description, newasset.quantity, newasset.category, newasset.make, newasset.model, newasset.serial, null, newasset.buildingId, spaceId.ToString(), newasset.space, profile);

                        if (nasset != null)
                        {

                            ctx.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                            jsonResult = ReturnSuccess("Asset Created");
                            return jsonResult.ToString();
                        }
                        else
                        {
                            ctx.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
                            jsonResult = ReturnError(string.Empty, "Unknown server error");
                            return jsonResult.ToString();
                        }
                    }
                    else
                    {
                        ctx.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
                        jsonResult = ReturnError(string.Empty, "Unauthorized.");
                        return jsonResult.ToString();
                    }

                }
                else
                {
                    ctx.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
                    jsonResult = ReturnError(string.Empty, "Unauthorized.");
                    return jsonResult.ToString();
                }
            }
            catch (Exception ex)
            {
                ctx.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
                Common.LogException("WCF - Create Asset", ex);
                jsonResult = ReturnError(string.Empty, "Unknown server error");
                return jsonResult.ToString();
            }
        }

        public string CreateTask(NewTask newtaskinfo)
        {
            WebOperationContext ctx = WebOperationContext.Current;
            HttpStatusCode httpstatus = HttpStatusCode.OK;
            JContainer jsonResult = new JObject();
            try
            {
                if (HttpContext.Current != null)
                {
                    System.Security.Principal.IPrincipal iuser = HttpContext.Current.User;
                    if (iuser != null)
                    {
                        int buildingid = 0;
                        int.TryParse(newtaskinfo.buildingId, out buildingid);

                        if (buildingid == 0)
                        {
                            //wrong buildingname
                            //must not continue
                            //#### user error
                            // throw new FaultException<InvalidInputFault>(new InvalidInputFault() { code = "", message = "Building Id" }, "In Valid Input");
                            ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                            jsonResult = ReturnError(string.Empty, "No building Id were provided.");
                            return jsonResult.ToString();
                        }
                        else
                        {
                            if (!BuildingManager.IsUserMemberOfBuilding(iuser, buildingid))
                            {
                                //no permission
                                jsonResult = ReturnError(string.Empty, "No Permissionto create task.");
                                return jsonResult.ToString();
                            }
                        }
                        using (SPSite site = new SPSite(BuildingManager.GetBuildingURL(buildingid)))
                        {
                            using (SPWeb web = site.OpenWeb())
                            {
                                SPList tasklist = web.Lists["BuildingTasks"];
                                SPContentType ctype = tasklist.ContentTypes["Building Task"];

                                web.AllowUnsafeUpdates = true;
                                SPListItem newtask = tasklist.Items.Add();
                                newtask["ContentTypeId"] = ctype.Id;
                                if (!string.IsNullOrEmpty(newtaskinfo.title))
                                {
                                    newtask["Title"] = newtaskinfo.title;
                                }
                                else
                                {
                                    // must not continue
                                    //#### user error
                                    ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                    jsonResult = ReturnError(string.Empty, "No value for Task Title.");
                                    return jsonResult.ToString();
                                }

                                newtask["Details"] = newtaskinfo.details;
                                int assetId = 0;
                                int spaceId = 0;
                                //check whether it was provided 
                                if (!string.IsNullOrEmpty(newtaskinfo.assetId))
                                {
                                    int.TryParse(newtaskinfo.assetId, out assetId);
                                    if (assetId > 0)
                                    {
                                        int assetBuildingId = BuildingManager.GetBuildingIdforAsset(assetId);
                                        if (assetBuildingId == buildingid)
                                        {
                                            newtask["AssetId"] = newtaskinfo.assetId;
                                        }
                                        else
                                        {
                                            //asset wrong
                                            //#### user error
                                            ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                            jsonResult = ReturnError(string.Empty, "Could not find asset.");
                                            return jsonResult.ToString();
                                        }
                                    }
                                    else
                                    {
                                        //not a number
                                        //wrong input
                                        //#### user error
                                        ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        jsonResult = ReturnError(string.Empty, "Could not find asset.");
                                        return jsonResult.ToString();
                                    }
                                }

                                if (!string.IsNullOrEmpty(newtaskinfo.spaceId))
                                {
                                    int.TryParse(newtaskinfo.spaceId, out spaceId);
                                    if (spaceId > 0)
                                    {
                                        string spacename = string.Empty;
                                        spacename = SpaceManager.GetAllSpaceNames(web, assetId);
                                        if (spacename != string.Empty)
                                        {
                                            newtask["SpaceId"] = newtaskinfo.spaceId;
                                        }
                                        else
                                        {
                                            //wrong input
                                            //#### user error
                                            ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                            jsonResult = ReturnError(string.Empty, "Could not find space.");
                                            return jsonResult.ToString();
                                        }
                                    }
                                    else
                                    {
                                        //not a number
                                        //wrong input
                                        //#### user error
                                        ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        jsonResult = ReturnError(string.Empty, "Could not find space.");
                                        return jsonResult.ToString();
                                    }
                                }
                                DateTime startdate;
                                if (!string.IsNullOrEmpty(newtaskinfo.startDate))
                                {
                                    if (DateTime.TryParse(newtaskinfo.startDate, out startdate))
                                    {
                                        newtask["Start Date"] = startdate.ToShortDateString();
                                    }
                                    else
                                    {
                                        //not a date
                                        //wrong input
                                        //#### user error
                                        ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        jsonResult = ReturnError(string.Empty, "Wrong Date format.");
                                        return jsonResult.ToString();
                                    }
                                }
                                DateTime enddate;
                                if (!string.IsNullOrEmpty(newtaskinfo.endDate))
                                {
                                    if (DateTime.TryParse(newtaskinfo.startDate, out enddate))
                                    {
                                        newtask["Due Date"] = enddate.ToShortDateString();

                                    }
                                    else
                                    {
                                        //not a date
                                        //wrong input
                                        //#### user error
                                        ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        jsonResult = ReturnError(string.Empty, "Wrong Due Date format.");
                                        return jsonResult.ToString();
                                    }
                                }
                                newtask["Created"] = DateTime.Now.ToShortDateString();
                                bool invalidEmailIput = false;
                                if (!string.IsNullOrEmpty(newtaskinfo.assignedToEmail))
                                {

                                    SPUser spuser = null;
                                    try
                                    {
                                        spuser = web.EnsureUser("i:05.t|jakedabuiltspaceissuer|" + newtaskinfo.assignedToEmail);
                                        invalidEmailIput = false;
                                    }
                                    catch (Exception)
                                    {
                                        invalidEmailIput = true;
                                    }
                                    if (spuser != null)
                                    {
                                        newtask["Assigned To"] = spuser;
                                    }

                                    else
                                    {
                                        try
                                        {
                                            string groupname = "BuiltSpace " + newtaskinfo.assignedToEmail;
                                            SPGroup spgroup = web.Groups[groupname];
                                            invalidEmailIput = false;
                                        }
                                        catch (Exception)
                                        {
                                            invalidEmailIput = true;
                                        }
                                    }
                                }
                                if (invalidEmailIput)
                                {
                                    // wrong input
                                    //#### user error
                                    ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                    jsonResult = ReturnError(string.Empty, "Could not find user or group for assign to.");
                                    return jsonResult.ToString();
                                }


                                newtask["Reporting User Contact"] = "na";

                                //check if task status were provided
                                if (!string.IsNullOrEmpty(newtaskinfo.state))
                                {
                                    string[] status = new string[5] { "Open", "In Progress", "Waiting", "Completed", "Closed" };
                                    if (status.Contains(newtaskinfo.state))
                                    {
                                        newtask["State"] = newtaskinfo.state;
                                    }
                                    else
                                    {
                                        //wrong input
                                        //#### user error
                                        jsonResult = ReturnError(string.Empty, "Wrong status value.");
                                        return jsonResult.ToString();
                                    }
                                }
                                else
                                {
                                    newtask["State"] = "Open";
                                }
                                if (!string.IsNullOrEmpty(newtaskinfo.priority))
                                {
                                    string[] priority = new string[3] { "(1) High", "(2) Normal", "(3) Low" };
                                    if (priority.Contains(newtaskinfo.priority))
                                        newtask["Priority"] = newtaskinfo.priority;
                                    else
                                    {
                                        //wrong input
                                        //#### user error
                                        ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        jsonResult = ReturnError(string.Empty, "Wrong Priority value.");
                                        return jsonResult.ToString();
                                    }
                                }
                                else
                                {
                                    newtask["Priority"] = "(2) Normal";
                                }

                                if (!string.IsNullOrEmpty(newtaskinfo.category))
                                {
                                    string[] category = new string[5] { "General", "HVAC", "Plumbing", "Electrical", "Cleaning" };
                                    if (category.Contains(newtaskinfo.category))
                                        newtask["Category"] = newtaskinfo.category;
                                    else
                                    {
                                        ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        jsonResult = ReturnError(string.Empty, "Wrong Category value.");
                                        return jsonResult.ToString();
                                    }
                                }
                                else
                                {
                                    newtask["Category"] = "";
                                }

                                if (!string.IsNullOrEmpty(newtaskinfo.workOrderNo))
                                {
                                    newtask["Work Order No"] = newtaskinfo.workOrderNo;
                                }

                                if (!string.IsNullOrEmpty(newtaskinfo.totalHours))
                                {
                                    decimal totalhours = 0;
                                    if (decimal.TryParse(newtaskinfo.totalHours, out totalhours))
                                    {
                                        newtask["Total Hours"] = newtaskinfo.totalHours;
                                    }
                                    else
                                    {
                                        /// wrong input
                                        //#### user error
                                        ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        jsonResult = ReturnError(string.Empty, "wrong input for Total Hours.");
                                        return jsonResult.ToString();
                                    }
                                }
                                if (!string.IsNullOrEmpty(newtaskinfo.totalCost))
                                {
                                    decimal totalCost = 0;
                                    if (decimal.TryParse(newtaskinfo.totalCost, out totalCost))
                                    {
                                        newtask["Total Cost"] = newtaskinfo.totalCost;
                                    }
                                    else
                                    {
                                        //wrong input
                                        //#### user error
                                        ctx.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        jsonResult = ReturnError(string.Empty, "wrong input for Total Cost.");
                                        return jsonResult.ToString();
                                    }
                                }
                                newtask.Update();
                                web.AllowUnsafeUpdates = false;

                                ctx.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                jsonResult = ReturnSuccess("Asset Created");
                                return jsonResult.ToString();
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                ctx.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
                Common.LogException("WCF - Create Task", ex);
                jsonResult = ReturnError(string.Empty, "Unknown server error");
                return jsonResult.ToString();

            }
        }

        #endregion

        #region Private functions

        private string ConvertDataTableToJson(DataTable dt)
        {
            System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            Dictionary<string, object> row;

            foreach (DataRow dr in dt.Rows)
            {
                row = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    row.Add(col.ColumnName, dr[col]);
                }
                rows.Add(row);
            }
            return serializer.Serialize(rows);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private JContainer ReturnError(string code, string message)
        {

            JArray result = new JArray();
            result.Add((new JObject(new JProperty("error",
                                new JObject(new JProperty("code", code),
                                             new JProperty("message",
                                                                     new JObject(new JProperty("lang", "en-US"),
                                                                     new JProperty("value", message))
                ))
              ))));

            return result;
            //JavaScriptSerializer serializer = new JavaScriptSerializer();
            //serializer.RecursionLimit = recursionDepth;
            //return serializer.Serialize(result);
        }

        private JContainer ReturnSuccess(string message)
        {
            JArray result = new JArray();
            result.Add((new JObject(new JProperty("status", message))));
            return result;
        }

        #endregion
    }

}
