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
using System.Diagnostics;
using System.IO;

namespace CompanyProject.Controllers
{
    public class UserController : Controller
    {
        DbManager db = new DbManager();
        
        //
        // GET: /User/

        public ActionResult Index()
        {
            try
            {
                if (Session["UserId"] == null)
                return RedirectToAction("Login");
                int UserId = Convert.ToInt32(Session["UserId"]);
                DataTable dt = db.GetDataSP("sp_Book_Select");
                ViewBag.BookList = dt;

                SqlParameter[] param1 =
                {
                   new SqlParameter("@UserId", UserId)
                };
                DataTable dt2 = db.GetDataSP("sp_User_BookedBooks", param1);
                ViewBag.BookedBooks = dt2;

                SqlParameter[] param2 = 
                {
                  new SqlParameter("@UserId", UserId)
                };
                DataTable dt3 = db.GetData("Select * from Add_Users where UserId=@UserId", param2);
                ViewBag.Profile = dt3;
              }
              catch(Exception ex)
            {
                db.FailSave(ex.Message, 50, "User/Index");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View();
        }
        public ActionResult ViewCard()
        {
            try
            {
                if (Session["UserId"] == null)
                    return RedirectToAction("Login", "User");

                int userId = Convert.ToInt32(Session["UserId"]);
                SqlParameter[] param = 
               {
                 new SqlParameter("@UserId", userId)
               };
                DataTable dt = db.GetData("select * from Add_Users where UserId=@UserId", param);
                if (dt.Rows.Count > 0)
                {
                    ViewBag.Profile = dt;
                }
                else
                {
                    ViewBag.Error = "User data not found!";
                }
              }
              catch(Exception ex)
              {
                db.FailSave(ex.Message, 79, "admin/ViewCard");
                string Message = "alert('Something went wrong! Please try again later.')";
              }
            return View();
        }
        [HttpGet]
        public ActionResult UploadPhoto()
        {
            return View();
        }
        [HttpPost]
        public ActionResult UploadPhoto(HttpPostedFileBase photo)
        {
            try
            {
                if (Session["UserId"] == null)
                    return RedirectToAction("Login", "User");

                if (photo != null && photo.ContentLength > 0)
                {
                    int userId = Convert.ToInt32(Session["UserId"]);

                    SqlParameter[] param1 =
                 {
                    new SqlParameter("@UserId", userId)
                 };

                    DataTable dt = db.GetData("SELECT Name FROM Add_Users WHERE UserId=@UserId", param1);

                    if (dt.Rows.Count == 0)
                    {
                        TempData["msg"] = "User not found!";
                        return RedirectToAction("Index");
                    }

                    string userName = dt.Rows[0]["Name"].ToString().Replace(" ", "_");
                    string extension = Path.GetExtension(photo.FileName).ToLower();

                    string fileName = userName + extension; 

                    string path = Server.MapPath("~/Content/Upload/" + fileName);

                    photo.SaveAs(path);

                    SqlParameter[] param2 =
                   {
                     new SqlParameter("@Photo", fileName),
                     new SqlParameter("@UserId", userId)
                };

                    int result = db.ExecuteNonQuery("UPDATE Add_Users SET Photo=@Photo WHERE UserId=@UserId", param2 );

                    if (result > 0)
                        TempData["msg"] = "Photo Uploaded Successfully";
                    else
                        TempData["msg"] = "Database update failed!";
                }
                else
                {
                    TempData["msg"] = "Please select a file!";
                }
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 143, "User/UploadPhoto POST");
                string Message = "alert('Something went wrong! Please try again later.)";
            }
            return RedirectToAction("Index");
        }
        [HttpGet]
        public ActionResult Login()
        {
            try
            {
                if (Session["IsImpersonating"] != null)
                {
                    return View();
                }
                if (Request.Cookies["RememberMe"] != null &&
                    Request.Cookies["RememberMe"]["Token"] != null &&
                    !string.IsNullOrEmpty(Request.Cookies["RememberMe"]["Token"]))
                {
                    string token = Request.Cookies["RememberMe"]["Token"];

                    SqlParameter[] param = 
                {
                    new SqlParameter("@Token", SqlDbType.VarChar) { Value = token }
                };

                    DataTable dt = db.GetData(@"SELECT u.UserId,u.Name,u.Role FROM Add_Users u INNER JOIN RememberMeToken r on u.UserId=r.UserId WHERE r.Token=@Token AND r.ExpiryDate>GETDATE()", param);

                    if (dt.Rows.Count > 0)
                    {
                        Session["UserId"] = dt.Rows[0]["UserId"].ToString();
                        Session["Name"] = dt.Rows[0]["Name"].ToString();
                        Session["Role"] = dt.Rows[0]["Role"].ToString();

                        if (Session["Role"].ToString() == "Admin")
                            return RedirectToAction("Dashboard", "Admin");
                        else
                            return RedirectToAction("Index", "User");
                    }
                }
                ViewBag.cph = db.CaptchaCode();
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 186, "User/Login GET");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View();
        }
        [HttpPost]
        public ActionResult Login(Login lg, string txtcaptcha, string txtcaptchacode)
        {
            try
            {
                ViewBag.cph = db.CaptchaCode();
                if (txtcaptcha == txtcaptchacode)
                {
                    if (ModelState.IsValid)
                    {
                        SqlParameter[] param = 
                    {
                        new SqlParameter("@UserId", lg.UserId)
                    };
                        DataTable dt = db.GetDataSP("sp_User_Login", param);
                        if (dt.Rows.Count > 0)
                        {
                            string password = dt.Rows[0]["Password"].ToString();
                            if (password.Equals(lg.Password))
                            {
                                Session["UserId"] = dt.Rows[0]["UserId"].ToString();
                                Session["Name"] = dt.Rows[0]["Name"].ToString();
                                Session["Role"] = dt.Rows[0]["Role"].ToString();

                                if (lg.RememberMe)
                                {
                                    string token = Guid.NewGuid().ToString();

                                    SqlParameter[] param1 = 
                                   {
                                     new SqlParameter("@UserId", Session["UserId"]),
                                     new SqlParameter("@Token", token),
                                     new SqlParameter("@Expiry", DateTime.Now.AddDays(30))
                                   };

                                    int result = db.ExecuteNonQuery("INSERT INTO RememberMeToken(UserId,Token,ExpiryDate) VALUES(@UserId,@Token,@Expiry)", param1);

                                    HttpCookie cookie = new HttpCookie("Login");
                                    cookie["UserId"] = dt.Rows[0]["UserId"].ToString();
                                    cookie.Expires = DateTime.Now.AddDays(30);

                                    Response.Cookies.Add(cookie);

                                    HttpCookie cookie1 = new HttpCookie("RememberMe");
                                    cookie1["Token"] = token;
                                    cookie1.Expires = DateTime.Now.AddDays(30);

                                    Response.Cookies.Add(cookie1);
                                  }

                                    Session.Remove("IsImpersonating");
                                    Session.Remove("AdminUserId");
                                    Session.Remove("AdminName");
                                    Session.Remove("AdminRole");

                                    string role = dt.Rows[0]["Role"].ToString().ToLower();
                                 
                                    if (role == "user")
                                   {
                                     Session["UserId"] = lg.UserId;
                                     return RedirectToAction("Index", "User");
                                   }
                                   else if (role == "admin")
                                  {
                                    Session["UserId"] = lg.UserId;
                                    return RedirectToAction("Dashboard", "Admin");
                                  }
                                  else
                                  {
                                    ViewBag.msg = "Invalid Role";
                                  }

                                }
                               else
                              {
                                ViewBag.msg = "Invalid Password";
                              }
                            }
                           else
                          {
                             ViewBag.msg = "Invalid UserId";
                          }
                        }
                      }
                      else
                      {
                        ViewBag.msg = "Captcha code not match";
                      }
                    }
                    catch(Exception ex)
                    {
                       db.FailSave(ex.Message, 282, "User/Login POST");
                       string Message = "alert('Something went wrong! Please try again later.')";
                    }
                   return View(lg);
               }
             public JsonResult RefreshCaptcha()
            {
              string msg = db.CaptchaCode();
              //Session["Captcha"] = msg;
              return Json(msg, JsonRequestBehavior.AllowGet);
            }

