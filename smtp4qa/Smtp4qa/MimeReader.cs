// MimeReader
// =================
//
// copyright by Peter Huber, Singapore, 2006
// this code is provided as is, bugs are probable, free for any use at own risk, no 
// responsibility accepted. All rights, title and interest in and to the accompanying content retained.  :-)
//
// based on Standard for ARPA Internet Text Messages, http://rfc.net/rfc822.html
// based on MIME Standard,  Internet Message Bodies, http://rfc.net/rfc2045.html
// based on MIME Standard, Media Types, http://rfc.net/rfc2046.html
// based on QuotedPrintable Class from ASP emporium, http://www.aspemporium.com/classes.aspx?cid=6

// based on MIME Standard, E-mail Encapsulation of HTML (MHTML), http://rfc.net/rfc2110.html
// based on MIME Standard, Multipart/Related Content-type, http://rfc.net/rfc2112.html


// ?? RFC 2557       MIME Encapsulation of Aggregate Documents http://rfc.net/rfc2557.html

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;


namespace glib.Email
{
    // Mime Reader
    // =================

    /// <summary>
    /// Reads MIME based emails streams and files
    /// </summary>
    public class MimeReader
    {
        /// <summary>
        /// Reader for Email streams
        /// </summary>
        protected StreamReader EmailStreamReader;
        /// <summary>
        /// char 'array' for carriage return / line feed
        /// </summary>
        protected string CRLF = "\r\n";

        //character array 'constants' used for analysing MIME
        //----------------------------------------------------------
        private static char[] BlankChars = { ' ' };
        private static char[] BracketChars = { '(', ')' };
        private static char[] ColonChars = { ':' };
        private static char[] CommaChars = { ',' };
        private static char[] EqualChars = { '=' };
        private static char[] ForwardSlashChars = { '/' };
        private static char[] SemiColonChars = { ';' };

        private static char[] WhiteSpaceChars = { ' ', '\t' };
        private static char[] NonValueChars = { '"', '(', ')' };


        //Help for debugging
        //------------------
        /// <summary>
        /// used for debugging. Collects all unknown header lines for all (!) emails received
        /// </summary>
        public static bool isCollectUnknowHeaderLines = true;
        /// <summary>
        /// list of all unknown header lines received, for all (!) emails 
        /// </summary>
        public static List<string> AllUnknowHeaderLines = new List<string>();



#if UNEEDED_CODE
        /// <summary>
        /// Set this flag, if you would like to get also the email in the raw US-ASCII format
        /// as received.
        /// Good for debugging, but takes quiet some space.
        /// </summary>
        public bool IsCollectRawEmail
        {
            get { return isGetRawEmail; }
            set { isGetRawEmail = value; }
        }
        private bool isGetRawEmail = false;
#endif


        // MimeReader Constructor
        //---------------------------

        /// <summary>
        /// constructor
        /// </summary>
        public MimeReader()
        {
        }


        /// <summary>
        /// Gets an email from the supplied Email Stream and processes it.
        /// </summary>
        /// <param name="sEmlPath">string that designates the path to a .EML file</param>
        /// <returns>RxMailMessage or null if email not properly formatted</returns>
        public RxMailMessage GetEmail(string sEmlPath)
        {
            RxMailMessage mm = null;
            using (Stream EmailStream = File.Open(sEmlPath, FileMode.Open))
            {
                mm = GetEmail(EmailStream);
            }

            return mm;
        }

