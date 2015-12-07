using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;
using System.Web.Services;
using System.Data;
using Jakeda.BuiltSpace.Services.Entity;
using Newtonsoft.Json.Linq;


namespace Jakeda.BuiltSpace.Platform.ISAPI
{

    [ServiceContract]

    public interface IBuildingData
    {

        [OperationContract(Name = "Spaces")]
        [WebInvoke(UriTemplate = "/Spaces?BuildingId={BuildingId}", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        string Spaces(int BuildingId);

        [OperationContract(Name = "Assets")]
        [WebInvoke(UriTemplate = "/Assets?BuildingId={BuildingId}", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        string Assets(int BuildingId);

        [OperationContract(Name = "AssetCategories")]
        [WebInvoke(UriTemplate = "/Assets/Categories", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        string AssetCategories();

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/CreateAsset", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        string CreateAsset(NewAsset asset);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/CreateTask", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        string CreateTask(NewTask task);
    }

    [DataContract]

    public class GreaterThan3Fault
    {

        [DataMember]

        public string FaultMessage { get; set; }

        [DataMember]

        public int ErrorCode { get; set; }

        [DataMember]

        public string Location { get; set; }

    }

    [DataContract]
    public class NewAsset
    {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string description { get; set; }
        [DataMember]
        public string quantity { get; set; }
        [DataMember]
        public string space { get; set; }
        [DataMember]
        public string category { get; set; }
        [DataMember]
        public string make { get; set; }
        [DataMember]
        public string model { get; set; }
        [DataMember]
        public string serial { get; set; }
        [DataMember]
        public string buildingId { get; set; }

        public NewAsset(string _name, string _description, string _quantity, string _space, string _category, string _make, string _model, string _serial, string _buildingId)
        {
            this.name = _name;
            this.description = _description;
            this.quantity = _quantity;
            this.space = _space;
            this.category = _category;
            this.make = _make;
            this.model = _model;
            this.serial = _serial;
            this.buildingId = _buildingId;
        }
    }

    [DataContract]
    public class NewTask
    {
        [DataMember]
        public string buildingId { get; set; }
        [DataMember]
        public string title { get; set; }
        [DataMember]
        public string details { get; set; }
        [DataMember]
        public string assetId { get; set; }
        [DataMember]
        public string spaceId { get; set; }
        [DataMember]
        public string startDate { get; set; }
        [DataMember]
        public string endDate { get; set; }
        [DataMember]
        public string assignedToEmail { get; set; }
        [DataMember]
        public string state { get; set; }
        [DataMember]
        public string priority { get; set; }
        [DataMember]
        public string category { get; set; }
        [DataMember]
        public string workOrderNo { get; set; }
        [DataMember]
        public string totalHours { get; set; }
        [DataMember]
        public string totalCost { get; set; }

        public NewTask(string _title, string _details, string _assetId, string _spaceId, string _startDate,
            string _endDate, string _assignedTo, string _state, string _priority, string _category, string _workOrderNo,
            string _totalHours, string _totalCost, string _buildingId)
        {
            this.buildingId = _buildingId;
            this.title = _title;
            this.details = _details;
            this.assetId = _assetId;
            this.spaceId = _spaceId;
            this.startDate = _startDate;
            this.endDate = _endDate;
            this.assignedToEmail = _assignedTo;
            this.state = _state;
            this.priority = _priority;
            this.category = _category;
            this.workOrderNo = _workOrderNo;
            this.totalHours = _totalHours;
            this.totalCost = _totalCost;
        }
    }

    [DataContract]
    [Serializable()]
    public class InvalidInputFault
    {
        [DataMember]
        public string code { get; set; }
        [DataMember]
        public string message { get; set; }
    }
}