      //  [HttpGet]
      //  public ActionResult BookNow(int bookId)
      //{
      //    Booking model = new Booking();
      //    try
      //    {
      //        if (Session["UserId"] == null)
      //            return RedirectToAction("Login");

      //        int userId = Convert.ToInt32(Session["UserId"]);
      //        SqlParameter[] param = 
      //      {
      //          new SqlParameter("@BookId", bookId)
      //      };
      //        string bookName = Convert.ToString(db.ExecuteScalar("SELECT BookName FROM BookMaster WHERE BookId=@BookId", param));
      //        SqlParameter[] param1 = 
      //      {
      //          new SqlParameter("@UserId", userId)
      //      };
      //        string userName = Convert.ToString(db.ExecuteScalar("SELECT Name FROM Add_Users WHERE UserId=@UserId", param1));
             
      //            model.UserId = userId;
      //            model.BookId = bookId;
      //            model.UserName = userName;
      //        ViewBag.BookName = bookName;
      //    }
      //      catch(Exception ex)
      //    {
      //        db.FailSave(ex.Message, 324, "User/BookNow GET");
      //        string Message = "alert('Something went wrong! Please try again later.')";
      //    }
      //     return View(model);
      //}
      //  [HttpPost]
      //  public ActionResult BookNow(Booking model)
      //  {
      //      try
      //      {
      //          if (Session["UserId"] == null)
      //              return RedirectToAction("Login", "User");
      //          int UserId = Convert.ToInt32(Session["UserId"]);
      //          SqlParameter[] param = 
      //          {
      //              new SqlParameter("@UserId", UserId),
      //              new SqlParameter("@BookId", model.BookId),
      //              new SqlParameter("@DateOfBooking", DateTime.Now),
      //              new SqlParameter("@Status", true)
      //          };
      //          int result = Convert.ToInt32(db.ExecuteScalarSP("sp_Booking_Insert", param));
               