        /// <summary>
        /// Gets an email from the supplied Email Stream and processes it.
        /// </summary>
        /// <param name="sEmlPath">Stream that designates the an Email stream</param>
        /// <returns>RxMailMessage or null if email not properly formatted</returns>
        public RxMailMessage GetEmail(Stream EmailStream)
        {
            EmailStreamReader = new StreamReader(EmailStream, Encoding.ASCII);

            //prepare message, set defaults as specified in RFC 2046
            //although they get normally overwritten, we have to make sure there are at least defaults
            RxMailMessage Message = new RxMailMessage();
            Message.ContentTransferEncoding = TransferEncoding.SevenBit;
            Message.TransferType = "7bit";

            //convert received email into RxMailMessage
            MimeEntityReturnCode MessageMimeReturnCode = ProcessMimeEntity(Message, "");

            if (MessageMimeReturnCode == MimeEntityReturnCode.bodyComplete || MessageMimeReturnCode == MimeEntityReturnCode.parentBoundaryEndFound)
            {
                // I've seen EML files that don't have a "To: entity but have "x-receiver:" entity set to the recipient. check and use that if need be
                if (Message.To.Count == 0)
                {
                    // do something with
                    string sTo = Message.Headers["x-receiver"];
                    if (!string.IsNullOrEmpty(sTo))
                        Message.To.Add(sTo);
                }

                // From: maybe also but never have seen it missing
                if (Message.From == null)
                {
                    // do something with
                    string sFrom = Message.Headers["x-sender"];
                    if (!string.IsNullOrEmpty(sFrom))
                        Message.From = new MailAddress(sFrom);
                }

                //TraceFrom("email with {0} body chars received", Message.Body.Length);
                return Message;
            }
            return null;
        }

        private void callGetEmailWarning(string warningText, params object[] warningParameters)
        {
            string warningString;
            try
            {
                warningString = string.Format(warningText, warningParameters);
            }
            catch (Exception)
            {
                //some strange email address can give string.Format() a problem
                warningString = warningText;
            }
            //CallWarning("GetEmail", "", "Problem EmailNo {0}: " + warningString, messageNo);
        }


        /// <summary>
        /// indicates the reason how a MIME entity processing has terminated
        /// </summary>
        private enum MimeEntityReturnCode
        {
            undefined = 0, //meaning like null
            bodyComplete, //end of message line found
            parentBoundaryStartFound,
            parentBoundaryEndFound,
            problem //received message doesn't follow MIME specification
        }


        //buffer used by every ProcessMimeEntity() to store  MIME entity
        StringBuilder MimeEntitySB = new StringBuilder(100000);

