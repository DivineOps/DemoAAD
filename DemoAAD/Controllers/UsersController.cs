using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using DemoAAD.Utils;
using Microsoft.Azure.ActiveDirectory.GraphClient;

namespace DemoAAD.Controllers
{
    /// <summary>
    /// User controller to get/set/update/delete users.
    /// </summary>
    [Authorize]
    public class UsersController : Controller
    {

        /// <summary>
        /// Gets a list of <see cref="User"/> objects from Graph.
        /// </summary>
        /// <returns>A view with the list of <see cref="User"/> objects.</returns>
        public ActionResult Index()
        {
            //Get the access token as we need it to make a call to the Graph API
            var accessToken = AuthUtils.GetAuthToken(Request, HttpContext);
            if (accessToken == null)
            {
                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            // Setup Graph API connection and get a list of users
            var clientRequestId = Guid.NewGuid();
			var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
			var graphConnection = new GraphConnection(accessToken, clientRequestId, graphSettings);
            
            var pagedReslts = graphConnection.List<User>(null, new FilterGenerator());
            return View(pagedReslts.Results);
        }

        /// <summary>
        /// Gets details of a single <see cref="User"/> Graph.
        /// </summary>
        /// <returns>A view with the details of a single <see cref="User"/>.</returns>
        public ActionResult Details(string objectId)
        {
            //Get the access token as we need it to make a call to the Graph API
			var accessToken = AuthUtils.GetAuthToken(Request, HttpContext);
            if (accessToken == null)
            {
                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            // Setup Graph API connection and get single User
			var clientRequestId = Guid.NewGuid();
			var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
			var graphConnection = new GraphConnection(accessToken, clientRequestId, graphSettings);

			var user = graphConnection.Get<User>(objectId);
            return View(user);
        }

        /// <summary>
        /// Creates a view to for adding a new <see cref="User"/> to Graph.
        /// </summary>
        /// <returns>A view with the details to add a new <see cref="User"/> objects</returns>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processes creation of a new <see cref="User"/> to Graph.
        /// </summary>
        /// <returns>A view with the details to all <see cref="User"/> objects</returns>
        [HttpPost]
        public ActionResult Create([Bind(Include="UserPrincipalName,AccountEnabled,PasswordProfile,MailNickname,DisplayName,GivenName,Surname,JobTitle,Department")] User user)
        {
            //Get the access token as we need it to make a call to the Graph API
			var accessToken = AuthUtils.GetAuthToken(Request, HttpContext);
            if (accessToken == null)
            {
                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }
        
            try
            {
                // Setup Graph API connection and add User
				var clientRequestId = Guid.NewGuid();
				var graphSettings = new GraphSettings {ApiVersion = GraphConfiguration.GraphApiVersion};
	            var graphConnection = new GraphConnection(accessToken, clientRequestId, graphSettings);

                graphConnection.Add(user);
                return RedirectToAction("Index");
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View();
            }
        }

        /// <summary>
        /// Creates a view to for editing an existing <see cref="User"/> in Graph.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="User"/>.</param>
        /// <returns>A view with details to edit <see cref="User"/>.</returns>
        public ActionResult Edit(string objectId)
        {
            //Get the access token as we need it to make a call to the Graph API
            string accessToken = AuthUtils.GetAuthToken(Request, HttpContext);
            if (accessToken == null)
            {
                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            // Setup Graph API connection and get single User
			var clientRequestId = Guid.NewGuid();
			var graphSettings = new GraphSettings {ApiVersion = GraphConfiguration.GraphApiVersion};
	        var graphConnection = new GraphConnection(accessToken, clientRequestId, graphSettings);

			var user = graphConnection.Get<User>(objectId);
            return View(user);
        }

        /// <summary>
        /// Gets a list of <see cref="Group"/> objects that a given <see cref="User"/> is member of.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="User"/>.</param>
        /// <returns>A view with the list of <see cref="Group"/> objects.</returns>
        public ActionResult GetGroups(string objectId)
        {

            //Get the access token as we need it to make a call to the Graph API
            string accessToken = AuthUtils.GetAuthToken(Request, HttpContext);
            if (accessToken == null)
            {
                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }
            // Setup Graph API connection and get Group membership
			var clientRequestId = Guid.NewGuid();
			var graphSettings = new GraphSettings {ApiVersion = GraphConfiguration.GraphApiVersion};
	        var graphConnection = new GraphConnection(accessToken, clientRequestId, graphSettings);

            GraphObject graphUser = graphConnection.Get<User>(objectId);
            var memberShip = graphConnection.GetLinkedObjects(graphUser, LinkProperty.MemberOf, null, 999);

            // users can be members of Groups and Roles
            // for this app, we will filter and only show Groups

			var count = 0;
            IList<Group> groupMembership = new List<Group>();
			foreach (var graphObj in memberShip.Results)
            {
                if (graphObj.ODataTypeName.Contains("Group"))
                {
					var groupMember = (Group)memberShip.Results[count];
                    groupMembership.Add(groupMember);
                }
                ++count;
            }

            //return View(groupIds);
            return View(groupMembership);
        }

        /// <summary>
        /// Processes editing of an existing <see cref="User"/>.
        /// </summary>
        /// <param name="user"><see cref="User"/> to be edited.</param>
        /// <returns>A view with list of all <see cref="User"/> objects.</returns>
        [HttpPost]
        public ActionResult Edit([Bind(Include = "ObjectId,UserPrincipalName,DisplayName,AccountEnabled,GivenName,Surname,JobTitle,Department,Mobile,StreetAddress,City,State,Country,")] User user, FormCollection values)
        {
            //Get the access token as we need it to make a call to the Graph API
            string accessToken = AuthUtils.GetAuthToken(Request, HttpContext);
            if (accessToken == null)
            {
                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }
           
            try
            {
                // TODO: Add update logic here

                // Setup Graph API connection and update single User
				var clientRequestId = Guid.NewGuid();
				var graphSettings = new GraphSettings {ApiVersion = GraphConfiguration.GraphApiVersion};
	            var graphConnection = new GraphConnection(accessToken, clientRequestId, graphSettings);
                graphConnection.Update(user);

                // update thumbnail photo 



                if (!String.IsNullOrEmpty(values["photofile"]))
                {

                  //string path = AppDomain.CurrentDomain.BaseDirectory + "uploads/";
                  //string filename = Path.GetFileName(values["photofile"]);
                  //Image image = Image.FromFile(filename);
              
                    var imageFile = Path.Combine(Server.MapPath("~/app_data"), values["photofile"]);
					var image = Image.FromFile(imageFile);

					var stream = new MemoryStream();
                  image.Save(stream, ImageFormat.Jpeg);
                    
                  // Write the photo file to the Graph service.                    
                  graphConnection.SetStreamProperty(user, GraphProperty.ThumbnailPhoto, stream, "image/jpeg");
                  
                }


                return RedirectToAction("Index");
            }
            catch(Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View();
            }
        }

        /// <summary>
        /// Creates a view to delete an existing <see cref="User"/>.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="User"/>.</param>
        /// <returns>A view of the <see cref="User"/> to be deleted.</returns>
        public ActionResult Delete(string objectId)
        {
            //Get the access token as we need it to make a call to the Graph API
            string accessToken = AuthUtils.GetAuthToken(Request, HttpContext);
            if (accessToken == null)
            {
                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            try
            {
                // Setup Graph API connection and get single User
                var clientRequestId = Guid.NewGuid();
                var graphSettings = new GraphSettings {ApiVersion = GraphConfiguration.GraphApiVersion};
	            var graphConnection = new GraphConnection(accessToken, clientRequestId, graphSettings);
                var user = graphConnection.Get<User>(objectId);
                return View(user);
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View();
            }
        }

        /// <summary>
        /// Processes the deletion of a given <see cref="User"/>.
        /// </summary>
        /// <param name="user"><see cref="User"/> to be deleted.</param>
        /// <returns>A view to display all the existing <see cref="User"/> objects.</returns>
        [HttpPost]
        public ActionResult Delete(User user)
        {
            //Get the access token as we need it to make a call to the Graph API
            string accessToken = AuthUtils.GetAuthToken(Request, HttpContext);
            if (accessToken == null)
            {
                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            try
            {
                // Setup Graph API connection and delete User
				var clientRequestId = Guid.NewGuid();
				var graphSettings = new GraphSettings {ApiVersion = GraphConfiguration.GraphApiVersion};
	            var graphConnection = new GraphConnection(accessToken, clientRequestId, graphSettings);
                graphConnection.Delete(user);
                return RedirectToAction("Index");
            }
            catch(Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View(user);
            }
        }

        /// <summary>
        /// Gets a list of <see cref="User"/> objects that a given <see cref="User"/> has as a direct report.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="User"/>.</param>
        /// <returns>A view with the list of <see cref="User"/> objects.</returns>
        public ActionResult GetDirectReports(string objectId)
        {

            //Get the access token as we need it to make a call to the Graph API
            string accessToken = AuthUtils.GetAuthToken(Request, HttpContext);
            if (accessToken == null)
            {
                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            // Setup Graph API connection and get Group membership
            Guid ClientRequestId = Guid.NewGuid();
            GraphSettings graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            GraphConnection graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

            GraphObject graphUser = graphConnection.Get<User>(objectId);
            IList<GraphObject> results = graphConnection.GetAllDirectLinks(graphUser, LinkProperty.DirectReports);
            IList<User> reports = new List<User>();
            foreach (GraphObject obj in results)
            {
                if (obj is User)
                {
                    User user = (User)obj;
                    reports.Add(user);
                }
                
            }
            return View(reports);
        }

        public ActionResult ShowThumbnail(string id)
        {
            string accessToken = AuthUtils.GetAuthToken(Request, HttpContext);
            if (accessToken == null)
            {
                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            // Setup Graph API connection and get Group membership
            Guid ClientRequestId = Guid.NewGuid();
            GraphSettings graphSettings = new GraphSettings();
            graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
            GraphConnection graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

          //  User user = graphConnection.Get<User>(id);
            User user = new User();
            user.ObjectId = id;

            try
            {
                Stream ms = graphConnection.GetStreamProperty(user, GraphProperty.ThumbnailPhoto, "image/jpeg");
                user.ThumbnailPhoto = ms;
            }
            catch
            {
                user.ThumbnailPhoto = null;
            }
             

            if (user.ThumbnailPhoto != null)
            {
                return File(user.ThumbnailPhoto, "image/jpeg");
            }

            return View();
        }
    }
}
