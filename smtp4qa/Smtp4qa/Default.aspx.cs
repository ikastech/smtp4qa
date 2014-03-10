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
using System.Data;


namespace Smtp4qa
{
    public partial class _Default : System.Web.UI.Page,IDisposable
    {
         string EmailFolderPath = ConfigurationManager.AppSettings["EmailFolderPath"];

       // string[] files = System.IO.Directory.GetFiles(EmailFolderPath, "*.eml").OrderBy(p => p.CreationTime).ToArray();
        FileInfo[] files;

        //bool showCalendar = false;

        public class CompareFileByDate : IComparer
        {
            int IComparer.Compare(object a, object b)
            {
                System.IO.FileInfo fia = new System.IO.FileInfo(a.ToString());
                System.IO.FileInfo fib = new System.IO.FileInfo(b.ToString());

                DateTime cta = fia.CreationTime;
                DateTime ctb = fia.CreationTime;

                return DateTime.Compare(ctb, cta);
            }
        }

        protected void Page_Load(object source, EventArgs e)
        {
            DirectoryInfo info = new DirectoryInfo(EmailFolderPath);

            files = info.GetFiles("*.eml").OrderByDescending(p => p.CreationTime).ToArray();
            if (!IsPostBack)
            {
                txtDate.Text = ChoosenDate.ToString("dd MMM yyyy");
            }
            if (Request.QueryString["email"] == null)
            {
                FileList(ChoosenDate);
                SetDates(ChoosenDate);
                //lblSelectCal.Text = "List of email date is " + ChoosenDate.ToString("dd MMM yyyy");
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
            Page.Title = path;
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

            string _path, _to, _from, _subject,_cc,_date, _urls;
            _path = EmailFolderPath + path;

            eMail em = GeteMailParticulars(path);
            _from = em.From;
            _to = em.To;
            _cc = em.Cc;
            _date = em.Date;
            _subject = em.Subject;

            email.Append("<tr class='altColor'>");
            email.Append("<td><b>FROM</b> </td>");
            email.Append("<td>" + _from + "</td>");
            email.Append("<tr>");
            
            email.Append("<tr class='nmColor'>");
            email.Append("<td><b>TO</b> </td>");
            email.Append("<td>" + _to + "</td>");
            email.Append("<tr>");

            email.Append("<tr class='altColor'>");
            email.Append("<td><b>CC</b> </td>");
            email.Append("<td>" + _cc + "</td>");
            email.Append("<tr>");

            email.Append("<tr class='nmColor'>");
            email.Append("<td><b>DATE</b> </td>");
            email.Append("<td>" + _date + "</td>");
            email.Append("<tr>");

            email.Append("<tr class='altColor'>");
            email.Append("<td><b>SUBJECT</b> </td>");
            email.Append("<td>" + _subject + "</td>");
            email.Append("<tr>");



            //using (StreamReader sr = new  StreamReader(_path))
            //{
            //    string fc = sr.ReadToEnd();
            //    _from = Regex.Matches(fc, "From: (.+)")[0].ToString();
            //    email.Append("<tr class='altColor'>");
            //    email.Append("<td><b>FROM</b> </td>");
            //    email.Append("<td>" + _from.Replace("From:", "") + "</td>");
            //    email.Append("<tr>");
            //    _to = Regex.Matches(fc, "To: (.+)")[0].ToString();
            //    email.Append("<tr class='nmColor'>");
            //    email.Append("<td><b>TO</b> </td>");
            //    email.Append("<td>" + _to.Replace("To:", "") + "</td>");
            //    email.Append("<tr>");
            //    _subject = Regex.Matches(fc, "Subject: (.+)")[0].ToString();
            //    email.Append("<tr class='altColor'>");
            //    email.Append("<td><b>SUBJECT</b> </td>");
            //    email.Append("<td>" + _subject.Replace("Subject:", "") + "</td>");
            //    email.Append("<tr>");


            //    //_urls = string.Empty;
            //    //foreach (Match m in Regex.Matches(fc, @"https?://([a-zA-Z\.]+)/"))
            //    //{
            //    //    _urls += m.ToString() + ' ';
            //    //}
            //    //email.Append("<tr class='altColor'>");
            //    //email.Append("<td><b>URLs</b> </td>");
            //    //email.Append("<td>" + _urls + "</td>");
            //    //email.Append("<tr>");
            //}
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

        public eMail GeteMailParticulars(string path)
        {
            //StringBuilder email = new StringBuilder();
            string _subject, _to, _from, _cc,_Bcc,_date;
            string _path = EmailFolderPath + path;
            eMail em = new eMail();
            
            using (StreamReader sr = new StreamReader(_path))
            {
                
                while (!sr.EndOfStream)
                {
                    string fc = sr.ReadLine();
                    if (fc.ToString().Contains("From: "))
                    {
                        _from = Regex.Matches(fc, "<(.+)>")[0].ToString();
                        em.From = _from.Replace("From:", "").Replace("<","").Replace(">","");
                    }
                    if (fc.ToString().Contains("To: "))
                    {
                        if (Regex.Matches(fc, "<(.+)>").Count > 0)
                        {
                            _to = Regex.Matches(fc, "<(.+)>")[0].ToString();
                        }
                        else
                        {
                            _to = Regex.Matches(fc, "(.+)")[0].ToString();
                        }
                        em.To = _to.Replace("To:", "");
                    }
                    if (fc.ToString().Contains("Cc: "))
                    {
                        if (Regex.Matches(fc, "<(.+)>").Count > 0)
                        {
                            _cc = Regex.Matches(fc, "<(.+)>")[0].ToString();
                        }
                        else
                        {
                            _cc = Regex.Matches(fc, "(.+)")[0].ToString();
                        }
                        em.Cc = _cc.Replace("Cc:", "").Replace("<", "").Replace(">", ""); 
                    }
                    if (fc.ToString().Contains("Bcc: "))
                    {
                        if (Regex.Matches(fc, "<(.+)>").Count > 0)
                        {
                            _cc = Regex.Matches(fc, "<(.+)>")[0].ToString();
                        }
                        else
                        {
                            _cc = Regex.Matches(fc, "(.+)")[0].ToString();
                        }
                        em.Bcc = _cc.Replace("Bcc:", "").Replace("<", "").Replace(">", ""); 
                    }
                    if (fc.ToString().Contains("Date: "))
                    {
                        if (Regex.Matches(fc, "<(.+)>").Count > 0)
                        {
                            _date = Regex.Matches(fc, "<(.+)>")[0].ToString();
                        }
                        else
                        {
                            _date = Regex.Matches(fc, "(.+)")[0].ToString();
                        }
                        em.Date = _date.Replace("Date:", "") ;
                    }
                    if (fc.ToString().Contains("Subject: "))
                    {
                        _subject = Regex.Matches(fc, "(.+)")[0].ToString();
                        em.Subject = _subject.Replace("Subject:", "");
                    }
                    
                }
            }

            return em;

        }

        //protected void FileList(DateTime today)
        //{
        //    System.Collections.Generic.List<string> todaysList = new System.Collections.Generic.List<string>();
        //    foreach (FileInfo fiInfo in files)
        //    {
        //        //System.IO.FileInfo flInfo = new System.IO.FileInfo(fiInfo);
        //        if (fiInfo.CreationTime.Date == today.Date) //use directly flInfo.CreationTime and flInfo.Name without create another variable 
        //        {
        //            todaysList.Add(fiInfo.Name);
        //        }
        //    }
        //    LoadList(todaysList);
        //}

        protected void FileList(DateTime today)
        {
           
            


            DataTable dt = new DataTable();
            dt.Columns.Add("From");
            dt.Columns.Add("To");
            dt.Columns.Add("Subject");
            dt.Columns.Add("eFile");
            dt.Columns.Add("cDate");
            DataRow dr;
            foreach (FileInfo fiInfo in files)
            {
               
                //System.IO.FileInfo flInfo = new System.IO.FileInfo(fiInfo);
                if (fiInfo.CreationTime.Date == today.Date) //use directly flInfo.CreationTime and flInfo.Name without create another variable 
                {
                    dr = dt.NewRow();
                    eMail em = GeteMailParticulars(fiInfo.Name);
                    dr["From"] = em.From;
                    dr["To"] = em.To;
                    dr["Subject"] = em.Subject;
                    dr["eFile"] = fiInfo.Name;
                    dr["cDate"] = RealTimeCalcultation(fiInfo.CreationTime);
                    dt.Rows.Add(dr);
                }
            }
            lblCount.Text = dt.Rows.Count+" Email(s)";
            Page.Title ="["+dt.Rows.Count+"] "+ today.ToShortDateString();
            LoadList(dt);
        }
        protected string RealTimeCalcultation(DateTime emailTime)
        {
            DateTime postTime = emailTime;
            DateTime requestTime = DateTime.Now;
            var result = requestTime - postTime;
            string resTime = string.Empty;
            if (result.TotalDays >= 1)
            {
                if ((int)result.TotalDays > 2)
                {
                    if (emailTime.Year == requestTime.Year)
                    {
                        return resTime = emailTime.Date.ToString("dd/MMM");
                    }
                    else
                    {
                        return resTime = emailTime.Date.ToString("dd/MMM/yyyy");
                    }
                }
                else
                {
                    return resTime = string.Format("{0} Days ago", (int)result.TotalDays);
                }
            }
            if (result.TotalHours >= 1)
                return resTime = string.Format("{0} Hours ago", (int)result.TotalHours);
            if (result.TotalMinutes != 0)
                return resTime = string.Format("{0} Minutes ago",(int) result.TotalMinutes);

            return resTime;
        }
        protected void LoadList(System.Collections.Generic.List<string> fileList)
        {
            rpt.DataSource = fileList;
            rpt.DataBind();
            divDetails.Style.Add("display", "none");
            divList.Style.Remove("display");
        }
        protected void LoadList(DataTable fileList)
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

        private void SetDates(DateTime SetDate)
        {
            lnkNext.Enabled = true;
            lnkPrevious.Enabled = true;
            DateTime CurrentDate = DateTime.Now;
            if (SetDate.Date == CurrentDate.Date)
            {
                PreviousDate = SetDate.Date.AddDays(-1);
                lnkNext.Enabled = false;
            }
            else if (SetDate.Date < CurrentDate.Date)
            {
                PreviousDate = SetDate.Date.AddDays(-1);
                NextDate = SetDate.Date.AddDays(1);
            }
            else if (SetDate.Date > CurrentDate.Date)
            {
                PreviousDate = CurrentDate.Date;
                lnkNext.Enabled = false;
            }
            ChoosenDate = SetDate;
            
        }

        //protected void Calendar1Change(object source, EventArgs e)
        //{
        //    ViewState.Add("CalDate", Calendar1.SelectedDate);
        //    lblSelectCal.Text = "Selected date :" + Calendar1.SelectedDate.ToString();
        //    // FileList(Calendar1.SelectedDate.Date);
        //    ChoosenDate = Calendar1.SelectedDate;
        //    FileList(ChoosenDate);
        //    Button1.Visible = true;
        //    lblSelectCal.Text = "List of email date is " + ChoosenDate.ToString();
        //}
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
        public DateTime PreviousDate
        {
            get
            {
                object o = ViewState["PreviousDate"];
                if (o != null) return (DateTime)o;
                return DateTime.Today;
            }
            set
            { ViewState["PreviousDate"] = value; }
        }
        public DateTime NextDate
        {
            get
            {
                object o = ViewState["NextDate"];
                if (o != null) return (DateTime)o;
                return DateTime.Today;
            }
            set
            { ViewState["NextDate"] = value; }
        }

        protected void txtDate_TextChanged(object sender, EventArgs e)
        {
            try
            {
                DateTime selDate = Convert.ToDateTime(txtDate.Text);
                SelectedDateList(selDate);
                SetDates(selDate);
                txtDate.Text = selDate.ToString("dd MMM yyyy");
                lblError.Visible = false;
            }
            catch (Exception)
            {
                lblError.Visible = true;
                
            }
            
        }

        private void SelectedDateList(DateTime selDate)
        {
            ViewState.Add("CalDate", selDate);
           // lblSelectCal.Text = "Selected date :" + selDate.ToString();
            ChoosenDate = selDate;
            FileList(ChoosenDate);
           // lblSelectCal.Text = "List of email date is " + ChoosenDate.ToString("dd MMM yyyy");
           // return selDate;
        }

        protected void lnkPrevious_Click(object sender, EventArgs e)
        {
            SelectedDateList(PreviousDate);
            txtDate.Text = PreviousDate.ToString("dd MMM yyyy");
            SetDates(PreviousDate);
            lblError.Visible = false;
        }

        protected void lnkNext_Click(object sender, EventArgs e)
        {
            SelectedDateList(NextDate);
            txtDate.Text = NextDate.ToString("dd MMM yyyy");
            SetDates(NextDate);
            lblError.Visible = false;
        }

        
    }
    public class eMail 
    {
        string _to,_bcc, _cc, _from, _subject, _body,_date;

        public string To
        {
            get { return _to; }
            set { _to = value; }
        }
        public string Cc
        {
            get { return _cc; }
            set { _cc = value; }
        }
        public string Bcc
        {
            get { return _bcc; }
            set { _bcc = value; }
        }
        public string From
        {
            get { return _from; }
            set { _from = value; }
        }
        public string Subject
        {
            get { return _subject; }
            set { _subject = value; }
        }
        public string Body
        {
            get { return _body; }
            set { _body = value; }
        }
        public string Date
        {
            get { return _date; }
            set { _date = value; }
        }
    }
}