        /// <summary>
        /// Process a MIME entity
        /// 
        /// A MIME entity consists of header and body.
        /// Separator lines in the body might mark children MIME entities
        /// </summary>
        private MimeEntityReturnCode ProcessMimeEntity(RxMailMessage message, string parentBoundaryStart)
        {
            bool hasParentBoundary = parentBoundaryStart.Length > 0;
            string parentBoundaryEnd = parentBoundaryStart + "--";
            MimeEntityReturnCode boundaryMimeReturnCode;

            //some format fields are inherited from parent, only the default for
            //ContentType needs to be set here, otherwise the boundary parameter would be
            //inherited too !
            message.SetContentTypeFields("text/plain; charset=us-ascii");

            //get header
            //----------
            string completeHeaderField = null;     //consists of one start line and possibly several continuation lines
            string response;

            // read header lines until empty line is found (end of header)
            while (true)
            {
                if (!readMultiLine(out response))
                {
                    callGetEmailWarning("incomplete MIME entity header received");
                    //empty this message
                    while (readMultiLine(out response)) { }
                    System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this
                    return MimeEntityReturnCode.problem;
                }

                if (response.Length < 1)
                {
                    //empty line found => end of header
                    if (completeHeaderField != null)
                    {
                        ProcessHeaderField(message, completeHeaderField);
                    }
                    else
                    {
                        //there was only an empty header.
                    }
                    break;
                }

                //check if there is a parent boundary in the header (wrong format!)
                if (hasParentBoundary && parentBoundaryFound(response, parentBoundaryStart, parentBoundaryEnd, out boundaryMimeReturnCode))
                {
                    callGetEmailWarning("MIME entity header  prematurely ended by parent boundary");
                    //empty this message
                    while (readMultiLine(out response)) { }
                    System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this
                    return boundaryMimeReturnCode;
                }
                //read header field
                //one header field can extend over one start line and multiple continuation lines
                //a continuation line starts with at least 1 blank (' ') or tab
                if (response[0] == ' ' || response[0] == '\t')
                {
                    //continuation line found.
                    if (completeHeaderField == null)
                    {
                        callGetEmailWarning("Email header starts with continuation line");
                        //empty this message
                        while (readMultiLine(out response)) { }
                        System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this
                        return MimeEntityReturnCode.problem;
                    }
                    else
                    {
                        // append space, if needed, and continuation line
                        if (completeHeaderField[completeHeaderField.Length - 1] != ' ')
                        {
                            //previous line did not end with a whitespace
                            //need to replace CRLF with a ' '
                            completeHeaderField += ' ' + response.TrimStart(WhiteSpaceChars);
                        }
                        else
                        {
                            //previous line did end with a whitespace
                            completeHeaderField += response.TrimStart(WhiteSpaceChars);
                        }
                    }

                }
                else
                {
                    //a new header field line found
                    if (completeHeaderField == null)
                    {
                        //very first field, just copy it and then check for continuation lines
                        completeHeaderField = response;
                    }
                    else
                    {
                        //new header line found
                        ProcessHeaderField(message, completeHeaderField);

                        //save the beginning of the next line
                        completeHeaderField = response;
                    }
                }
            }//end while read header lines


            //process body
            //------------

            MimeEntitySB.Length = 0;  //empty StringBuilder. For speed reasons, reuse StringBuilder defined as member of class
            string BoundaryDelimiterLineStart = null;
            bool isBoundaryDefined = false;
            if (message.ContentType.Boundary != null)
            {
                isBoundaryDefined = true;
                BoundaryDelimiterLineStart = "--" + message.ContentType.Boundary;
            }
            //prepare return code for the case there is no boundary in the body
            boundaryMimeReturnCode = MimeEntityReturnCode.bodyComplete;

            //read body lines
            while (readMultiLine(out response))
            {
                //check if there is a boundary line from this entity itself in the body
                if (isBoundaryDefined && response.TrimEnd() == BoundaryDelimiterLineStart)
                {
                    //boundary line found.
                    //stop the processing here and start a delimited body processing
                    return ProcessDelimitedBody(message, BoundaryDelimiterLineStart, parentBoundaryStart, parentBoundaryEnd);
                }

                //check if there is a parent boundary in the body
                if (hasParentBoundary &&
                  parentBoundaryFound(response, parentBoundaryStart, parentBoundaryEnd, out boundaryMimeReturnCode))
                {
                    //a parent boundary is found. Decode the content of the body received so far, then end this MIME entity
                    //note that boundaryMimeReturnCode is set here, but used in the return statement
                    break;
                }

                //process next line
                MimeEntitySB.Append(response + CRLF);
            }

            //a complete MIME body read
            //convert received US ASCII characters to .NET string (Unicode)
            string TransferEncodedMessage = MimeEntitySB.ToString();
            bool isAttachmentSaved = false;
            switch (message.ContentTransferEncoding)
            {
                case TransferEncoding.SevenBit:
                    //nothing to do
                    saveMessageBody(message, TransferEncodedMessage);
                    break;

                case TransferEncoding.Base64:
                    //convert base 64 -> byte[]
                    byte[] bodyBytes = System.Convert.FromBase64String(TransferEncodedMessage);
                    message.ContentStream = new MemoryStream(bodyBytes, false);

                    if (message.MediaMainType == "text")
                    {
                        //convert byte[] -> string
                        message.Body = DecodeByteArryToString(bodyBytes, message.BodyEncoding);

                    }
                    else if (message.MediaMainType == "image" || message.MediaMainType == "application")
                    {
                        SaveAttachment(message);
                        isAttachmentSaved = true;
                    }
                    break;

                case TransferEncoding.QuotedPrintable:
                    saveMessageBody(message, QuotedPrintable.Decode(TransferEncodedMessage));
                    break;

                default:
                    saveMessageBody(message, TransferEncodedMessage);
                    //no need to raise a warning here, the warning was done when analising the header
                    break;
            }

            if (message.ContentDisposition != null && message.ContentDisposition.DispositionType.ToLowerInvariant() == "attachment" && !isAttachmentSaved)
            {
                SaveAttachment(message);
                isAttachmentSaved = true;
            }
            return boundaryMimeReturnCode;
        }


        /// <summary>
        /// Check if the response line received is a parent boundary 
        /// </summary>
        private bool parentBoundaryFound(string response, string parentBoundaryStart, string parentBoundaryEnd, out MimeEntityReturnCode boundaryMimeReturnCode)
        {
            boundaryMimeReturnCode = MimeEntityReturnCode.undefined;
            if (response == null || response.Length < 2 || response[0] != '-' || response[1] != '-')
            {
                //quick test: reponse doesn't start with "--", so cannot be a separator line
                return false;
            }
            if (response == parentBoundaryStart)
            {
                boundaryMimeReturnCode = MimeEntityReturnCode.parentBoundaryStartFound;
                return true;
            }
            else if (response == parentBoundaryEnd)
            {
                boundaryMimeReturnCode = MimeEntityReturnCode.parentBoundaryEndFound;
                return true;
            }
            return false;
        }


