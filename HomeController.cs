using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using LJOB.Models;
using System;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Identity;
using System.Data.SqlTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.Data.SqlClient;
using System.IO;
using Azure.Core;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MyIdea.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment environment;


        public HomeController(IWebHostEnvironment env)
        {
            environment = env;

        }

        public string GetconnectionString(string TheBase)
        {
            return environment.IsDevelopment()
              ? @$"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={environment.ContentRootPath}\{TheBase};Integrated Security=True;Connect Timeout=15"
              : @$"Data Source=.\SQLEXPRESS;Integrated Security=SSPI;AttachDBFilename={environment.ContentRootPath}\{TheBase};User Instance=true;Connect Timeout=15";
        }



        [HttpGet]
        public ActionResult Index()
        {
            return View("IndexH");
        }


        public ActionResult IndexH()
        {
            DataTable localTbl = new DataTable();
            using (SqlConnection connection = new SqlConnection(GetconnectionString("Job.mdf")))
            {
                SqlDataAdapter sqlDa = new SqlDataAdapter("SELECT * FROM Users", connection);
                connection.Open();
                sqlDa.Fill(localTbl);

                return View(localTbl);
            }
            
        }



        [HttpGet]

        public ActionResult SignUp()
        {
            return View();
        }



        [HttpPost]
        public ActionResult SignUp(IFormFile file)
        {
            string firstName = Request.Form["Förnamn"];
            string lastName = Request.Form["Efternamn"];
            string username = Request.Form["Username"];
            string password = Request.Form["lösenord"];
            string email = Request.Form["Mail"];
            string Profilbild = Request.Form["Profilbild"];

            using (SqlConnection connection = new SqlConnection(GetconnectionString("Job.mdf")))
            {
                connection.Open();


                string checkUserQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                using (SqlCommand checkUserCmd = new SqlCommand(checkUserQuery, connection))
                {
                    checkUserCmd.Parameters.AddWithValue("@Username", username);
                    int userCount = (int)checkUserCmd.ExecuteScalar();
                    if (userCount > 0)
                    {
                        ViewData["Error"] = "Användarnamnet är redan taget.";
                        return View();
                    }
                }


                string insertQuery = "INSERT INTO Users (Förnamn, Efternamn, Username, lösenord, Mail, Profilbild) VALUES(@name, @lastname, @User, @Psw, @Email, @Pic)";
                using (SqlCommand sqlCmd = new SqlCommand(insertQuery, connection))
                {
                    sqlCmd.Parameters.AddWithValue("@name", firstName);
                    sqlCmd.Parameters.AddWithValue("@lastname", lastName);
                    sqlCmd.Parameters.AddWithValue("@User", username);
                    sqlCmd.Parameters.AddWithValue("@Psw", password);
                    sqlCmd.Parameters.AddWithValue("@Email", email);


                    if (file == null)
                    {
                        sqlCmd.Parameters.AddWithValue("@Pic", "dummy.jpeg");
                        sqlCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        HttpContext.Session.SetString(userimg, file.FileName);
                        var filePath = Path.Combine(this.environment.ContentRootPath, "wwwroot/images/", file.FileName);
                        using (var stream = System.IO.File.Create(filePath))
                        {
                            file.CopyTo(stream);
                        }
                        sqlCmd.ExecuteNonQuery();
                    }
                }



                string selectQuery = "SELECT TOP 1 * FROM Users WHERE Username = @Username ORDER BY UserID DESC";
                using (SqlCommand selectCmd = new SqlCommand(selectQuery, connection))
                {
                    selectCmd.Parameters.AddWithValue("@Username", username);
                    using (SqlDataReader reader = selectCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            HttpContext.Session.SetInt32(UserID, Convert.ToInt32(reader["UserID"]));
                            HttpContext.Session.SetString(UserName, reader["Username"].ToString());
                            HttpContext.Session.SetString(Inloggad, "yes");
                        }
                    }
                }
            }

            return RedirectToAction("IndexH");
        }




        public const string UserID = "_UserID";
        public const string UserName = "_Uname";
        public const string Inloggad = "_InloggStatus";
        public const string userimg = "_userimg";

        [HttpPost]
        public IActionResult Log()
        {
            DataTable Users = new DataTable();
            using (SqlConnection sqlCon = new SqlConnection(GetconnectionString("Job.mdf")))
            {
                sqlCon.Open();
                string query = "SELECT * FROM Users WHERE Username = @User AND lösenord = @Psw";
                SqlDataAdapter sqlDa = new SqlDataAdapter(query, sqlCon);
                sqlDa.SelectCommand.Parameters.AddWithValue("@User", Request.Form["Username"].ToString());
                sqlDa.SelectCommand.Parameters.AddWithValue("@Psw", Request.Form["lösenord"].ToString());
                sqlDa.Fill(Users);
            }

            if (Users.Rows.Count > 0)
            {

                HttpContext.Session.SetString("LvlUser", Users.Rows[0]["LvlUser"].ToString());
                HttpContext.Session.SetInt32(UserID, Convert.ToInt32(Users.Rows[0]["UserID"]));
                HttpContext.Session.SetString(UserName, Users.Rows[0]["Username"].ToString());
                HttpContext.Session.SetString(userimg, Users.Rows[0]["Profilbild"].ToString());
                HttpContext.Session.SetString(Inloggad, "yes");


                ViewData["UserID"] = HttpContext.Session.GetInt32(UserID);
                ViewData["Username"] = HttpContext.Session.GetString(UserName);
                ViewData["text"] = "inloggad";

                return RedirectToAction("IndexH");
            }
            else
            {
                ViewData["text"] = "ej inloggad";
                return View("Log");
            }
        }

        public IActionResult log()
        {
            return View("log");
        }

        public IActionResult LogOut()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("IndexH");
        }


        public ActionResult NyAnsökan ()
        {
            return View();
        }





        [HttpPost]

        public IActionResult Job()
        {
            jobModel job = new jobModel();  
            using (SqlConnection connection = new SqlConnection(GetconnectionString("Job.mdf")))
            {
                connection.Open();
                string query = "INSERT INTO Jobs (JobTitel,company, position, location, appliedDate, status, description, salary, applicationUrl, contactPerson, notes) VALUES (@Jobtitel,@company, @position, @location, @appliedDate, @status, @description, @salary, @applicationUrl, @contactPerson, @notes)";
                using (SqlCommand sqlCmd = new SqlCommand(query, connection))
                {
                    sqlCmd.Parameters.AddWithValue("@Jobtitel", job.jobTitle);
                    sqlCmd.Parameters.AddWithValue("@company", job.company);
                    sqlCmd.Parameters.AddWithValue("@position", job.position);
                    sqlCmd.Parameters.AddWithValue("@location", job.location);
                    sqlCmd.Parameters.AddWithValue("@appliedDate", job.appliedDate);
                    sqlCmd.Parameters.AddWithValue("@status", job.status);
                    sqlCmd.Parameters.AddWithValue("@description", job.description);
                    sqlCmd.Parameters.AddWithValue("@salary", job.salary);
                    sqlCmd.Parameters.AddWithValue("@applicationUrl", job.applicationUrl);
                    sqlCmd.Parameters.AddWithValue("@contactPerson", job.contactPerson);
                    sqlCmd.Parameters.AddWithValue("@notes", job.notes);
                    sqlCmd.ExecuteNonQuery();
                }



            }
            return RedirectToAction("NyAnsökan");
        }
























    }
}
