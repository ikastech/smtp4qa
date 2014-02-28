<%@ Page Language="C#" %>

<%@ Register Assembly="System.Web.Extensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"    Namespace="System.Web.UI.WebControls" TagPrefix="asp" %>

<!DOCTYPE html>

<script runat="server" >
    static string EmailFolderPath = @"C:\TestEmail\";
    
    string[] files = System.IO.Directory.GetFiles(EmailFolderPath, "*.eml");
   
   
    
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
        {Calendar1.Visible = false;}

        IComparer fileComparer = new CompareFileByDate();
        Array.Sort(files, fileComparer);

  
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

            }
        }
    }
    
     public void LoadDetail(string path)
    {
        StringBuilder email = new StringBuilder();
        email.Append("<div><a href='Default.aspx' target='_blank' > <b>Back to list page </b></a> </div><br>");
        email.Append("<table border='1' />");
        email.Append("<tr>");
        email.Append("<td><b>Filename</b> </td>");
        email.Append("<td>" + path + "</td>");
        email.Append("<tr>");
         
            string _path, _to, _from, _subject, _urls;
            _path = EmailFolderPath + path;

            string fc = new System.IO.StreamReader(_path).ReadToEnd();
            _from = Regex.Matches(fc, "From: (.+)")[0].ToString();
            email.Append("<tr>");
            email.Append("<td><b>From</b> </td>");
            email.Append("<td>" + _from.Replace("From:","") + "</td>");
            email.Append("<tr>");
            _to = Regex.Matches(fc, "To: (.+)")[0].ToString();
            email.Append("<tr>");
            email.Append("<td><b>To</b> </td>");
            email.Append("<td>" + _to.Replace("To:","") + "</td>");
            email.Append("<tr>");
            _subject = Regex.Matches(fc, "Subject: (.+)")[0].ToString();
            email.Append("<tr>");
            email.Append("<td><b>Subject</b> </td>");
            email.Append("<td>" + _subject.Replace("Subject:","") + "</td>");
            email.Append("<tr>");

            
            _urls = string.Empty;
            foreach (Match m in Regex.Matches(fc,@"https?://([a-zA-Z\.]+)/"))
            {
                _urls += m.ToString() + ' ';
            }
            email.Append("<tr>");
            email.Append("<td><b>URLs</b> </td>");
            email.Append("<td>" + _urls + "</td>");
            email.Append("<tr>");
            System.IO.StreamReader rd = new System.IO.StreamReader(_path);
         int _tCount =0;
         StringBuilder _body = new StringBuilder();
         while (!rd.EndOfStream)
         {
             string temp = rd.ReadLine();
             
             if (_tCount > 0)
             {
                 _body.AppendLine(temp);
             }
             if (temp == "Content-Transfer-Encoding: quoted-printable" && _tCount==0)
             {
                 _tCount =1; 
             }
             
         }
         email.Append("<tr>");
         email.Append("<td><b>Body </b> </td>");
         email.Append("<td>" + _body + "</td>");
         email.Append("<tr>");
         
         email.Append("</table>");
         
         ltlDetails.Text = email.ToString();

         divList.Style.Add("display", "none");
         divDetails.Style.Remove("display");
     }


     protected void FileList(DateTime today)
    {
        
        System.Collections.Generic.List<string> todaysList = new System.Collections.Generic.List<string>();
        foreach (string fiInfo in files)
        {
            System.IO.FileInfo flInfo = new System.IO.FileInfo(fiInfo);
            if (flInfo.CreationTime.Date == today.Date) //use directly flInfo.CreationTime and flInfo.Name without create another variable 
            {
                todaysList.Add(flInfo.Name);
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
</script>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <asp:Label ID="lblTest" runat="server" Text=""></asp:Label>
    
    
    
    <div id="divHeader" runat="server" style="text-align: center">
    <hr />
    <asp:Calendar ID="Calendar1" runat="server" OnSelectionChanged="Calendar1Change" ></asp:Calendar>    <asp:Button ID="Button1" runat="server" Text="Select Date" OnClick="Button1_Click" />
    <asp:Label ID="lblSelectCal" runat="server" Text=""></asp:Label>
    <hr />
    </div>
  
    
      <div id="divList" style="width:100%;" runat="server" >
        <asp:Repeater ID="rpt" runat="server">
        <HeaderTemplate>
        <table border="1">
        <thead>
        <tr>
        <td >No</td>
        <td>Email (Sorted by latest email)</td>
        </tr>
        </thead>
        
        </HeaderTemplate>
        
            <ItemTemplate>
        <tr>
           <td > <%# Container.ItemIndex + 1 %></td> <td> <a href="Default.aspx?email='<%# Container.DataItem %>' " target="_blank" > <%# Container.DataItem %></a></td>
            </tr>
            </ItemTemplate>
           
            <FooterTemplate>
            </table>
            </FooterTemplate>
        </asp:Repeater>
    </div>
    
    <div id="divDetails" style="width:100%;" runat="server">
        <asp:Literal ID="ltlDetails" runat="server"></asp:Literal>
    </div>
    
    
    
    </form>
</body>
</html>