        /// <summary>
        /// Convert one MIME header field and update message accordingly
        /// </summary>
        private void ProcessHeaderField(RxMailMessage message, string headerField)
        {
            string headerLineType;
            string headerLineContent;
            int separatorPosition = headerField.IndexOf(':');
            if (separatorPosition < 1)
            {
                // header field type not found, skip this line
                callGetEmailWarning("character ':' missing in header format field: '{0}'", headerField);
            }
            else
            {

                //process header field type
                headerLineType = headerField.Substring(0, separatorPosition).ToLowerInvariant();
                headerLineContent = headerField.Substring(separatorPosition + 1).Trim(WhiteSpaceChars);
                if (headerLineType == "" || headerLineContent == "")
                {
                    //1 of the 2 parts missing, drop the line
                    return;
                }
                // add header line to headers
                message.Headers.Add(headerLineType, headerLineContent);

                //interpret if possible
                switch (headerLineType)
                {
                    case "bcc":
                        AddMailAddresses(headerLineContent, message.Bcc);
                        break;
                    case "cc":
                        AddMailAddresses(headerLineContent, message.CC);
                        break;
                    case "content-description":
                        message.ContentDescription = headerLineContent;
                        break;
                    case "content-disposition":
                        //message.ContentDisposition = new ContentDisposition(headerLineContent);
                        message.SetContentDisposition(headerLineContent);
                        break;
                    case "content-id":
                        message.ContentId = headerLineContent;
                        break;
                    case "content-transfer-encoding":
                        message.TransferType = headerLineContent;
                        message.ContentTransferEncoding = ConvertToTransferEncoding(headerLineContent);
                        break;
                    case "content-type":
                        message.SetContentTypeFields(headerLineContent);
                        break;
                    case "date":
                        message.DeliveryDate = ConvertToDateTime(headerLineContent);
                        break;
                    case "delivered-to":
                        message.DeliveredTo = ConvertToMailAddress(headerLineContent);
                        break;
                    case "from":
                        MailAddress address = ConvertToMailAddress(headerLineContent);
                        if (address != null)
                            message.From = address;
                        break;
                    case "message-id":
                        message.MessageId = headerLineContent;
                        break;
                    case "mime-version":
                        message.MimeVersion = headerLineContent;
                        //message.BodyEncoding = new Encoding();
                        break;
                    case "sender":
                        message.Sender = ConvertToMailAddress(headerLineContent);
                        break;
                    case "subject":
                        message.Subject = headerLineContent;
                        break;
                    case "received":
                        //throw mail routing information away
                        break;
                    case "reply-to":
                        message.ReplyTo = ConvertToMailAddress(headerLineContent);
                        break;
                    case "return-path":
                        message.ReturnPath = ConvertToMailAddress(headerLineContent);
                        break;
                    case "to":
                        AddMailAddresses(headerLineContent, message.To);
                        break;
                    default:
                        message.UnknowHeaderlines.Add(headerField);
                        if (isCollectUnknowHeaderLines)
                        {
                            AllUnknowHeaderLines.Add(headerField);
                        }
                        break;
                }
            }
        }


        /// <summary>
        /// find individual addresses in the string and add it to address collection
        /// </summary>
        /// <param name="Addresses">string with possibly several email addresses</param>
        /// <param name="AddressCollection">parsed addresses</param>
        private void AddMailAddresses(string Addresses, MailAddressCollection AddressCollection)
        {
            MailAddress adr;
            try
            {
                // I copped out on this regex - trying to figure out how to do it in just a single regex was giving me a headache
                // just replace the comma that's inside quotes with a char (^c) thats not going to occur in a legal email name (or the 2183 RFC for that matter)
                Regex regexObj = new Regex(@"""[^""]*""");
                MatchCollection mc3 = regexObj.Matches(Addresses);
                foreach (Match match in mc3)
                {
                    string sQuotedString = match.Value.Replace(',', (char)3);
                    Addresses = Addresses.Replace(match.Value, sQuotedString);
                }
                string[] AddressSplit = Addresses.Split(',');
                foreach (string adrString in AddressSplit)
                {
                    // be sure to add the comma back if it was replaced
                    adr = ConvertToMailAddress(adrString.Replace((char)3, ','));
                    if (adr != null)
                    {
                        AddressCollection.Add(adr);
                    }
                }
            }
            catch
            {
                System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this
            }
        }