      //          if (result == 1)
      //          {
      //              TempData["msg"] = "This book is already issued. Return it before issuing again.";
      //          }
      //          else
      //          {
      //              TempData["msg"] = "Book issued successfully!";
      //          }
      //      }
      //      catch (Exception ex)
      //      {
      //          db.FailSave(ex.Message, 357, "User/BookNow");
      //          string Message = "alert('Something went wrong! Please try again later.')";
      //      }
      //      return RedirectToAction("Index", "User");
      //  }

        public ActionResult ReturnBook(int bookingId)
        {
            try
            {
                if (Session["UserId"] == null)
                    return RedirectToAction("Login");
                int UserId = Convert.ToInt32(Session["UserId"]);
                SqlParameter[] param = 
               {
                  new SqlParameter("@BookingId", bookingId)
               };

                int result = db.ExecuteNonQuerySP("sp_Book_Return", param);
                if (result > 0)
                    TempData["msg"] = "Book Returned Successfully";
                else
                    TempData["msg"] = "Something went wrong!";
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 381, "User/ReturnBook");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return RedirectToAction("Index");
        }
       [HttpPost]
        public ActionResult UpdateProfile(string Name, string Mobile, string City, string Password)
        {
            try
            {
                if (Session["UserId"] == null)
                    return RedirectToAction("Login");

                int UserId = Convert.ToInt32(Session["UserId"]);
                SqlParameter[] param = 
                {
                    new SqlParameter("@Name", Name),
                    new SqlParameter("@Mobile", Mobile),
                    new SqlParameter("@City", City),
                    new SqlParameter("@Password", Password),
                    new SqlParameter("@UserId", UserId)
                };
                int result = db.ExecuteNonQuery("UPDATE Add_Users SET Name=@Name, Mobile=@Mobile, City=@City, Password=@Password WHERE UserId=@UserId", param);
               
                TempData["msg"] = "Profile updated successfully!";
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 409, "User/UpdateProfile POST");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return RedirectToAction("Index");
        }
        public ActionResult UserBookingList()
       {
           try
           {
               if (Session["UserId"] == null)
                   return RedirectToAction("Login", "User");

               int UserId = Convert.ToInt32(Session["UserId"]);

               SqlParameter[] param = 
            {
                new SqlParameter("@UserId", UserId)
            };
               DataTable dt = db.GetDataSP("sp_User_BookingList", param);
               ViewBag.BookingList = dt;
           }
            catch(Exception ex)
           {
               db.FailSave(ex.Message, 432, "User/UserBookingList");
               string Message = "alert('Something went wrong! Please try again later')";
           }
           return View();
       }
       public ActionResult Logout()
        {
            try
            {
                if (Session["IsImpersonating"] != null)
                {
                    Session["UserId"] = Session["AdminUserId"];
                    Session["Name"] = Session["AdminName"];
                    Session["Role"] = Session["AdminRole"];

                    Session.Remove("AdminUserId");
                    Session.Remove("AdminName");
                    Session.Remove("AdminRole");
                    Session.Remove("IsImpersonating");

                    return RedirectToAction("Dashboard", "Admin");
                }

                if (Request.Cookies["RememberMe"] != null)
                {
                    string token = Request.Cookies["RememberMe"]["Token"];

                    SqlParameter[] param = 
                {
                    new SqlParameter("@Token", token)
                };
                    int result = db.ExecuteNonQuery("DELETE FROM RememberMeToken WHERE Token=@Token", param);
                    HttpCookie cookie = new HttpCookie("RememberMe");
                    cookie.Expires = DateTime.Now.AddDays(-1);
                    Response.Cookies.Add(cookie);
                }

                Session.Clear();
                Session.Abandon();
            }
           catch(Exception ex)
            {
                db.FailSave(ex.Message, 474, "User/Logout");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return RedirectToAction("Login", "User");
        }
        [HttpGet]
       public ActionResult ForgotPassword()
       {
           return View();
       }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPassword model)
        {
            ViewBag.DebugResult = "POST method called";

            if (!ModelState.IsValid)
            {
                ViewBag.DebugResult += " | ModelState invalid";
                return View(model);
            }

            int userId;
            if (!int.TryParse(model.UserId, out userId))
            {
                ViewBag.msg = "Invalid UserId format";
                ViewBag.DebugResult += " | UserId conversion failed";
                return View(model);
            }

            ViewBag.DebugResult += " | UserId received: " + userId;
            string email = "";
            SqlParameter[] param = 
            {
                new SqlParameter("@UserId", userId)
            };
           object result = Convert.ToString(db.ExecuteScalar("SELECT Email FROM Add_Users WHERE UserId=@UserId", param));
           
           if (result != null)
               email = result.ToString();
            if (result != null)
                email = result.ToString();

            ViewBag.DebugEmail = email;

            if (string.IsNullOrEmpty(email))
            {
                ViewBag.msg = "UserId not found or email missing";
                ViewBag.DebugResult += " | Email not found";
                return View(model);
            }
            string token = Guid.NewGuid().ToString();

            ViewBag.DebugResult += " | Token generated";
            int ForgetId = db.GetCounter("ForgotPassword");
            SqlParameter[] param1 = 
            {
                new SqlParameter("@ForgetId", ForgetId),
                new SqlParameter("@UserId", userId),
                new SqlParameter("@TokenId", token)
            };
            int result1 = db.ExecuteNonQuerySP("sp_ForgotPassword_Insert", param1);
            ViewBag.DebugResult += " | Token saved in DB";

            string resetLink = Url.Action("ResetPassword", "User", new { token = token }, Request.Url.Scheme);

            SqlParameter[] param2 = 
            {
                new SqlParameter("@UserId", userId)
            };
            
           object result2 = db.ExecuteScalar("SELECT Name FROM Add_Users WHERE UserId=@UserId", param2);
           string name = "";
           if (result2 != null)
               name = result2.ToString();
            string msg = GetEmail("ResetPassword");

            msg = msg.Replace("#name#", name);
            msg = msg.Replace("#link#", resetLink);

            SendOTPEmail(email, "Reset Password", msg);

            ViewBag.ResetLink = resetLink;

            ViewBag.DebugResult += " | Reset link generated";

            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("shiivamtripathi6@gmail.com");
                mail.To.Add(email);
                mail.Subject = "Reset Password";
                mail.Body = "Click below link to reset password:\n\n" + resetLink;
                mail.IsBodyHtml = false;

                SmtpClient smtp = new SmtpClient();
                smtp.Send(mail);
                ViewBag.msg = "Reset link sent to your email";

                SqlParameter[] param3 = 
                {
                    new SqlParameter("@To", email),
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@Subject", "Reset Password"),
                    new SqlParameter("@Body", resetLink)
                };
               int result3 = db.ExecuteNonQuerySP("sp_MailLog_Insert", param3);

                ViewBag.DebugResult += " | MailLog inserted";
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 586, "User/ForgotPassword");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View(model);
        }
        [HttpGet]
        public ActionResult ResetPassword(string token)
        {
            ResetPassword model = new ResetPassword();
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    ViewBag.msg = "Invalid link";
                    return View();
                }
               
