IMPLEMENTING BASIC AUTHENTICATION IN WEB API

1. Create Folder ApiSecurity;

2. Create table named "nApiSecurity"
    CREATE TABLE [dbo].[nApiSecurity](
    	[id] [int] IDENTITY(1,1) NOT NULL,
    	[username] [varchar](255) NULL,
    	[pass] [varchar](255) NULL,
    	[createDate] [datetime] NULL,
    	[createdBy] [varchar](255) NULL,
    	[modDate] [datetime] NULL,
    	[modBy] [varchar](255) NULL,
    	[company] [varchar](255) NULL,
    	[program] [varchar](255) NULL
    )

3. Insert data into "nApiSecurity"

4. Create a class that checks if the credential is valid
  A. Add a new class file to "ApiSecurity" folder. Name it APISecurity.cs
  B. Copy and paste the following code in it

  using System.Data;
  using System.Data.SqlClient;
  using System.Configuration;
  using ApiDemo.Models;

  public static bool ApiSecurityCredential(string username,string password,string company,string program) {
      string MASTER = ConfigurationManager.ConnectionStrings["MASTER"].ConnectionString;
      string str = "";
      SqlCommand cmd;

      using (SqlConnection cn = new SqlConnection(MASTER)) {
          cn.Open();
          str = "EXEC nspApiSecurity " +
                "@function='checkCredential', " +
                "@username = '" + username + "', " +
                "@pass ='" + password + "', " +
                "@company='"+ company +"', " +
                "@program='"+ program +"' ";
          cmd = new SqlCommand(str, cn);
          cmd.CommandTimeout = 0;
          str = Convert.ToString(cmd.ExecuteScalar());

          if (str != null) {
              return true;
          }
          else
          {
              return false;
          }

      }
  }

3. Create a class that filter the AUTHENTICATION
  A. Add a new class file to "ApiSecurity" folder. Name it BasicAuthenticationAttribute.cs
  B. Copy and paste the following code in it

  using System.Web.Http.Filters;
  using System.Net;
  using System.Net.Http;
  using System.Text;
  using System.Threading;
  using System.Security.Principal;

  public class BasicAuthenticationAttribute : AuthorizationFilterAttribute
  {
      public override void OnAuthorization(HttpActionContext actionContext)
      {
          if (actionContext.Request.Headers.Authorization == null)
          {
              actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
          }
          else {
              string authenticationToken = actionContext.Request.Headers.Authorization.Parameter;
              string decodeAuthenticationToken = Encoding.UTF8.GetString(Convert.FromBase64String(authenticationToken));
              string [] sysData = decodeAuthenticationToken.Split(':');
              string username = sysData[0];
              string password = sysData[1];
              string company = sysData[2];
              string program = sysData[3];

              bool retValue = ApiSecurity.ApiSecurityCredential(username,password,company,program);
              if (retValue == true) {
                  Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(username + ":" + password + ":" + company + ":" + program), null);
              }
              else {
                  actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
              }
          }
      }
  }

4. Create new folder "_DAL"
5. Create new class for api logs
  A. Add a new class file to "_DAL" folder. Name it DALTools.cs
  B. Copy and paste the following code in it

  using System.Data;
  using System.Data.SqlClient;
  using System.Configuration;

  private string master = ConfigurationManager.ConnectionStrings["MASTER"].ConnectionString;
  private string str = "";
  SqlCommand cmd;
  DataTable dt = new DataTable();
  int i = 0;

  public string InsertAPISecutityLogs(string resources,string apiLogs) {
      try
      {
          using (SqlConnection cn = new SqlConnection(master))
          {
              cn.Open();
              string[] sysData = apiLogs.Split(':');
              string apiCredential = sysData[0] + "/" + sysData[1];
              string company = sysData[2];
              string program = sysData[3];
              try
              {
                  str = "EXEC nspApiSecurity " +
                        "@function ='inserAPILogs', " +
                        "@resources ='" + resources + "', " +
                        "@apiCredential ='" + apiCredential + "', " +
                        "@company ='" + company + "', " +
                        "@program ='" + program + "' ";
                  cmd = new SqlCommand(str, cn);
                  cmd.CommandTimeout = 0;
                  cmd.ExecuteNonQuery();

                  return "DONE";
              }
              catch (Exception e) {
                  return e.Message;
              }
          }
      }
      catch (Exception e) {
          return e.Message;
      }
  }

