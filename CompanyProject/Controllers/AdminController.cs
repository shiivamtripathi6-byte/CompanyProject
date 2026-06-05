using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using CompanyProject.Models;
using System.Net;
using System.Net.Mail;

namespace CompanyProject.Controllers
{
    public class AdminController : Controller
    {
        DbManager db = new DbManager();
        //
        // GET: /Admin/
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Dashboard()
        {
            try
            {
                if (Session["UserId"] == null ||
                Session["Role"] == null ||
                Session["Role"].ToString().Trim().ToLower() != "admin")
                {
                    return RedirectToAction("Login", "User");
                }
                DataTable dt = db.GetDataSP("sp_Book_Select");
                ViewBag.BookList = dt;

                SqlParameter[] param1 =
                {
                  new SqlParameter("@UserId", Session["UserId"])
                };
                DataTable dtBooked = db.GetDataSP("sp_User_BookedBooks", param1);
                ViewBag.BookedBooks = dtBooked;
                DataTable dtDashboard = db.GetDataSP("sp_Admin_Dashboard");
                if (dtDashboard.Rows.Count > 0)
                {
                    ViewBag.TotalUsers = dtDashboard.Rows[0]["TotalUsers"];
                    ViewBag.TotalBooks = dtDashboard.Rows[0]["TotalBooks"];
                    ViewBag.IssuedBooks = dtDashboard.Rows[0]["IssuedBooks"];
                    ViewBag.ReturnedBooks = dtDashboard.Rows[0]["ReturnedBooks"];
                    ViewBag.AvailableBooks = dtDashboard.Rows[0]["AvailableBooks"];
                }
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 55, "AdminController");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View();
        }
        [HttpGet]
        public ActionResult Add_User(int? id)
        {
           AddUser user = new AddUser();
          try
                {
                    if (id != null)
                    {
                        string query = "SELECT * FROM Add_Users WHERE UserId=@UserId";
                        SqlParameter[] param = 
                    {
                       new SqlParameter("@UserId", id)
                    };
                        DataTable dt = db.GetData(query, param);

                        if (dt.Rows.Count > 0)
                        {
                            user.UserId = Convert.ToInt32(dt.Rows[0]["UserId"]);
                            user.Name = dt.Rows[0]["Name"].ToString();
                            user.Mobile = dt.Rows[0]["Mobile"].ToString();
                            user.City = dt.Rows[0]["City"].ToString();
                            user.Password = dt.Rows[0]["Password"].ToString();
                            user.Status = Convert.ToBoolean(dt.Rows[0]["Status"]);
                            user.Email = dt.Rows[0]["Email"].ToString(); 
                            user.Role = dt.Rows[0]["Role"].ToString();
                            user.DateOFRegistration = Convert.ToDateTime(dt.Rows[0]["DateOfRegistration"]);
                        }
                    }
                }
                catch(Exception ex)
                {
                    db.FailSave(ex.Message, 91, "Admin/Add_User GET");
                    string Message = "alert('Something went wrong! Please try again later.')";
                }
            return View(user);
        }
        [HttpPost]
        public ActionResult Add_User(AddUser user)
        {
            if(!ModelState.IsValid)
            {
                return View(user);
            }
            try
            {
             if (string.IsNullOrEmpty(user.Role))
                user.Role = "User";
                user.Status = true;
                string checkQuery = "select Email, Mobile from Add_Users where (Email=@Email OR Mobile=@Mobile) AND UserId != @UserId";
                SqlParameter[] param = 
                {
                    new SqlParameter("@Email", user.Email),
                    new SqlParameter("@Mobile", user.Mobile),
                    new SqlParameter("@UserId", user.UserId)
                };
                DataTable dt = db.GetData(checkQuery, param);

                bool emailExists = false;
                bool mobileExists = false;

                foreach (DataRow row in dt.Rows)
                {
                    if (row["Email"].ToString() == user.Email)
                        emailExists = true;

                    if (row["Mobile"].ToString() == user.Mobile)
                        mobileExists = true;
                }

                if (emailExists && mobileExists)
                {
                    ViewBag.msg = "Email Id and Mobile already exist";
                    return View(user);
                }
                else if (emailExists)
                {
                    ViewBag.msg = "Email Id already exist";
                    return View(user);
                }
                else if (mobileExists)
                {
                    ViewBag.msg = "Mobile already exist";
                    return View(user);
                }
                int userId = db.GetCounter("Add_User");

                bool isNewUser = (user.UserId == 0);

                SqlParameter[] paramInsert =
               {
                  new SqlParameter("@UserId", user.UserId),
                  new SqlParameter("@Name", user.Name ?? ""),
                  new SqlParameter("@Mobile", user.Mobile ?? ""),
                  new SqlParameter("@City", user.City ?? ""),
                  new SqlParameter("@Password", user.Password ?? ""),
                  new SqlParameter("@Status", user.Status),
                  new SqlParameter("@Email", user.Email),
                  new SqlParameter("@Role", user.Role ?? "User")
                };

                if (isNewUser)
                {
                    userId = db.GetCounter("Add_User");
                    paramInsert[0].Value = userId;

                    object newId = db.ExecuteScalarSP("sp_User_Insert", paramInsert);

                    if (newId != null)
                    {
                        string emailBody = GetEmail("Add_User");

                        emailBody = emailBody.Replace("#name#", user.Name)
                                             .Replace("#mobile#", user.Mobile)
                                             .Replace("#email#", user.Email);

                        SendUserEmail(user.Email, "Account Created Successfully", emailBody);

                        TempData["msg"] = "User Added Successfully";

                        return RedirectToAction("Add_User");
                      }
                    }
                else
                {
                    int result = db.ExecuteNonQuerySP("sp_User_Update", paramInsert);

                    if (result > 0)
                    {
                        TempData["msg"] = "User Updated Successfully";
                        return RedirectToAction("Display_User");
                    }
                    else
                    {
                        TempData["msg"] = "Update Failed";
                    }
                }
                }
              catch (Exception ex)
            {
                db.FailSave(ex.Message, 199, "Admin/Add_User POST");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View(user);
          }
        public void SendUserEmail(string email, string subject, string body)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("shiivamtripathi6@gmail.com"); 
                mail.To.Add(email);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient();
                smtp.Send(mail);
                System.Diagnostics.Debug.WriteLine("Email sent successfully to " + email);
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 221, "Admin/SendUserEmail");
                 string Message = "alert('Something went wrong! Please try again later')";
                System.Diagnostics.Debug.WriteLine(Message);
            }
        }
        public string GetEmail(string type)
        {
            string msg = "";
            try
            {
                SqlParameter[] param = 
            {
                new SqlParameter("@Type", type)
            };
                object obj = db.ExecuteScalar("select Message from EmailMaster where Type_name=@Type", param);
                if (obj != null)
                    msg = obj.ToString();
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 241, "Admin/GetEmail");
                String Message = "alert('Something went Error! Please try again later')";
            }
        return msg;
      }
        public ActionResult Display_User(string search)
        {
                DataTable dt = new DataTable();
            try
            {
                SqlCommand cmd = new SqlCommand();

                if (!string.IsNullOrEmpty(search))
                {
                    SqlParameter[] param = 
                   {
                     new SqlParameter("@search", search)
                   };
                   string query = @"SELECT * FROM Add_Users WHERE Name LIKE '%' + @search + '%' OR Mobile LIKE '%' + @search + '%' OR City LIKE '%' + @search + '%'";
                   dt = db.GetData(query, param);
                }
                else
                {
                    dt = db.GetDataSP("sp_Users_Display");
                }
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 269, "Admin/Display_User");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View(dt);
       }
        [HttpPost]
        public ActionResult DeleteUser(int id)
        {
            try
            {
                SqlParameter[] param = 
                {
                    new SqlParameter("@UserId", id)
                };
                int result = db.ExecuteNonQuerySP("sp_User_Delete", param);
               TempData["Success"] = "User soft-deleted Successfully!";
        
            }
            catch (Exception ex)
          {
              db.FailSave(ex.Message, 289, "Admin/DeleteUser POST");
              string Message = "alert('Something went wrong! Please try again later')";
          }

            return RedirectToAction("Display_User");
       }
       public ActionResult RestoreUser(int id)
        {
            try
            {
                SqlParameter[] param = 
                {
                    new SqlParameter("@UserId", id)
                };
                int result = db.ExecuteNonQuerySP("sp_User_Restore", param);
                if (result > 0)
                   TempData["Success"] = "User Restored Successfully!";
                else
                    TempData["Success"] = "Restored Failed";
                }  
               catch(Exception ex)
            {
                db.FailSave(ex.Message, 311, "Admin/RestoreUser");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return RedirectToAction("Deleted_Users");
           }
        public ActionResult Deleted_Users()
       {
         DataTable dt = new DataTable();
         try
         {
            dt = db.GetDataSP("sp_Deleted_Users_Display");
         }
        catch(Exception ex)
         {
             db.FailSave(ex.Message, 325, "Admin/Deleted_Users");
             string Message = "alert('Something went wrong! Please try again later.')";
         }
         return View(dt);
     }
        [HttpGet]
        public ActionResult AddBook(int? id)
        {
            BookMaster bm = new BookMaster();

            try
            {
                if (id.HasValue)
                {
                    SqlParameter[] param =
                {
                    new SqlParameter("@BookId", id)
                };

                    DataTable dt = db.GetData("SELECT * FROM BookMaster WHERE BookId=@BookId", param);
                    if (dt.Rows.Count > 0)
                    {
                        bm.BookId = Convert.ToInt32(dt.Rows[0]["BookId"]);
                        bm.BookName = dt.Rows[0]["BookName"].ToString();
                        bm.Quantity = Convert.ToInt32(dt.Rows[0]["Quantity"]);
                    }
                }
                else
                {
                    bm = new BookMaster();
                    bm.BookId = 0;
                    bm.BookName = "";
                    bm.Quantity = 0;
                }

                bm.Rows = db.GetDataSP("sp_Book_Select_Active");
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 364, "Admin/AddBook GET");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View(bm);
        }
        [HttpPost]
        public ActionResult AddBook(BookMaster bm)
        {
            try
            {
                bool isNewBook = (bm.BookId == 0);

                if (isNewBook)
                {
                    bm.BookId = db.GetCounter("BookMaster");
                }

                SqlParameter[] param =
               {
                   new SqlParameter("@BookId", bm.BookId),
                   new SqlParameter("@BookName", bm.BookName),
                   new SqlParameter("@Quantity", bm.Quantity)
               };

                int result = db.ExecuteNonQuerySP("sp_Book_Insert", param);

                if (result >= 0)
                {
                    if (isNewBook)
                    {
                        TempData["msg"] = "Book Added Successfully";
                    }
                    else
                    {
                        TempData["msg"] = "Book Updated Successfully";
                    }
                }
                else
                {
                    TempData["msg"] = "Operation Failed";
                }
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 408, "Admin/AddBook POST");
                string Message = "alert('Something went wrong! Please try again later.')";
            }

            return RedirectToAction("AddBook", new { id = (int?)null });
        }
        public ActionResult BookingList()
        {
            DataTable dt = new DataTable();
            try
            {
              dt = db.GetDataSP("sp_Admin_Booking_Select");
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 423, "Admin/BookingList");
                string Message = "alert('Something went wrong! Please try gain later.')";
            }
            ViewBag.BookingList = dt;
            return View(dt);
        }
        [HttpPost]
        public ActionResult DeleteBooks(int id)
        {
            try
            {
                SqlParameter[] param = 
                {
                    new SqlParameter("@BookId", id)
                };
                int result = db.ExecuteNonQuerySP("sp_Book_Delete", param);
                TempData["Success"] = "Books soft-deleted Successfully!";
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 443, "Admin/DeleteBooks");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return RedirectToAction("AddBook");
        }
        public ActionResult ToggleStatus(int id)
        {
            try
            {
                SqlParameter[] param = 
                {
                    new SqlParameter("@UserId", id)
                };
                int result = db.ExecuteNonQuerySP("sp_User_ToggleStatus", param);
                TempData["Success"] = "User status updated successfully!";
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 461, "Admin/ToggleStatus");
                string Message = "alert('Something went wrong! Please try again later')";
            }

            return RedirectToAction("Display_User");
        }

        public ActionResult ReturnedBooks()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = db.GetDataSP("sp_ReturnedBooks_Display");
            }
           catch(Exception ex)
            {
                db.FailSave(ex.Message, 477, "Admin/ReturnedBooks");
                string Message = "alert('Something went wrong! Please try again later')";
            }
            return View(dt);
        }

        public ActionResult ToggleBookStatus(int id)
        {
            try
            {
                SqlParameter[] param = 
            {
                new SqlParameter("@BookId", id)
            };
                int result = db.ExecuteNonQuery("UPDATE BookMaster SET Status = CASE WHEN Status = 1 THEN 0 ELSE 1 END WHERE BookId=@BookId", param);
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 495, "Admin/ToggleBookStatus");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return RedirectToAction("AddBook");
        }
        public ActionResult Impersonate(int id)
        {
            try
            {
                if (Session["UserId"] == null || Session["Role"] == null)
                {
                    return RedirectToAction("Login", "User");
                }

                if (Session["IsImpersonating"] == null)
                {
                    Session["AdminUserId"] = Session["UserId"];
                    Session["AdminName"] = Session["Name"];
                    Session["AdminRole"] = Session["Role"];
                    Session["IsImpersonating"] = true;
                }

                SqlParameter[] param = 
            {
                new SqlParameter("@UserId", id)
            };
                DataTable dt = db.GetDataSP("sp_GetUserById", param);

                if (dt.Rows.Count > 0)
                {
                    Session["UserId"] = dt.Rows[0]["UserId"].ToString();
                    Session["Name"] = dt.Rows[0]["Name"].ToString();
                    Session["Role"] = dt.Rows[0]["Role"].ToString();

                    return RedirectToAction("Index", "User");
                }
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 534, "Admin/Impersonate");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return RedirectToAction("Display_User", "Admin");
        }
        public ActionResult BackToAdmin()
        {
         try
         {
           if
            (Session["IsImpersonating"] != null &&
            Session["AdminUserId"] != null &&
            Session["AdminRole"] != null)
          {
            Session["UserId"] = Session["AdminUserId"].ToString();
            Session["Name"] = Session["AdminName"] != null ? Session["AdminName"].ToString() : "";
            Session["Role"] = Session["AdminRole"].ToString();

            Session.Remove("AdminUserId");
            Session.Remove("AdminName");
            Session.Remove("AdminRole");
            Session.Remove("IsImpersonating");

            return RedirectToAction("Dashboard", "Admin");
          }
        }
            catch(Exception ex)
             {
                 db.FailSave(ex.Message, 562, "Admin/BackToAdmin");
                 string Message = "alert('Something went wrong! Please try again later.')";
             }
            return RedirectToAction("Login", "User");
        }
        public ActionResult MailLogList()
        {
          List<MailLog> list = new List<MailLog>();
          try
          { 
          DataTable dt = db.GetDataSP("sp_MailLog_SelectAll");
          foreach (DataRow dr in dt.Rows)
          {
            MailLog obj = new MailLog
            {
            MailId = Convert.ToInt32(dr["MailId"]),
            To = dr["To"].ToString(),
            UserId = Convert.ToInt32(dr["UserId"]),
            Subject = dr["Subject"].ToString(),
            Body = dr["Body"].ToString(),
            DateTime = Convert.ToDateTime(dr["DateTime"])
          };
            list.Add(obj);
          }
         }
          catch(Exception ex)
            {
                db.FailSave(ex.Message, 589, "Admin/MailLogList");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
             return View(list);
         }     
       public ActionResult UserProfile(int id)
        {
            DataTable dt = new DataTable();
            try
            {
                string query = "select * from Add_Users where UserId=@id";
                SqlParameter[] param =
            {
               new SqlParameter("@id", id)
            };

               dt = db.GetData(query, param);
               if (dt.Rows.Count == 0)
               {
                   ViewBag.Message = "User not found!";
               }
           }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 613, "Admin/UserProfile");
                string Message = "alert('Something went wrong!Please try again later.')";
            }
            return View(dt);
        }
     
        [HttpPost]
      public ActionResult Update(List<EmailMaster> model)
        {
            try
            {
                int successCount = 0;
                foreach (var item in model)
                {
                    SqlParameter[] param = 
                    {
                        new SqlParameter("@msg", item.Message),
                        new SqlParameter("@Id", item.Id)
                    };
                    int result = db.ExecuteNonQuery("update EmailMaster set Message=@msg where Id=@Id", param);
                 if (result > 0)
                successCount++;
               }

             if (successCount > 0)
                  TempData["msg"] = "Email Master Updated Successfully";
              else
                    TempData["msg"] = "No records updated!";
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 644, "Admin/Update POST");
                string Message = "alert('Something went wrong! Please try again later.')";
          
            }
          return RedirectToAction("EmailMaster");
        }
        [HttpGet]
        public ActionResult AddEmailTemplate(int? id)
        {
            List<EmailMaster> list = new List<EmailMaster>();
            try
            {
                DataTable dt = db.GetData("select * from EmailMaster where Status = 1", null);
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new EmailMaster
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        Type = dr["Type_name"].ToString(),
                        Message = dr["Message"].ToString()
                    });
                }

                if (id != null)
                {
                    SqlParameter[] param1 = 
                {
                    new SqlParameter("@Id", id)
                };
                    DataTable dt2 = db.GetData("select * from EmailMaster where Id=@Id", param1);

                    if (dt2.Rows.Count > 0)
                    {
                        ViewBag.Id = dt2.Rows[0]["Id"];
                        ViewBag.Type = dt2.Rows[0]["Type_name"].ToString();
                        ViewBag.Message = dt2.Rows[0]["Message"].ToString();
                    }
                }
                else
                {
                    ViewBag.Id = null;
                    ViewBag.Type = "";
                    ViewBag.Message = "";
                }
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 691, "Admin/AddEmailTemplate GET");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View(list);
        }       
        [HttpPost]
        public ActionResult AddEmailTemplate(EmailMaster em)
        {
            try
            {
                if (em.Id == 0)
                {
                    int id = db.GetCounter("EmailMaster");
                    string query = "INSERT INTO EmailMaster(Type_name, Message, Status) VALUES(@Type, @Message, 1)";
                    SqlParameter[] param = 
                {
                    new SqlParameter("@Id", id),
                    new SqlParameter("@Type", em.Type),
                    new SqlParameter("@Message", em.Message)
                };
                    db.ExecuteNonQuery(query, param);
                    TempData["msg"] = "Template Added Successfully";
                }
                else
                {
                    SqlParameter[] param1 = 
                {
                    new SqlParameter("@Id", em.Id),
                    new SqlParameter("@Type", em.Type),
                    new SqlParameter("@Message", em.Message)
                };
                    int result = db.ExecuteNonQuery("update EmailMaster set Type_name=@Type,Message=@Message where Id=@Id", param1);
                    if (result > 0)
                        TempData["msg"] = "Template Updated Successfully";
                    else
                        TempData["msg"] = "Update Failed!";
                }
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 731, "Admin/AddEmailTemplate");
                string Message = "alert('Something went wrong! Please try again later')";
            }
            return RedirectToAction("AddEmailTemplate", new { id = (int?)null });
        }
        [HttpPost]
        public ActionResult DeleteEmailTemplate(int id)
        {
            try
            {
                SqlParameter[] param = 
                {
                    new SqlParameter("@Id", id)
                };
                int result = db.ExecuteNonQuery("update EmailMaster set Status = 10 where Id = @Id", param);
                if (result > 0)
                   TempData["Success"] = "Email Template soft-deleted Successfully!";
               else
                   TempData["Success"] = "Record not found!";
                }
               catch (Exception ex)
              {
                db.FailSave(ex.Message, 753, "Admin/DeleteEmailTemplate POST");
                string Message = "alert('Something went wrong! Please try again later.')";
              }
            return RedirectToAction("AddEmailTemplate");
        }

    }
}
