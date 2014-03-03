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
    
    
    </style>
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
       <table border="1" cellspacing="0.5" cellpadding="5">
        <thead class="tHeader" >
        <tr>
        <td style="width:20;">No. </td>
        <td style="width:100%; text-align: center;">Email (Recent first)</td>
        </tr>
        </thead>
        
        </HeaderTemplate>
          <AlternatingItemTemplate>
                    <tr class="altColor">
           <td > <%# Container.ItemIndex + 1 %></td> <td> <a href="Default.aspx?email='<%# Container.DataItem %>' " target="_blank" class="aLink" > <%# Container.DataItem %></a></td>
            </tr>
                        </AlternatingItemTemplate>
            <ItemTemplate>
           <tr class="nmColor">
           <td > <%# Container.ItemIndex + 1 %></td> <td> <a href="Default.aspx?email='<%# Container.DataItem %>' " target="_blank" class="aLink" > <%# Container.DataItem %></a></td>
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