        /// <summary>
        /// Tries to convert a string into an email address
        /// </summary>
        public MailAddress ConvertToMailAddress(string address)
        {
            // just remove the quotes since they are not valid in email addresses and the MailAdress parser doesn't need them. 
            // this will handles both 
            // ->>>    "name@host.com" 
            // and 
            // ->>>    "LName,FName (name@host.com)" <name@host.com>
            // formats
            address = address.Replace("\"", "");
            address = address.Trim();
            if (address == "<>")
            {
                //empty email address, not recognised a such by .NET
                return null;
            }
            try
            {
                return new MailAddress(address);
                //return new MailAddress(address.Trim(new char[] { '"' }));
            }
            catch
            {
                callGetEmailWarning("address format not recognized: '" + address.Trim() + "'");
            }
            return null;
        }


        private IFormatProvider culture = new CultureInfo("en-US", true);

        /// <summary>
        /// Tries to convert string to date
        /// If there is a run time error, the smallest possible date is returned
        /// <example>Wed, 04 Jan 2006 07:58:08 -0800</example>
        /// </summary>
        public DateTime ConvertToDateTime(string date)
        {
            DateTime ReturnDateTime;
            try
            {
                //sample; 'Wed, 04 Jan 2006 07:58:08 -0800 (PST)'
                //remove day of the week before ','
                //remove date zone in '()', -800 indicates the zone already

                //remove day of week
                string cleanDateTime = date;
                string[] DateSplit = cleanDateTime.Split(CommaChars, 2);
                if (DateSplit.Length > 1)
                {
                    cleanDateTime = DateSplit[1];
                }

                //remove time zone (PST)
                DateSplit = cleanDateTime.Split(BracketChars);
                if (DateSplit.Length > 1)
                {
                    cleanDateTime = DateSplit[0];
                }

                //convert to DateTime
                if (!DateTime.TryParse(
                  cleanDateTime,
                  culture,
                  DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces,
                  out ReturnDateTime))
                {
                    //try just to convert the date
                    int DateLength = cleanDateTime.IndexOf(':') - 3;
                    cleanDateTime = cleanDateTime.Substring(0, DateLength);

                    if (DateTime.TryParse(
                      cleanDateTime,
                      culture,
                      DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces,
                      out ReturnDateTime))
                    {
                        callGetEmailWarning("got only date, time format not recognised: '" + date + "'");
                    }
                    else
                    {
                        callGetEmailWarning("date format not recognised: '" + date + "'");
                        System.Diagnostics.Debugger.Break();  //didn't have a sample email to test this
                        return DateTime.MinValue;
                    }
                }

            }
            catch
            {
                callGetEmailWarning("date format not recognised: '" + date + "'");
                return DateTime.MinValue;
            }
            return ReturnDateTime;
        }


        /// <summary>
        /// converts TransferEncoding as defined in the RFC into a .NET TransferEncoding
        /// 
        /// .NET doesn't know the type "bit8". It is translated here into "bit7", which
        /// requires the same kind of processing (none).
        /// </summary>
        /// <param name="TransferEncodingString"></param>
        /// <returns></returns>
        private TransferEncoding ConvertToTransferEncoding(string TransferEncodingString)
        {
            // here, "bit8" is marked as "bit7" (i.e. no transfer encoding needed)
            // "binary" is illegal in SMTP
            // something like "7bit" / "8bit" / "binary" / "quoted-printable" / "base64"
            switch (TransferEncodingString.Trim().ToLowerInvariant())
            {
                case "7bit":
                case "8bit":
                    return TransferEncoding.SevenBit;
                case "quoted-printable":
                    return TransferEncoding.QuotedPrintable;
                case "base64":
                    return TransferEncoding.Base64;
                case "binary":
                    throw new Exception("SMPT does not support binary transfer encoding");
                default:
                    callGetEmailWarning("not supported content-transfer-encoding: " + TransferEncodingString);
                    return TransferEncoding.Unknown;
            }
        }

