using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
namespace CompanyProject.Models
{
    public class DbManager
    {
        private readonly string _connectionString;

        public DbManager()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["mycon"].ConnectionString;
        }
         public SqlConnection OpenConnection()
    {
        SqlConnection con = new SqlConnection(_connectionString);
        if (con.State == ConnectionState.Closed)
            con.Open();

        return con;
    }
         public void CloseConnection(SqlConnection con)
    {
        if (con != null && con.State == ConnectionState.Open)
            con.Close();
    }
         public int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
    {
        SqlConnection con = OpenConnection();

        SqlCommand cmd = new SqlCommand(query, con);

        if (parameters != null)
            cmd.Parameters.AddRange(parameters);

        int result = cmd.ExecuteNonQuery();

        CloseConnection(con);

        return result;
    }
         public int ExecuteNonQuerySP(string spName, SqlParameter[] parameters = null)
         {
             SqlConnection con = OpenConnection();

             SqlCommand cmd = new SqlCommand(spName, con);
             cmd.CommandType = CommandType.StoredProcedure;

             if (parameters != null)
                 cmd.Parameters.AddRange(parameters);

             int result = cmd.ExecuteNonQuery();

             CloseConnection(con);

             return result;
         }

         public DataTable ExecuteReaderSP(string spName, SqlParameter[] parameters = null)
         {
             SqlConnection con = OpenConnection();

             SqlCommand cmd = new SqlCommand(spName, con);
             cmd.CommandType = CommandType.StoredProcedure;

             if (parameters != null)
                 cmd.Parameters.AddRange(parameters);

             SqlDataAdapter da = new SqlDataAdapter(cmd);
             DataTable dt = new DataTable();
             da.Fill(dt);

             CloseConnection(con);

             return dt;
         }

        public DataTable GetData(string query, SqlParameter[] parameters)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, con);
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public DataTable GetDataSP(string spName, SqlParameter[] parameters = null)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand(spName, con);
                cmd.CommandType = CommandType.StoredProcedure;
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public string CaptchaCode()
        {
            char ch1, ch2, ch3, ch4, ch5;
            Random r = new Random();
            ch1 = Convert.ToChar(r.Next(65, 92));
            ch2 = Convert.ToChar(r.Next(97, 122));

            ch3 = Convert.ToChar(r.Next(50, 55));

            ch4 = Convert.ToChar(r.Next(65, 92));

            ch5 = Convert.ToChar(r.Next(97, 122));

            string cph = ch1 + "" + ch2 + "" + ch3 + "" + ch4 + "" + ch5;
            return cph;
        }

        public object ExecuteScalar(string query, SqlParameter[] parameters = null)
        {
            SqlConnection con = OpenConnection();

            SqlCommand cmd = new SqlCommand(query, con);

            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            object result = cmd.ExecuteScalar();

            CloseConnection(con);

            return result;
        }
        public object ExecuteScalarSP(string spName, SqlParameter[] parameters = null)
        {
            SqlConnection con = OpenConnection();

            SqlCommand cmd = new SqlCommand(spName, con);
            cmd.CommandType = CommandType.StoredProcedure;

            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            object result = cmd.ExecuteScalar();

            CloseConnection(con);

            return result;
        }
        public int GetCounter(string tableName)
        {
            int counter = 0;
            string selectQuery = "SELECT CounterId FROM tab_Counter WHERE TableName = @TableName";

            SqlParameter[] selectParam =
            {
              new SqlParameter("@TableName", tableName)
            };

               DataTable dt = GetData(selectQuery, selectParam);

                if (dt.Rows.Count > 0)
              {
                 counter = Convert.ToInt32(dt.Rows[0]["CounterId"]) + 1;
                 string updateQuery = "UPDATE tab_Counter SET CounterId = @CounterId WHERE TableName = @TableName";
             SqlParameter[] updateParam =
             {
                new SqlParameter("@CounterId", counter),
                new SqlParameter("@TableName", tableName)
             };
                ExecuteNonQuery(updateQuery, updateParam);
            }
            return counter;
        }
        public void FailSave(string Message, int LineNo, string PageName)

        {
            int id = GetCounter("FailSave");
            string query = "INSERT INTO FailSave (Id, ErrorMessage, ErrorDate, Line_No, PageName) VALUES (@Id, @ErrorMessage, GETDATE(), @Line_No, @pageName)";

           SqlParameter[] param = 
           {
               new SqlParameter("@Id", id),
               new SqlParameter("@ErrorMessage", Message),
               new SqlParameter("@Line_No", LineNo),
               new SqlParameter("@PageName", PageName)
           };
            ExecuteNonQuery(query, param);
        }
    }
}