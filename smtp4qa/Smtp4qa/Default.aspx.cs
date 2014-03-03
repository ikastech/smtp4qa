using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using glib.Email;
using System.Net.Mail;
using System.Configuration;

namespace Smtp4qa
{
    public partial class _Default : System.Web.UI.Page,IDisposable
    {
         string EmailFolderPath = ConfigurationManager.AppSettings["EmailFolderPath"];

       // string[] files = System.IO.Directory.GetFiles(EmailFolderPath, "*.eml").OrderBy(p => p.CreationTime).ToArray();
        FileInfo[] files;

        bool showCalendar = false;

        public class CompareFileByDate : IComparer
        {
            int IComparer.Compare(Object a, Object b)
            {
                System.IO.FileInfo fia = new System.IO.FileInfo((string)a);
                System.IO.FileInfo fib = new System.IO.FileInfo((string)b);

                DateTime cta = fia.CreationTime;
                DateTime ctb = fib.CreationTime;

                return DateTime.Compare(ctb, cta);
            }
        }
      
        protected void Page_Load(object source, EventArgs e)
        {
            if (showCalendar)
            { Calendar1.Visible = true; }
            else
            { Calendar1.Visible = false; }

            //IComparer fileComparer = new CompareFileByDate();
            //Array.Sort(files, fileComparer);

            DirectoryInfo info = new DirectoryInfo(EmailFolderPath);

            files = info.GetFiles("*.eml").OrderBy(p => p.CreationTime).ToArray();
            
            lblSelectCal.Text = "List of email date is " + ChoosenDate.ToString();

            if (Request.QueryString["email"] == null)
            {
                FileList(ChoosenDate);
            }
            else
            {
                try
                {
                    string selectedFile = Request.QueryString["email"].ToString();
                    if (selectedFile != "")
                    {
                        LoadDetail(selectedFile.Replace("'", ""));
                        divHeader.Style.Add("display", "none");
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        public string GetBody(string path)
        {
            MimeReader mime = new MimeReader();

            RxMailMessage mm = mime.GetEmail(path);

            return GetPlainText(mm);

            //divList.Style.Add("display", "none");
            
            //divDetails.Style.Remove("display");
        }
        private string GetPlainText(RxMailMessage mm)
        {
            // check for plain text in body
            if (!mm.IsBodyHtml && !string.IsNullOrEmpty(mm.Body))
                return mm.Body;

            string sText = string.Empty;
            foreach (AlternateView av in mm.AlternateViews)
            {
                // check for plain text
                if (string.Compare(av.ContentType.MediaType, "text/plain", true) == 0)
                    continue;// return StreamToString(av.ContentStream);

                // check for HTML text
                if (string.Compare(av.ContentType.MediaType, "text/html", true) == 0)
                    sText = StreamToString(av.ContentStream);
            }

            // HTML is our only hope
            if (sText == string.Empty && mm.IsBodyHtml && !string.IsNullOrEmpty(mm.Body))
                sText = mm.Body;

            if (sText == string.Empty)
                return string.Empty;

            // need to convert the HTML to plaintext
           // return PgUtil.StripHTML(sText);

            return sText;
        }
        private static string StreamToString(Stream stream)
        {
            string sText = string.Empty;
            using (StreamReader sr = new StreamReader(stream))
            {
                sText = sr.ReadToEnd();
                stream.Seek(0, SeekOrigin.Begin);   // leave the stream the way we found it
                stream.Close();
            }

            return sText;
        }

        public void LoadDetail(string path)
        {
            StringBuilder email = new StringBuilder();
            email.Append("<div><a href='Default.aspx' target='_blank' > <b>Back to list page </b></a> </div><br>");
            email.Append("<table border='1' cellspacing='0.5' cellpadding='5' />");
            email.Append("<thead class='tHeader' >");
        email.Append("<tr>");
        email.Append("<td style='width:20;'>Content </td>");
        email.Append("<td style='width:100%; text-align: center;'>Details</td>");
        email.Append("</tr>");
        email.Append("</thead>");
            email.Append("<tr class='nmColor'>");
            email.Append("<td><b>FILENAME</b> </td>");
            email.Append("<td>" + path + "</td>");
            email.Append("<tr>");

            string _path, _to, _from, _subject, _urls;
            _path = EmailFolderPath + path;

            using (StreamReader sr = new  StreamReader(_path))
            {
                string fc = sr.ReadToEnd();
                _from = Regex.Matches(fc, "From: (.+)")[0].ToString();
                email.Append("<tr class='altColor'>");
                email.Append("<td><b>FROM</b> </td>");
                email.Append("<td>" + _from.Replace("From:", "") + "</td>");
                email.Append("<tr>");
                _to = Regex.Matches(fc, "To: (.+)")[0].ToString();
                email.Append("<tr class='nmColor'>");
                email.Append("<td><b>TO</b> </td>");
                email.Append("<td>" + _to.Replace("To:", "") + "</td>");
                email.Append("<tr>");
                _subject = Regex.Matches(fc, "Subject: (.+)")[0].ToString();
                email.Append("<tr class='altColor'>");
                email.Append("<td><b>SUBJECT</b> </td>");
                email.Append("<td>" + _subject.Replace("Subject:", "") + "</td>");
                email.Append("<tr>");


                //_urls = string.Empty;
                //foreach (Match m in Regex.Matches(fc, @"https?://([a-zA-Z\.]+)/"))
                //{
                //    _urls += m.ToString() + ' ';
                //}
                //email.Append("<tr class='altColor'>");
                //email.Append("<td><b>URLs</b> </td>");
                //email.Append("<td>" + _urls + "</td>");
                //email.Append("<tr>");
            }
            //System.IO.StreamReader rd = new System.IO.StreamReader(_path);
            //int _tCount = 0;
            //StringBuilder _body = new StringBuilder();
            //while (!rd.EndOfStream)
            //{
            //    string temp = rd.ReadLine();

            //    if (_tCount > 0)
            //    {
            //        _body.AppendLine(temp);
            //    }
            //    if (temp == "Content-Transfer-Encoding: quoted-printable" && _tCount == 0)
            //    {
            //        _tCount = 1;
            //    }

            //}


            email.Append("<tr class='nmColor'>");
            email.Append("<td><b>BODY </b> </td>");
            email.Append("<td>" + GetBody(_path).Replace("\n", "<br>") + "</td>");
            email.Append("<tr>");

            email.Append("</table>");

            ltlDetails.Text = email.ToString();

            divList.Style.Add("display", "none");
            divDetails.Style.Remove("display");
        }


        protected void FileList(DateTime today)
        {
            System.Collections.Generic.List<string> todaysList = new System.Collections.Generic.List<string>();
            
            foreach (FileInfo fiInfo in files)
            {
                //System.IO.FileInfo flInfo = new System.IO.FileInfo(fiInfo);
                if (fiInfo.CreationTime.Date == today.Date) //use directly flInfo.CreationTime and flInfo.Name without create another variable 
                {
                    todaysList.Add(fiInfo.Name);
                }
            }
            
            LoadList(todaysList);
        }

        protected void LoadList(System.Collections.Generic.List<string> fileList)
        {
            rpt.DataSource = fileList;
            rpt.DataBind();
            divDetails.Style.Add("display", "none");
            divList.Style.Remove("display");
        }
        protected void LoadList(string[] fileList)
        {
            rpt.DataSource = fileList;
            rpt.DataBind();
            divDetails.Style.Add("display", "none");
            divList.Style.Remove("display");
        }
        protected void Calendar1Change(object source, EventArgs e)
        {
            ViewState.Add("CalDate", Calendar1.SelectedDate);
            lblSelectCal.Text = "Selected date :" + Calendar1.SelectedDate.ToString();
            // FileList(Calendar1.SelectedDate.Date);
            ChoosenDate = Calendar1.SelectedDate;
            FileList(ChoosenDate);
            Button1.Visible = true;
            lblSelectCal.Text = "List of email date is " + ChoosenDate.ToString();
        }
        public DateTime ChoosenDate
        {
            get
            {
                object o = ViewState["ChoosenDate"];
                if (o != null) return (DateTime)o;
                return DateTime.Today;
            }
            set
            { ViewState["ChoosenDate"] = value; }
        }
        protected void Button1_Click(object sender, EventArgs e)
        {
            Calendar1.Visible = true;
            Button1.Visible = false;
        }
    }
}
