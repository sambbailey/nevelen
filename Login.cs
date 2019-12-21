using System;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Data.Sql;
using System.Data.OleDb;
using System.Data.SqlClient;

namespace Fox
{
    public partial class Login : Form
    {
        string connString = @"Data Source=nevelen.database.windows.net;Initial Catalog=NEVELEN;User ID=Fox;Password=NewYork123!;Connect Timeout=1;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        SqlConnection con = new SqlConnection();
        public Login()
        {
            InitializeComponent();
        }

        private void passwordtxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button2.PerformClick();
                e.SuppressKeyPress = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void usernametxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button2.PerformClick();
                e.SuppressKeyPress = true;

            }

        }

        private void usernametxt_TextChanged(object sender, EventArgs e)
        {
            usernametxt.CharacterCasing = CharacterCasing.Upper;
        }

        private void Login_Load(object sender, EventArgs e)
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            this.lblVersion.Text = String.Format(this.lblVersion.Text, version.Major, version.Minor, version.Build, version.Revision);
        }

        private void passwordtxt_Enter(object sender, EventArgs e)
        {
            passwordtxt.SelectAll();
        }

        private void usernametxt_Enter(object sender, EventArgs e)
        {
            usernametxt.SelectAll();
        }

        private void Login_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            Environment.Exit(1);
        }

        private void Login_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
                MessageBox.Show("This cannot be Maximized", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }


        private void button2_Click(object sender, EventArgs e)
        {
            userid = usernametxt.Text;
            SqlConnection cnn = new SqlConnection(connString);
            cnn.Open();
            if (cnn.State == ConnectionState.Open)
            {

                bool emptylogin =
                string.IsNullOrEmpty(usernametxt.Text)
                && string.IsNullOrEmpty(passwordtxt.Text);
                if (emptylogin)
                {

                    MessageBox.Show("Please enter a username and password to login.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                }
                else if (string.IsNullOrWhiteSpace(usernametxt.Text))
                {
                    MessageBox.Show("Please enter a username to login.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                }
                else if (string.IsNullOrWhiteSpace(passwordtxt.Text))
                {
                    MessageBox.Show("Please enter a password to login.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    try
                    {
                        ExpiredPasswordAccounts();
                        SqlConnection con = new SqlConnection(connString);
                        con.Open();
                        string txtUserId = usernametxt.Text;
                        string txtPassword = passwordtxt.Text;

                        using (SqlCommand selCmd = new SqlCommand("SELECT UserID, Full_Name, LoginID, Role FROM USERS WHERE UserId= @UserID and Password = @Password", con))
                        {
                            bool forcedPassword = FlaggedUserID == "N";
                            bool reminderPassword = FlaggedUserID == "Y";
                            bool permPassword = FlaggedUserID == "X";
                            bool cleanAccount = string.IsNullOrWhiteSpace(FlaggedUser);

                            selCmd.Parameters.AddWithValue("@UserID", txtUserId);
                            selCmd.Parameters.AddWithValue("@Password", txtPassword);
                            SqlDataReader dr = selCmd.ExecuteReader();
                            if (dr.Read())
                            {
                                string Fullname = dr["Full_Name"].ToString();
                                string LoginID = dr["LoginID"].ToString();
                                string Role = dr["Role"].ToString();
                                roles = Role;
                                fullnames = Fullname;
                                loginid = LoginID;
                            }
                            if (dr.HasRows)
                            {
                                if (forcedPassword)
                                {
                                    if (MessageBox.Show("Your Password Has Expired, Please Change Your Password To Login.", "Password Expired", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                                    {
                                        UserChangePassword UCP = new UserChangePassword();
                                        UCP.Show();
                                        this.Hide();
                                    }
                                }
                                if (reminderPassword)
                                {
                                    if (MessageBox.Show("Your Password Has Expired, Do You Wish To Change Your Password", "Password Expired", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                                    {
                                        UserChangePassword LEP = new UserChangePassword();
                                        LEP.Show();
                                        this.Hide();
                                    }
                                    else
                                    {
                                        LoginOpen();
                                    }
                                }
                                else if (permPassword || cleanAccount)
                                {
                                    LoginOpen();
                                }
                            }
                            else
                            {
                                if (MessageBox.Show("Invalid Username and Password, Please Try Again!", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
                                {
                                    if (MessageBox.Show("Are you sure you wish to quit?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                                    {
                                        this.Close();
                                    }
                                    else
                                    {
                                        //do nothing
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // We should log the error somewhere, 
                        // for this example let's just show a message
                        MessageBox.Show("ERROR: " + ex.Message);
                    }
                }
            }
        }

        public static string userid { get; set; }
        public static string roles { get; set; }
        public static string fullnames { get; set; }
        public static string loginid { get; set; }
        public static string FlaggedUser { get; set; }
        public static string FlaggedUserID { get; set; }

        private void ExpiredPasswordAccounts()
        {
            string sql = null;

            // Prepare a proper parameterized query 
            sql = "SELECT USERID , Perm_Password FROM USERS WHERE USERID = @USERID AND [PASSWORD_CHANGE_DATE] < = DATEADD(mm, -3, GETDATE())";

            // Create the connection (and be sure to dispose it at the end)
            using (SqlConnection cnn = new SqlConnection(connString))
            {
                try
                {
                    // Open the connection to the database. 
                    // This is the first critical step in the process.
                    // If we cannot reach the db then we have connectivity problems
                    cnn.Open();

                    // Prepare the command to be executed on the db
                    using (SqlCommand cmd = new SqlCommand(sql, cnn))
                    {
                        cmd.Parameters.Add("@USERID", SqlDbType.NVarChar).Value = usernametxt.Text;
                        // Let's ask the db to execute the query
                        SqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            string flaggedUser = dr["USERID"].ToString();
                            string flaggedID = dr["Perm_Password"].ToString();
                            FlaggedUser = flaggedUser;
                            FlaggedUserID = flaggedID;
                        }
                        else
                        {
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    // We should log the error somewhere, 
                    // for this example let's just show a message
                    MessageBox.Show("ERROR: " + ex.Message);
                }
            }
        }

        public static void WelcomeMessage()
        {
            MessageBox.Show("Welcome " + fullnames + ", this is in Beta so please expect bugs", "Welcome to Fox", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoginOpen()
        {
            FlaggedUserID = null;
            if (roles == "User" || roles == "Moderator")
            {
                Database.Last_Accessed();
                this.Hide();
                WelcomeMessage();
                Fox_Main Fox = new Fox_Main();
                Fox.Show();
            }
            else
            {
                Database.Last_Accessed();
                Fox_Main Fox = new Fox_Main();
                Fox.Show();
                this.Hide();
            }
        }
    }
}