                model.Token = token;
                SqlParameter[] param = 
            {
                new SqlParameter("@TokenId", token)
            };

                object obj = db.ExecuteScalarSP("sp_ForgotPassword_Validate", param);
                if (obj == null)
                {
                    ViewBag.msg = "Invalid link";
                    return View();
                }

                string status = obj.ToString();

                if (status == "INVALID")
                {
                    ViewBag.msg = "Invalid link";
                    return View();
                }
                else if (status == "USED")
                {
                    ViewBag.msg = "This link is already used";
                    return View();
                }
                else if (status == "EXPIRED")
                {
                    ViewBag.msg = "Link is expired";
                    return View();
                }
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 636, "User/ResetPassword GET");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View(model);
        }
       [HttpPost]
        [ValidateAntiForgeryToken]
       public ActionResult ResetPassword(ResetPassword model)
       {
           try
           {
               if (!ModelState.IsValid)
               {
                   ViewBag.msg = "Invalid data";
                   return View(model);
               }
               SqlParameter[] param =
             {
              new SqlParameter("@TokenId", model.Token),
              new SqlParameter("@Password", model.NewPassword)
             };

               DataTable dt = db.ExecuteReaderSP("sp_ForgotPassword_Update", param);

               string result = "";

               if (dt.Rows.Count > 0)
               {
                   result = dt.Rows[0]["Result"].ToString();
               }
            
               if (result == "USED")
               {
                   ViewBag.msg = "This link is already used";
                   return View();
               }
               else if (result == "EXPIRED")
               {
                   ViewBag.msg = "Link is expired";
                   return View();
               }
               else if (result == "INVALID")
               {
                   ViewBag.msg = "Invalid link";
                   return View();
               }
               else if (result == "SUCCESS")
               {
                   SqlParameter[] paramUser =
                  {
                    new SqlParameter("@TokenId", model.Token)
                   };

                   DataTable dtUser = db.GetData("SELECT Name,Email FROM Add_Users u INNER JOIN ForgotPassword f ON u.UserId=f.UserId WHERE f.TokenId=@TokenId", paramUser);

                   string name = "";
                   string email = "";

                   if (dtUser.Rows.Count > 0)
                   {
                       name = dtUser.Rows[0]["Name"].ToString();
                       email = dtUser.Rows[0]["Email"].ToString();
                   }
              
                   string msg = GetEmail("PasswordChanged");

                   msg = msg.Replace("#name#", name);

                   SendOTPEmail(email, "Password Changed", msg);

                   ViewBag.msg = "Password reset successfully";

                   ResetPassword newModel = new ResetPassword();
                   return View(newModel);
               }
               ViewBag.msg = "Unknown error";
           }
           catch (Exception ex)
           {
               db.FailSave(ex.Message, 715, "User/ResetPassword POST");
               string Message = "alert('Something went wrong! Please try again later.')";
           }
           return View();
       }
        [HttpGet]
        public ActionResult Login1(string mode)
       {
           try
           {
               Session["OTP"] = null;
               Session["OTPUserId"] = null;
               if (mode == "register")
               {
                   ViewBag.Mode = "register";
               }
               else
               {
                   ViewBag.Mode = "login1";
               }

               if (Session["OTPUserId"] == null)
               {
                   ViewBag.ActivePanel = "p1";
                   ViewBag.OTPSection = false;
               }
               else
               {
                   ViewBag.ActivePanel = "verify";
                   ViewBag.OTPSection = true;
               }
               ViewBag.ShowRegisterButton = false;
           }
           catch(Exception ex)
           {
               db.FailSave(ex.Message, 750, "User/Login1 GET");
               string Message = "alert('Something went wrong! Please try again later.')";
           }
            return View();
       }
        [HttpPost]
        public ActionResult Login1(string Input, string mode)
        {
            try
            {
                ViewBag.Mode = mode;

                string OTP = new Random().Next(100000, 999999).ToString();

                Session["OTP"] = OTP;
                Session["LoginInput"] = Input;

                object idObj = db.ExecuteScalar("SELECT UserId FROM Add_Users WHERE CAST(UserId AS VARCHAR(20))=@Input OR Email=@Input OR Mobile=@Input",
                new SqlParameter[]
               {
                  new SqlParameter("@Input", Input)
               });

                string userName = Input;

                object obj = db.ExecuteScalar("SELECT Name FROM Add_Users WHERE CAST(UserId AS VARCHAR(20))=@Input OR Email=@Input OR Mobile=@Input",
                 new SqlParameter[]
                {
                   new SqlParameter("@Input", Input)
                });

                if (obj != null)
                    userName = obj.ToString();

                if (idObj != null)
                {
                    int userId = Convert.ToInt32(idObj);

                    int OTPId = db.GetCounter("UserOTP");

                    SqlParameter[] otpParam =
                  {
                     new SqlParameter("@OTPId", OTPId),
                     new SqlParameter("@UserId", userId),
                     new SqlParameter("@OTP", OTP)
                  };

                    db.ExecuteNonQuerySP("sp_Insert_UserOTP", otpParam);

                    Session["OTPUserId"] = userId;
                }
                else
                {
                    Session["OTPUserId"] = null;
                }

                string msg = GetEmail("OTP");

                msg = msg.Replace("#name#", userName)
                         .Replace("#otp#", OTP);

                if (Input.Contains("@"))
                    SendOTPEmail(Input, "OTP Verification", msg);
                else
                    SendOTPSMS(Input, OTP);

                ViewBag.Success = "OTP sent successfully";
                ViewBag.GeneratedOTP = OTP;

                ViewBag.OTPSection = true;
                ViewBag.ActivePanel = "verify";
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 824, "User/Login1 POST");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View("Login1");
        }
        public void SendOTPSMS(string mobile, string otp)
        {
            System.Diagnostics.Debug.WriteLine("OTP is: " + otp + " Mobile: " + mobile);
        }
        public void SendOTPEmail(string email, string subject, string body)
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
            }
            catch(Exception ex)
            {
             db.FailSave(ex.Message, 853,"User/SendOTPEmail");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
           }
        [HttpPost]
        public ActionResult VerifyOTP(string OTP)
        {
            try
            {
                if (Session["OTP"] == null || Session["LoginInput"] == null)
                {
                    return RedirectToAction("Login1");
                }

                string sessionOTP = Session["OTP"].ToString();
                string input = Session["LoginInput"].ToString();

                if (OTP == sessionOTP)
                {
                 object userObj = db.ExecuteScalar("SELECT UserId FROM Add_Users WHERE Email=@Input OR Mobile=@Input OR CAST(UserId AS VARCHAR(20))=@Input",
                 new SqlParameter[]
                 {
                    new SqlParameter("@Input", input)
                  });

                    if (userObj == null)
                    {
                        TempData["msg"] = "User not registered. Please register first.";
                        Session["OTP"] = null;
                        Session["OTPUserId"] = null;
                        TempData["ShowRegisterPanel"] = true;
                        TempData["Input"] = input;
                        return RedirectToAction("Register");
                    }

                    int userId = Convert.ToInt32(userObj);

                    SqlParameter[] otpParam =
                   {
                      new SqlParameter("@UserId", userId),
                      new SqlParameter("@OTP", OTP)
                   };

                    DataTable otpDt = db.GetData(@"SELECT * FROM UserOTP WHERE UserId = @UserId AND OTP = @OTP AND Status = 0 AND GETDATE() <= DATEADD(MINUTE, 5, [Date])", otpParam);

                    if (otpDt.Rows.Count == 0)
                    {
                        ViewBag.Error = "OTP expired or invalid";
                        ViewBag.OTPSection = true;
                        return View("Login1");
                    }

                    SqlParameter[] updateParam =
                   {
                       new SqlParameter("@UserId", userId),
                       new SqlParameter("@OTP", OTP)
                   };

                    db.ExecuteNonQuery("UPDATE UserOTP SET Status = 1 WHERE UserId=@UserId AND OTP=@OTP AND Status=0", updateParam);

                    SqlParameter[] OTPparam = 
                    {
                        new SqlParameter("@Input", input)
                    };
                    DataTable dt = db.ExecuteReaderSP("sp_GetUser_OTPLogin", OTPparam);

                    if (dt.Rows.Count > 0)
                    {
                        Session["UserId"] = dt.Rows[0]["UserId"].ToString();
                        Session["Name"] = dt.Rows[0]["Name"].ToString();
                        Session["Role"] = dt.Rows[0]["Role"].ToString();
                        if (Session["Role"].ToString().ToLower() == "admin")
                        {
                            return RedirectToAction("Dashboard", "Admin");
                        }
                        else
                        {
                            return RedirectToAction("Index", "User");
                        }
                    }
                    else
                    {
                        TempData["msg"] = "User not registered. Please register first.";
                        Session["OTP"] = null;
                        Session["OTPUserId"] = null;
                        TempData["ShowRegisterPanel"] = true;
                        TempData["Input"] = input;
                        return RedirectToAction("Register");
                    }
                }
                else
                {
                    ViewBag.Error = "Invalid OTP";
                    ViewBag.OTPSection = true;
                    return View("Login1");
                }
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 952, "User/VerifyOTP POST");
                string Message = "alert('Something went wrong! Please try again later.')";
                return RedirectToAction("Login");
            }
        }
        [HttpPost]
        public ActionResult ResendOTP()
        {
            try
            {
                if (Session["OTPUserId"] == null)
                {
                    return RedirectToAction("Login1");
                }

                int userId = Convert.ToInt32(Session["OTPUserId"]);

                string email = "";
                string mobile = "";
                SqlParameter[] sendparam = 
                {
                    new SqlParameter("@UserId", userId)
                };
                DataTable dt = db.GetData("SELECT Email, Mobile FROM Add_Users WHERE UserId=@UserId", sendparam);
                if (dt.Rows.Count > 0)
                {
                    email = dt.Rows[0]["Email"].ToString();
                    mobile = dt.Rows[0]["Mobile"].ToString();
                }

                string OTP = new Random().Next(100000, 999999).ToString();

                Session["OTP"] = OTP;

                ViewBag.GeneratedOTP = OTP;
                int OTPId = db.GetCounter("UserOTP");
                SqlParameter[] param1 = 
                {
                    new SqlParameter("@OTPId", OTPId),
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@OTP", OTP)
                };
                int result = db.ExecuteNonQuerySP("sp_Insert_UserOTP", param1);

                if (!string.IsNullOrEmpty(email))
                {
                    string msg = GetEmail("OTP");

                    msg = msg.Replace("#name#", email);
                    msg = msg.Replace("#otp#", OTP);

                    SendOTPEmail(email, "OTP Verification", msg);
                    ViewBag.Success = "OTP resent successfully to Email";
                }
                else
                {
                    SendOTPSMS(mobile, OTP);
                    ViewBag.Success = "OTP resent successfully to Mobile";
                }

                ViewBag.OTPSection = true;
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 1016, "User/ResendOTP POST");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View("Login1");
        }
        public ActionResult GoToVerifyOTP()
        {
            try
            {
                if (Session["OTPUserId"] != null)
                {
                    ViewBag.OTPSection = true;
                }
                else
                {
                    ViewBag.Error = "Please enter UserId / Email / Mobile first to verify OTP.";
                    ViewBag.OTPSection = false;
                }
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 1037, "User/GoToVerifyOTP");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View("Login1");
        }
        [HttpGet]
         public ActionResult Register()
        {
            try
            {
                ViewBag.ShowRegisterPanel = true;

                if (TempData["msg"] != null)
                    ViewBag.msg = TempData["msg"];

                if (TempData["Input"] != null)
                    ViewBag.Input = TempData["Input"];
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 1057, "User/Register GET");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View("Login1");
        }
        [HttpPost]
        public ActionResult Register(string Name, string Email, string Mobile, string Password)
        {
            try
            {
                SqlParameter[] Registerparam = 
                {
                    new SqlParameter("@Email", Email),
                    new SqlParameter("@Mobile", Mobile)
                };
                DataTable dt = db.GetData("SELECT Email, Mobile FROM Add_Users WHERE Email=@Email OR Mobile=@Mobile", Registerparam);
                bool emailExists = false;
                bool mobileExists = false;

                while (dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["Email"].ToString() == Email)
                        emailExists = true;

                    if (dt.Rows[0]["Mobile"].ToString() == Mobile)
                        mobileExists = true;
                }

                if (emailExists && mobileExists)
                {
                    TempData["msg"] = "Email and Mobile already registered.";
                    ViewBag.ShowRegisterPanel = true;
                    return View("Login1");
                }
                else if (emailExists)
                {
                    TempData["msg"] = "Email already registered.";
                    ViewBag.ShowRegisterPanel = true;
                    return View("Login1");
                }
                else if (mobileExists)
                {
                    TempData["msg"] = "Mobile number already registered.";
                    ViewBag.ShowRegisterPanel = true;
                    return View("Login1");
                }
                SqlParameter[] param1 = 
                {
                    new SqlParameter("@Name", Name),
                    new SqlParameter("@Email", Email),
                    new SqlParameter("@Mobile", Mobile),
                    new SqlParameter("@Password", Password)
                };
                 int result = Convert.ToInt32(db.ExecuteScalar("INSERT INTO Add_Users(Name,Email,Mobile,Password,Role,Status) VALUES(@Name,@Email,@Mobile,@Password,'User',1); select SCOPE_IDENTITY();", param1));

                string msg = GetEmail("Register");

                msg = msg.Replace("#name#", Name);
                msg = msg.Replace("#email#", Email);
                msg = msg.Replace("#mobile#", Mobile);

                SendOTPEmail(Email, "Registration Successfully", msg);

                Session.Remove("OTPUserId");
                Session.Remove("OTP");
                Session.Remove("LoginInput");

                TempData["msg"] = "Thank You ! Registration successfully completed.";
                TempData["RegId"] = result;
            }
            catch (Exception ex)
            {
                db.FailSave(ex.Message, 1129, "User/Register POST");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return RedirectToAction("Thankyou");
        }
        public ActionResult Thankyou()
        {
            try
            {
                Session["OTPUserId"] = null;
                Session["OTP"] = null;
                Session["LoginInput"] = null;
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 1144, "User/Thankyou");
                string Message = "alert('Something went wrong! Please try again later.')";
            }
            return View();
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
                object result = db.ExecuteScalar("select Message from EmailMaster where Type_name=@Type", param);

                if (result != null)
                {
                    msg = result.ToString();
                }
            }
            catch(Exception ex)
            {
                db.FailSave(ex.Message, 1167, "User/GetEmail");
                string Message = "alert('Something went wrong! Please try sgsin later.')";
            }
            return msg;
        }
        
        [HttpPost]
             public ActionResult LoginWithPassword(string UserId, string Password)
          {
              try
              {
                  SqlParameter[] param = 
            {
                new SqlParameter("@Input", UserId),
                new SqlParameter("@Password", Password)
            };
                  DataTable dt = db.GetData(@"SELECT UserId,Name,Role FROM Add_Users WHERE (CAST(UserId AS VARCHAR(20)) = @Input OR Email = @Input OR Mobile = @Input) AND Password COLLATE SQL_Latin1_General_CP1_CS_AS = @Password", param);

                  if (dt.Rows.Count > 0)
                  {
                      Session["UserId"] = dt.Rows[0]["UserId"].ToString();
                      Session["Name"] = dt.Rows[0]["Name"].ToString();
                      Session["Role"] = dt.Rows[0]["Role"].ToString();

                      if (Session["Role"].ToString() == "Admin")
                          return RedirectToAction("Dashboard", "Admin");
                      else
                          return RedirectToAction("Index", "User");
                  }
                  else
                  {
                      ViewBag.Error = "Invalid UserId / Email / Mobile or Password";
                      return View("Login1");
                  }
              }
              catch(Exception ex)
              {
                  db.FailSave(ex.Message, 1204, "User/LoginWithPassword POST");
                  string Message = "alert('Something went wrong! Please try again later.')";
              }
              return View("Login1");
           }
      }
}