4. Enable BasicAuthenticationAttribute to your API with specific methods
  using System.Threading;
  using ApiDemo._DAL; // TO CALL API LOGS
  using ApiDemo.ApiSecurity;  // TO CALL BASIC Authorization

  [BasicAuthentication] //ENABLE BASIC AUTHENTICATION
  [HttpPost]
  public HttpResponseMessage GetBranch([FromBody] BranchDataModel[] sysParam) {
      try
      {
          using (SqlConnection cn = new SqlConnection(MASTER))
          {
              cn.Open();
              try
              {
                  string apiSecurityLogs = Thread.CurrentPrincipal.Identity.Name; //GET THE RESPONSE OF THREAD FROM THE API AUTHENTICATION /APO TOKEN
                  string retDAL = DALTools.InsertAPISecutityLogs("[api/GetMethod/GetBranch]",apiSecurityLogs); // INSERT TO LOGS
                  if (retDAL != "DONE") //CHECK IF THE INSERT LOGS IS SUCCESSFULL OR NOT
                  {
                      return Request.CreateResponse(HttpStatusCode.Conflict,"apiLogs");
                  }


                  List<yourModelName> sysData = new List<yourModelName>();
                  str = "SELECT SCRIPT";
                  cmd = new SqlCommand(str, cn);
                  cmd.CommandTimeout = 0;
                  dt.Load(cmd.ExecuteReader());
                  if (dt.Rows.Count > 0)
                  {
                      for (i = 0; i < dt.Rows.Count; i++)
                      {
                          sysData.Add(new yourModelName
                          {
                              id = dt.Rows[i][""].ToString(),
                              name = dt.Rows[i][""].ToString(),
                              age = dt.Rows[i][""].ToString(),
                          });
                      }
                      return Request.CreateResponse(HttpStatusCode.OK, sysData);
                  }
                  else
                  {
                      return Request.CreateResponse(HttpStatusCode.NotFound, "Data not found.");
                  }
              }
              catch (Exception e)
              {
                  return Request.CreateResponse(HttpStatusCode.Conflict, e.Message);
              }
          }
      }
      catch (Exception e) {
          return Request.CreateResponse(HttpStatusCode.RequestTimeout, "Network problem.");
      }
  }

5. Consume api with Authorization using fiddler
  url : http://localhost:49871/api/GetMethod/GetBranch
  header :
            Accept: application/json
            Content-Type: application/json
            Authorization: Toke yourTokenBased64
  Request Body :
            [{
            "branchCode":"",
            "brancName":""
            }]

6. Consume api with Authorization using Jquery

  function yourFunction(callback) {
      vdtl = {}
      dtl = [];

      vdtl["name"] = "";
      vdtl["age"] = "";
      dtl.push(vdtl);

      $.ajax({
          url: "http://domain:port/api/apiname/methodname",
          type: "POST",
          datatype: "JSON",
          data: JSON.stringify(dtl),
          headers: {
              'Authorization' : "Token tokenbased64",
          },
          contentType: "application/json; charset=utf-8",
          success: function (data) {
              if (data.length > 0) {
                  dtl = [];
                  $.each(data, function (i, val) {
                      vdtl = {};
                      vdtl["name"] = val.name;
                      vdtl["val"] = val.age;
                      dtl.push(vdtl);
                  });
                  callback(dtl);
              }
          }
      });
  }

7. convert your credential to based 64
   go to https://www.base64encode.org/ and paster your credential devided by ":"
   sample credential [adminUsername:adminPassword]
   