        /// <summary>
        /// Copies the content found for the MIME entity to the RxMailMessage body and creates
        /// a stream which can be used to create attachements, alternative views, ...
        /// </summary>
        private void saveMessageBody(RxMailMessage message, string contentString)
        {
            message.Body = contentString;
            System.Text.Encoding ascii = System.Text.Encoding.ASCII;
            MemoryStream bodyStream = new MemoryStream(ascii.GetBytes(contentString), 0, contentString.Length);
            message.ContentStream = bodyStream;
        }

        /// <summary>
        /// each attachement is stored in its own MIME entity and read into this entity's
        /// ContentStream. SaveAttachment creates an attachment out of the ContentStream
        /// and attaches it to the parent MIME entity.
        /// </summary>
        private void SaveAttachment(RxMailMessage message)
        {
            if (message.Parent == null)
            {
                System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this
            }
            else
            {
                Attachment thisAttachment = new Attachment(message.ContentStream, message.ContentType);

                //no idea why ContentDisposition is read only. on the other hand, it is anyway redundant
                if (message.ContentDisposition != null)
                {
                    ContentDisposition messageContentDisposition = message.ContentDisposition;
                    ContentDisposition AttachmentContentDisposition = thisAttachment.ContentDisposition;
                    if (messageContentDisposition.CreationDate > DateTime.MinValue)
                    {
                        AttachmentContentDisposition.CreationDate = messageContentDisposition.CreationDate;
                    }
                    AttachmentContentDisposition.DispositionType = messageContentDisposition.DispositionType;
                    AttachmentContentDisposition.FileName = messageContentDisposition.FileName;
                    AttachmentContentDisposition.Inline = messageContentDisposition.Inline;
                    if (messageContentDisposition.ModificationDate > DateTime.MinValue)
                    {
                        AttachmentContentDisposition.ModificationDate = messageContentDisposition.ModificationDate;
                    }

                    // see note below
                    //AttachmentContentDisposition.Parameters.Clear();
                    if (messageContentDisposition.ReadDate > DateTime.MinValue)
                    {
                        AttachmentContentDisposition.ReadDate = messageContentDisposition.ReadDate;
                    }
                    if (messageContentDisposition.Size > 0)
                    {
                        AttachmentContentDisposition.Size = messageContentDisposition.Size;
                    }

                    // I think this is a bug. Setting the ContentDisposition values above had 
                    // already set the Parameters collection so I got an error when the attempt 
                    // was made to add the same parameter again
                    //foreach (string key in messageContentDisposition.Parameters.Keys)
                    //{
                    //    AttachmentContentDisposition.Parameters.Add(key, messageContentDisposition.Parameters[key]);
                    //}
                }

                //get ContentId
                string contentIdString = message.ContentId;
                if (contentIdString != null)
                {
                    thisAttachment.ContentId = RemoveBrackets(contentIdString);
                }
                thisAttachment.TransferEncoding = message.ContentTransferEncoding;
                message.Parent.Attachments.Add(thisAttachment);
            }
        }


        /// <summary>
        /// removes leading '&lt;' and trailing '&gt;' if both exist
        /// </summary>
        /// <param name="parameterString"></param>
        /// <returns></returns>
        private string RemoveBrackets(string parameterString)
        {
            if (parameterString == null)
            {
                return null;
            }
            if (parameterString.Length < 1 ||
                  parameterString[0] != '<' ||
                  parameterString[parameterString.Length - 1] != '>')
            {
                System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this
                return parameterString;
            }
            else
            {
                return parameterString.Substring(1, parameterString.Length - 2);
            }
        }


