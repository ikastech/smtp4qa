<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Smtp4qa._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
    <style type="text/css">
   
    .altColor
    {
        background-color:#D6DADF; color: #000000; font-weight: lighter;
    }
    .aLink
    {
        /*color: #8CBAEF;*/
    }
    .nmColor
    {
        font-weight: lighter;
    }
    .tHeader
    {
        background-color:#3C454F; color:#FFFFFF; font-weight: bold;
    }
    .ui-datepicker { font-size:8pt !important}
    .cal
{
   background-image: url(calendar.gif);
background-repeat: no-repeat;
vertical-align: middle;
background-color: white;
background-position: right center;
cursor: pointer; 
   }
    </style>
  <link rel="stylesheet" href="//code.jquery.com/ui/1.10.4/themes/smoothness/jquery-ui.css">
  <script src="//code.jquery.com/jquery-1.10.2.js"></script>
  <script src="//code.jquery.com/ui/1.10.4/jquery-ui.js"></script>
  <script>
        $(function() {
        $("#txtDate").datepicker({ dateFormat: 'dd M yy'});
        });
    </script>
    
</head>
<body>
    <form id="form1" runat="server">
  
   
    <div id="divHeader" runat="server" style="text-align: center">
    
   <%-- <asp:Calendar ID="Calendar1" runat="server" OnSelectionChanged="Calendar1Change" ></asp:Calendar>    <asp:Button ID="Button1" runat="server" Text="Select Date" OnClick="Button1_Click" />--%>
    
    </div>
    
      <div id="divList" style="width:100%;" runat="server" >
          <table>
              <tr>
                  <td style="width: 70%">
                      
                     
                  </td>
                  
                  <td style="width: 40%; text-align: right;">
                      <asp:Label ID="lblError" Visible="false" runat="server" Text="Invalid date" ForeColor="Red"></asp:Label>   
                      <asp:TextBox ID="txtDate"  CssClass="cal" runat="server" AutoPostBack="True" OnTextChanged="txtDate_TextChanged" />
                   &nbsp;  &nbsp;&nbsp; <asp:LinkButton ID="lnkPrevious" runat="server" OnClick="lnkPrevious_Click">Previous</asp:LinkButton>&nbsp;
                      <asp:LinkButton ID="lnkNext" runat="server" OnClick="lnkNext_Click">Next</asp:LinkButton>
                     &nbsp;&nbsp; &nbsp;<asp:Label ID="lblCount" runat="server" ></asp:Label>
                  </td>
                 
                 
              </tr>
              
          </table>
        <asp:Repeater ID="rpt" runat="server">
        <HeaderTemplate>
       &nbsp;<table border="1" cellspacing="0.5" cellpadding="5">
        <thead class="tHeader" >
        <tr>
        <td style="width:10px;">No. </td>
        <td style="width:10%;">From </td>
        <td style="width:20%;">To </td>
        <td style="width:60%;">Subject </td>
        <td style="width:10%;">Date</td>
        </tr>
        </thead>
        
        </HeaderTemplate>
          <AlternatingItemTemplate>
          
                    <tr class="altColor">
                    
           <td > <%# Container.ItemIndex + 1 %></td><td><%# Eval("From") %> </td><td><%# Eval("To") %> </td><td><a href="Default.aspx?email='<%# Eval("eFile") %>' " target="_blank" class="aLink" > <%# Eval("Subject") %></a> </td><td><%# Eval("cDate")%></td>
            </tr>
                        </AlternatingItemTemplate>
            <ItemTemplate>
           <tr class="nmColor">
           
           <td > <%# Container.ItemIndex + 1 %></td> <td><%# Eval("From") %> </td><td><%# Eval("To") %> </td><td><a href="Default.aspx?email='<%# Eval("eFile") %>' " target="_blank" class="aLink" ><%# Eval("Subject") %></a> </td><td><%# Eval("cDate")%></td>
            </tr>
            </ItemTemplate>
           
            <FooterTemplate>
            </table>
            </FooterTemplate>
        </asp:Repeater>
    </div>
    
    <div id="divDetails"  runat="server">
       <asp:Literal ID="ltlDetails" runat="server" ></asp:Literal>
      </div>
    
    
    
    </form>
</body>
</html>