        private MimeEntityReturnCode ProcessDelimitedBody(RxMailMessage message, string BoundaryStart, string parentBoundaryStart, string parentBoundaryEnd)
        {
            string response;

            if (BoundaryStart.Trim() == parentBoundaryStart.Trim())
            {
                //Mime entity boundaries have to be unique
                callGetEmailWarning("new boundary same as parent boundary: '{0}'", parentBoundaryStart);
                //empty this message
                while (readMultiLine(out response)) { }
                return MimeEntityReturnCode.problem;
            }

            MimeEntityReturnCode ReturnCode;
            do
            {

                //empty StringBuilder
                MimeEntitySB.Length = 0;
                RxMailMessage ChildPart = message.CreateChildEntity();

                //recursively call MIME part processing
                ReturnCode = ProcessMimeEntity(ChildPart, BoundaryStart);

                if (ReturnCode == MimeEntityReturnCode.problem)
                {
                    //it seems the received email doesn't follow the MIME specification. Stop here
                    return MimeEntityReturnCode.problem;
                }

                //add the newly found child MIME part to the parent
                AddChildPartsToParent(ChildPart, message);
            } while (ReturnCode != MimeEntityReturnCode.parentBoundaryEndFound);

            //disregard all future lines until parent boundary is found or end of complete message
            MimeEntityReturnCode boundaryMimeReturnCode;
            bool hasParentBoundary = parentBoundaryStart.Length > 0;
            while (readMultiLine(out response))
            {
                if (hasParentBoundary && parentBoundaryFound(response, parentBoundaryStart, parentBoundaryEnd, out boundaryMimeReturnCode))
                {
                    return boundaryMimeReturnCode;
                }
            }

            return MimeEntityReturnCode.bodyComplete;
        }


        /// <summary>
        /// Add all attachments and alternative views from child to the parent
        /// </summary>
        private void AddChildPartsToParent(RxMailMessage child, RxMailMessage parent)
        {
            //add the child itself to the parent
            parent.Entities.Add(child);

            //add the alternative views of the child to the parent
            if (child.AlternateViews != null)
            {
                foreach (AlternateView childView in child.AlternateViews)
                {
                    parent.AlternateViews.Add(childView);
                }
            }

            //add the body of the child as alternative view to parent
            //this should be the last view attached here, because the POP 3 MIME client
            //is supposed to display the last alternative view
            if (child.MediaMainType == "text" && child.ContentStream != null && child.Parent.ContentType != null && child.Parent.ContentType.MediaType.ToLowerInvariant() == "multipart/alternative")
            {
                AlternateView thisAlternateView = new AlternateView(child.ContentStream);
                thisAlternateView.ContentId = RemoveBrackets(child.ContentId);
                thisAlternateView.ContentType = child.ContentType;
                thisAlternateView.TransferEncoding = child.ContentTransferEncoding;
                parent.AlternateViews.Add(thisAlternateView);
            }

            //add the attachments of the child to the parent
            if (child.Attachments != null)
            {
                foreach (Attachment childAttachment in child.Attachments)
                {
                    parent.Attachments.Add(childAttachment);
                }
            }
        }


        /// <summary>
        /// Converts byte array to string, using decoding as requested
        /// </summary>
        public string DecodeByteArryToString(byte[] ByteArry, Encoding ByteEncoding)
        {
            if (ByteArry == null)
            {
                //no bytes to convert
                return null;
            }
            Decoder byteArryDecoder;
            if (ByteEncoding == null)
            {
                //no encoding indicated. Let's try UTF7
                System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this
                byteArryDecoder = Encoding.UTF7.GetDecoder();
            }
            else
            {
                byteArryDecoder = ByteEncoding.GetDecoder();
            }
            int charCount = byteArryDecoder.GetCharCount(ByteArry, 0, ByteArry.Length);
            char[] bodyChars = new Char[charCount];
            int charsDecodedCount = byteArryDecoder.GetChars(ByteArry, 0, ByteArry.Length, bodyChars, 0);
            //convert char[] to string
            return new string(bodyChars);
        }

        /// <summary>
        /// read one line in multiline mode from the Email stream. 
        /// </summary>
        /// <param name="response">line received</param>
        /// <returns>false: end of message</returns>
        /// <returns></returns>
        protected bool readMultiLine(out string response)
        {
            response = null;
            response = EmailStreamReader.ReadLine();
            if (response == null)
                return false;     // if we can't read anymore from the stream then the stream is ended since this version is reading from a file and not the net

            //check for byte stuffing, i.e. if a line starts with a '.', another '.' is added, unless
            //it is the last line
            if (response.Length > 0 && response[0] == '.')
            {
                if (response == ".")
                {
                    //closing line found
                    return false;
                }
                //remove the first '.'
                response = response.Substring(1, response.Length - 1);
            }
            return true;
        }
    }
}
